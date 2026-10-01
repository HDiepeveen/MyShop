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
        var delivery = await (await host.Client.PostAsJsonAsync("/api/delivery-methods", new
        {
            name = "Pakketdienst", description = "Binnen twee werkdagen.", amount = "4.95",
            currency = "EUR", enabled = true
        })).Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(HttpStatusCode.Forbidden,
            (await host.Client.GetAsync("/api/customer/orders")).StatusCode);

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
            deliveryMethodId = delivery.GetProperty("id").GetGuid(),
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
        Assert.Equal("Pakketdienst", order.GetProperty("deliveryMethod").GetProperty("name").GetString());
        Assert.Equal("4.95", order.GetProperty("deliveryMethod").GetProperty("amount").GetString());
        Assert.Equal("29.95", Assert.Single(order.GetProperty("totals").EnumerateArray())
            .GetProperty("amount").GetString());
        Assert.Equal(HttpStatusCode.OK, (await host.Client.PutAsJsonAsync(
            $"/api/orders/{first.GetProperty("id").GetGuid()}/status", new
            {
                status = "cancelled", reason = "Klant annuleert",
                revision = order.GetProperty("revision").GetGuid()
            })).StatusCode);
        var restored = await host.Client.GetFromJsonAsync<JsonElement>($"/api/products/{productId}");
        Assert.Equal(2, restored.GetProperty("variants")[0].GetProperty("stockQuantity").GetInt32());

        Assert.Equal(HttpStatusCode.Unauthorized,
            (await visitor.GetAsync("/api/customer/orders")).StatusCode);
        var customerCookies = new CookieContainer();
        using var customer = new HttpClient(new HttpClientHandler
        {
            CookieContainer = customerCookies,
            AllowAutoRedirect = false
        }) { BaseAddress = host.Client.BaseAddress };
        Assert.Equal(HttpStatusCode.NoContent, (await customer.GetAsync("/api/auth/csrf")).StatusCode);
        customer.DefaultRequestHeaders.Add("X-XSRF-TOKEN",
            customerCookies.GetCookies(customer.BaseAddress!)["XSRF-TOKEN"]!.Value);
        Assert.Equal(HttpStatusCode.NoContent, (await customer.PostAsJsonAsync(
            "/api/customer/auth/register",
            new { email = "customer@example.test", password = "Customer-Password42!" })).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await customer.GetAsync("/api/auth/csrf")).StatusCode);
        customer.DefaultRequestHeaders.Remove("X-XSRF-TOKEN");
        customer.DefaultRequestHeaders.Add("X-XSRF-TOKEN",
            customerCookies.GetCookies(customer.BaseAddress!)["XSRF-TOKEN"]!.Value);
        var customerResponse = await customer.PostAsJsonAsync("/api/shop/orders",
            request with { checkoutToken = Guid.NewGuid() });
        Assert.Equal(HttpStatusCode.OK, customerResponse.StatusCode);
        var customerOrder = await customerResponse.Content.ReadFromJsonAsync<JsonElement>();
        var history = await customer.GetFromJsonAsync<JsonElement>("/api/customer/orders?offset=0&limit=20");
        Assert.Equal(1, history.GetProperty("totalCount").GetInt32());
        Assert.Equal(customerOrder.GetProperty("id").GetGuid(),
            history.GetProperty("items")[0].GetProperty("id").GetGuid());
        var historyDetail = await customer.GetAsync(
            $"/api/customer/orders/{customerOrder.GetProperty("id").GetGuid()}");
        Assert.Equal(HttpStatusCode.OK, historyDetail.StatusCode);
        var historyOrder = await historyDetail.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(HttpStatusCode.NotFound, (await customer.GetAsync(
            $"/api/customer/orders/{first.GetProperty("id").GetGuid()}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await customer.PostAsJsonAsync(
            $"/api/customer/orders/{first.GetProperty("id").GetGuid()}/cancel",
            new { revision = Guid.NewGuid() })).StatusCode);
        var cancellation = await customer.PostAsJsonAsync(
            $"/api/customer/orders/{customerOrder.GetProperty("id").GetGuid()}/cancel",
            new { revision = historyOrder.GetProperty("revision").GetGuid() });
        Assert.Equal(HttpStatusCode.OK, cancellation.StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await customer.PostAsJsonAsync(
            $"/api/customer/orders/{customerOrder.GetProperty("id").GetGuid()}/cancel",
            new { revision = historyOrder.GetProperty("revision").GetGuid() })).StatusCode);
        var cancelledHistory = await customer.GetFromJsonAsync<JsonElement>(
            $"/api/customer/orders/{customerOrder.GetProperty("id").GetGuid()}");
        Assert.Equal("cancelled", cancelledHistory.GetProperty("status").GetString());
        Assert.Equal(2, (await host.Client.GetFromJsonAsync<JsonElement>(
            $"/api/products/{productId}")).GetProperty("variants")[0]
            .GetProperty("stockQuantity").GetInt32());

        await using var scope = host.App.Services.CreateAsyncScope();
        var database = scope.ServiceProvider.GetRequiredService<MyShopDbContext>();
        Assert.Equal(2, await database.Database.SqlQueryRaw<int>(
            "SELECT COUNT(*) AS [Value] FROM [Orders]").SingleAsync());
        Assert.Equal("Ada Lovelace", await database.Database.SqlQueryRaw<string>(
            "SELECT [CustomerName] AS [Value] FROM [Orders] WHERE [CustomerUserId] IS NULL").SingleAsync());
        Assert.Equal(12.50m, await database.Database.SqlQueryRaw<decimal>(
            "SELECT TOP(1) [UnitAmount] AS [Value] FROM [OrderLines]").SingleAsync());
        Assert.Equal(29.95m, await database.Database.SqlQueryRaw<decimal>(
            "SELECT TOP(1) [Amount] AS [Value] FROM [OrderTotals]").SingleAsync());
        Assert.Equal("Betaal binnen 14 dagen.", await database.Database.SqlQueryRaw<string>(
            "SELECT TOP(1) [PaymentInstructions] AS [Value] FROM [Orders]").SingleAsync());
        Assert.Equal(1, await database.Database.SqlQueryRaw<int>(
            "SELECT COUNT(*) AS [Value] FROM [Orders] WHERE [CustomerUserId] IS NOT NULL").SingleAsync());
    }
}
