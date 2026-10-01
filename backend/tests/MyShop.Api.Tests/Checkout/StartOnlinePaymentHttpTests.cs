using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using MyShop.Api.Tests.Security;
using MyShop.Infrastructure.Persistence;

namespace MyShop.Api.Tests.Checkout;

public sealed class StartOnlinePaymentHttpTests
{
    [SecuritySqlFact]
    public async Task GuestStartsOnlinePaymentAndStoresProviderStart()
    {
        await using var host = await SecurityHost.Create(new Dictionary<string, string?>
        {
            ["Payments:Online:Provider"] = "TestPay"
        });
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
        var current = await host.Client.GetFromJsonAsync<JsonElement>($"/api/products/{productId}");
        Assert.Equal(HttpStatusCode.NoContent, (await host.Client.PutAsJsonAsync(
            $"/api/products/{productId}/presentation", new
            {
                description = "A shirt", imageUrl = "https://example.com/shirt.jpg", imageAlt = "Shirt",
                isPublished = true, revision = current.GetProperty("revision").GetGuid()
            })).StatusCode);
        var settings = await host.Client.GetFromJsonAsync<JsonElement>("/api/payment-options");
        Assert.Equal(HttpStatusCode.OK, (await host.Client.PutAsJsonAsync("/api/payment-options", new
        {
            payLaterEnabled = true,
            onlinePaymentEnabled = true,
            revision = settings.GetProperty("revision").GetGuid()
        })).StatusCode);
        var delivery = await (await host.Client.PostAsJsonAsync("/api/delivery-methods", new
        {
            name = "Pakketdienst", description = "Binnen twee werkdagen.", amount = "4.95",
            currency = "EUR", enabled = true
        })).Content.ReadFromJsonAsync<JsonElement>();

        var cookies = new CookieContainer();
        using var visitor = new HttpClient(new HttpClientHandler
        {
            CookieContainer = cookies,
            AllowAutoRedirect = false
        }) { BaseAddress = host.Client.BaseAddress };
        Assert.Equal(HttpStatusCode.NoContent, (await visitor.GetAsync("/api/auth/csrf")).StatusCode);
        visitor.DefaultRequestHeaders.Add("X-XSRF-TOKEN",
            cookies.GetCookies(visitor.BaseAddress!)["XSRF-TOKEN"]!.Value);
        var checkoutToken = Guid.NewGuid();

        var response = await visitor.PostAsJsonAsync("/api/shop/online-payments", new
        {
            checkoutToken,
            deliveryMethodId = delivery.GetProperty("id").GetGuid(),
            customerName = "Ada Lovelace",
            email = "ada@example.com",
            addressLine = "Main street 1",
            postalCode = "1234 AB",
            city = "Amsterdam",
            countryCode = "NL",
            lines = new[]
            {
                new { productId, variantId, quantity = 2, expectedAmount = "12.50", expectedCurrency = "EUR" }
            }
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var payment = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("TestPay", payment.GetProperty("providerName").GetString());
        Assert.Equal($"OP-{checkoutToken:N}".ToUpperInvariant(), payment.GetProperty("paymentReference").GetString());
        Assert.Equal($"test_{checkoutToken:N}", payment.GetProperty("providerPaymentId").GetString());
        Assert.Equal($"https://payments.example.test/test_{checkoutToken:N}",
            payment.GetProperty("checkoutUrl").GetString());

        await using var scope = host.App.Services.CreateAsyncScope();
        var database = scope.ServiceProvider.GetRequiredService<MyShopDbContext>();
        Assert.Equal(1, await database.Database.SqlQueryRaw<int>(
            "SELECT COUNT(*) AS [Value] FROM [OnlinePaymentStarts]").SingleAsync());
        Assert.Equal(1, await database.Database.SqlQueryRaw<int>(
            "SELECT COUNT(*) AS [Value] FROM [OnlinePaymentStartLines]").SingleAsync());
        Assert.Equal(1, await database.Database.SqlQueryRaw<int>(
            "SELECT COUNT(*) AS [Value] FROM [OnlinePaymentStartTotals]").SingleAsync());
        Assert.Equal("Ada Lovelace", await database.Database.SqlQueryRaw<string>(
            "SELECT [CustomerName] AS [Value] FROM [OnlinePaymentStarts]").SingleAsync());
        Assert.Equal("Shirt", await database.Database.SqlQueryRaw<string>(
            "SELECT [ProductName] AS [Value] FROM [OnlinePaymentStartLines]").SingleAsync());
        Assert.Equal($"test_{checkoutToken:N}", await database.Database.SqlQueryRaw<string>(
            "SELECT [ProviderPaymentId] AS [Value] FROM [OnlinePaymentStarts]").SingleAsync());
        Assert.Equal(29.95m, await database.Database.SqlQueryRaw<decimal>(
            "SELECT [Amount] AS [Value] FROM [OnlinePaymentStartTotals]").SingleAsync());

        var repeated = await visitor.PostAsJsonAsync("/api/shop/online-payments", new
        {
            checkoutToken,
            deliveryMethodId = delivery.GetProperty("id").GetGuid(),
            customerName = "Ada Lovelace",
            email = "ada@example.com",
            addressLine = "Main street 1",
            postalCode = "1234 AB",
            city = "Amsterdam",
            countryCode = "NL",
            lines = new[]
            {
                new { productId, variantId, quantity = 2, expectedAmount = "12.50", expectedCurrency = "EUR" }
            }
        });
        Assert.Equal(HttpStatusCode.OK, repeated.StatusCode);
        var repeatedPayment = await repeated.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(payment.GetProperty("providerPaymentId").GetString(),
            repeatedPayment.GetProperty("providerPaymentId").GetString());
        Assert.Equal(1, await database.Database.SqlQueryRaw<int>(
            "SELECT COUNT(*) AS [Value] FROM [OnlinePaymentStarts]").SingleAsync());
    }
}
