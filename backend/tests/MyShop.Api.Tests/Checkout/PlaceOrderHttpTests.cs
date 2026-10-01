using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using MyShop.Api.Tests.Security;
using MyShop.Infrastructure.Persistence;

namespace MyShop.Api.Tests.Checkout;

public sealed class PlaceOrderHttpTests
{
    [SecuritySqlFact]
    public async Task GuestPlacesAnIdempotentPayLaterOrderFromCurrentServerPrices()
    {
        await using var host = await SecurityHost.Create();
        await host.Csrf();
        Assert.Equal(HttpStatusCode.NoContent, (await host.Login()).StatusCode);
        await host.Csrf();
        var type = await (await host.Client.PostAsJsonAsync("/api/product-types", new { name = "Clothing" }))
            .Content.ReadFromJsonAsync<JsonElement>();
        var product = await (await host.Client.PostAsJsonAsync("/api/products", new
        {
            productTypeId = type.GetProperty("id").GetGuid(),
            name = "Shirt",
            initialVariantName = "Small"
        })).Content.ReadFromJsonAsync<JsonElement>();
        var productId = product.GetProperty("id").GetGuid();
        var createdProduct = await host.Client.GetFromJsonAsync<JsonElement>($"/api/products/{productId}");
        var variantId = createdProduct.GetProperty("variants")[0].GetProperty("id").GetGuid();
        Assert.Equal(HttpStatusCode.NoContent, (await host.Client.PutAsJsonAsync(
            $"/api/products/{productId}/variants/{variantId}/price",
            new { amount = 12.50m, currency = "EUR" })).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await host.Client.PutAsJsonAsync(
            $"/api/products/{productId}/variants/{variantId}/stock",
            new { quantity = 2 })).StatusCode);
        var current = await host.Client.GetFromJsonAsync<JsonElement>($"/api/products/{productId}");
        Assert.Equal(HttpStatusCode.NoContent, (await host.Client.PutAsJsonAsync(
            $"/api/products/{productId}/presentation", new
            {
                description = "A shirt", imageUrl = "https://example.com/shirt.jpg", imageAlt = "Shirt",
                isPublished = true, revision = current.GetProperty("revision").GetGuid()
            })).StatusCode);
        var paymentSettings = await host.Client.GetFromJsonAsync<JsonElement>("/api/payment-options");
        Assert.Equal(HttpStatusCode.OK, (await host.Client.PutAsJsonAsync("/api/payment-options", new
        {
            payLaterEnabled = true,
            onlinePaymentEnabled = false,
            payLaterInstructions = "Betaal binnen 14 dagen.",
            revision = paymentSettings.GetProperty("revision").GetGuid()
        })).StatusCode);

        var cookies = new CookieContainer();
        using var visitor = new HttpClient(new HttpClientHandler
        {
            CookieContainer = cookies,
            AllowAutoRedirect = false
        }) { BaseAddress = host.Client.BaseAddress };
        var request = new
        {
            checkoutToken = Guid.NewGuid(), paymentMethod = "payLater", customerName = "Ada Lovelace",
            email = "ada@example.com", addressLine = "Main street 1", postalCode = "1234 AB",
            city = "Amsterdam", countryCode = "NL",
            lines = new[] { new { productId, variantId, quantity = 2, expectedAmount = "12.50", expectedCurrency = "EUR" } }
        };
        Assert.Equal(HttpStatusCode.BadRequest,
            (await visitor.PostAsJsonAsync("/api/shop/orders", request)).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await visitor.GetAsync("/api/auth/csrf")).StatusCode);
        visitor.DefaultRequestHeaders.Add("X-XSRF-TOKEN",
            cookies.GetCookies(visitor.BaseAddress!)["XSRF-TOKEN"]!.Value);

        var firstResponse = await visitor.PostAsJsonAsync("/api/shop/orders", request);
        Assert.Equal(HttpStatusCode.OK, firstResponse.StatusCode);
        var first = await firstResponse.Content.ReadFromJsonAsync<JsonElement>();
        Assert.StartsWith("MS-", first.GetProperty("number").GetString(), StringComparison.Ordinal);
        Assert.Equal("Betaal binnen 14 dagen.", first.GetProperty("paymentInstructions").GetString());
        var repeated = await (await visitor.PostAsJsonAsync("/api/shop/orders", request))
            .Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(first.GetProperty("id").GetGuid(), repeated.GetProperty("id").GetGuid());
        Assert.Equal("Betaal binnen 14 dagen.", repeated.GetProperty("paymentInstructions").GetString());
        var depleted = await host.Client.GetFromJsonAsync<JsonElement>($"/api/products/{productId}");
        Assert.Equal(0, depleted.GetProperty("variants")[0].GetProperty("stockQuantity").GetInt32());
        var competing = await visitor.PostAsJsonAsync("/api/shop/orders",
            request with { checkoutToken = Guid.NewGuid() });
        Assert.Equal(HttpStatusCode.Conflict, competing.StatusCode);
        Assert.Equal("cartChanged", (await competing.Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("code").GetString());
        var order = await host.Client.GetFromJsonAsync<JsonElement>(
            $"/api/orders/{first.GetProperty("id").GetGuid()}");
        Assert.Equal(HttpStatusCode.OK, (await host.Client.PutAsJsonAsync(
            $"/api/orders/{first.GetProperty("id").GetGuid()}/status", new
            {
                status = "cancelled", reason = "Klant annuleert",
                revision = order.GetProperty("revision").GetGuid()
            })).StatusCode);
        var restored = await host.Client.GetFromJsonAsync<JsonElement>($"/api/products/{productId}");
        Assert.Equal(2, restored.GetProperty("variants")[0].GetProperty("stockQuantity").GetInt32());

        await using var scope = host.App.Services.CreateAsyncScope();
        var database = scope.ServiceProvider.GetRequiredService<MyShopDbContext>();
        Assert.Equal(1, await database.Database.SqlQueryRaw<int>(
            "SELECT COUNT(*) AS [Value] FROM [Orders]").SingleAsync());
        Assert.Equal("Ada Lovelace", await database.Database.SqlQueryRaw<string>(
            "SELECT [CustomerName] AS [Value] FROM [Orders]").SingleAsync());
        Assert.Equal(12.50m, await database.Database.SqlQueryRaw<decimal>(
            "SELECT [UnitAmount] AS [Value] FROM [OrderLines]").SingleAsync());
        Assert.Equal(25m, await database.Database.SqlQueryRaw<decimal>(
            "SELECT [Amount] AS [Value] FROM [OrderTotals]").SingleAsync());
        Assert.Equal("Betaal binnen 14 dagen.", await database.Database.SqlQueryRaw<string>(
            "SELECT [PaymentInstructions] AS [Value] FROM [Orders]").SingleAsync());
    }
}
