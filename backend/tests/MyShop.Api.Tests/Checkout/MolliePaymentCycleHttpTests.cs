using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using MyShop.Api.Tests.Security;
using MyShop.Infrastructure.Persistence;

namespace MyShop.Api.Tests.Checkout;

public sealed class MolliePaymentCycleHttpTests
{
    [SecuritySqlFact]
    public async Task ChecksFullCycleIncludingCallbackRetriesCustomerOwnershipEmailAndInvoice()
    {
        var mollie = new MollieHandler();
        await using var host = await SecurityHost.Create(new()
        {
            ["Payments:Online:Provider"] = "Mollie",
            ["Payments:Mollie:ApiKey"] = "test_abcdefghijklmnopqrstuvwxyz",
            ["Payments:Mollie:ReturnUrl"] = "http://localhost:4200/winkel/betaling",
            ["Payments:Mollie:WebhookUrl"] = "https://shop.example.test/api/payments/mollie/webhook"
        }, services => services.AddHttpClient("Mollie").ConfigurePrimaryHttpMessageHandler(() => mollie));
        await host.Csrf(); Assert.Equal(HttpStatusCode.NoContent, (await host.Login()).StatusCode); await host.Csrf();
        var type = await (await host.Client.PostAsJsonAsync("/api/product-types", new { name = "Mollie test" })).Content.ReadFromJsonAsync<JsonElement>();
        var product = await (await host.Client.PostAsJsonAsync("/api/products", new { productTypeId = type.GetProperty("id").GetGuid(), name = "Test product", initialVariantName = "Default" })).Content.ReadFromJsonAsync<JsonElement>();
        var productId = product.GetProperty("id").GetGuid();
        var detail = await host.Client.GetFromJsonAsync<JsonElement>($"/api/products/{productId}");
        var variantId = detail.GetProperty("variants")[0].GetProperty("id").GetGuid();
        Assert.Equal(HttpStatusCode.NoContent, (await host.Client.PutAsJsonAsync($"/api/products/{productId}/variants/{variantId}/price", new { amount = 0m, netAmount = 10m, currency = "EUR", vatRate = 21m, vatExempt = false })).StatusCode);
        detail = await host.Client.GetFromJsonAsync<JsonElement>($"/api/products/{productId}");
        Assert.Equal(HttpStatusCode.NoContent, (await host.Client.PutAsJsonAsync($"/api/products/{productId}/presentation", new { description = "Test", imageUrl = "https://example.test/product.jpg", imageAlt = "Product", isPublished = true, revision = detail.GetProperty("revision").GetGuid() })).StatusCode);
        var settings = await host.Client.GetFromJsonAsync<JsonElement>("/api/payment-options");
        Assert.Equal(HttpStatusCode.OK, (await host.Client.PutAsJsonAsync("/api/payment-options", new { payLaterEnabled = true, onlinePaymentEnabled = true, revision = settings.GetProperty("revision").GetGuid() })).StatusCode);
        var delivery = await (await host.Client.PostAsJsonAsync("/api/delivery-methods", new { name = "Pakket", amount = "4.95", currency = "EUR", enabled = true })).Content.ReadFromJsonAsync<JsonElement>();
        var company = await host.Client.GetFromJsonAsync<JsonElement>("/api/billing/company");
        Assert.Equal(HttpStatusCode.OK, (await host.Client.PutAsJsonAsync("/api/billing/company", new { name = "Test seller", addressLine = "Street 1", postalCode = "1234 AB", city = "Utrecht", vatId = "NL123456789B01", kvkNumber = "12345678", invoicePrefix = "INV-", revision = company.GetProperty("revision").GetGuid() })).StatusCode);
        await host.Csrf();
        Assert.Equal(HttpStatusCode.NoContent, (await host.Client.PostAsJsonAsync("/api/customer/auth/register", new { email = "mollie-client@example.test", password = SecurityHost.Password })).StatusCode);
        await host.Csrf();
        var token = Guid.NewGuid();
        var input = new { checkoutToken = token, deliveryMethodId = delivery.GetProperty("id").GetGuid(), customerName = "Ada", email = "mollie-client@example.test", addressLine = "Buyer street 2", postalCode = "2345 BC", city = "Amsterdam", countryCode = "NL", lines = new[] { new { productId, variantId, quantity = 2, expectedAmount = "12.10", expectedCurrency = "EUR" } } };
        var started = await host.Client.PostAsJsonAsync("/api/shop/online-payments", input);
        Assert.Equal(HttpStatusCode.OK, started.StatusCode);
        var payment = await started.Content.ReadFromJsonAsync<JsonElement>();
        var id = payment.GetProperty("providerPaymentId").GetString()!;
        Assert.Equal("29.15", payment.GetProperty("totals")[0].GetProperty("amount").GetString());
        Assert.Equal(HttpStatusCode.OK, (await host.Client.PostAsJsonAsync("/api/shop/online-payments", input)).StatusCode);
        Assert.Equal(1, mollie.Starts);
        var completion = $"/api/shop/online-payments/{token}/complete";
        using var callback = new HttpClient { BaseAddress = host.Client.BaseAddress };
        var webhook = $"/api/payments/mollie/webhook?checkoutToken={token}";
        Assert.Equal(HttpStatusCode.BadRequest, (await callback.PostAsync(webhook, new FormUrlEncodedContent(new Dictionary<string,string> { ["id"] = "tr_wrong" }))).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await callback.PostAsJsonAsync("/api/customer/auth/register", new { email = "csrf-client@example.test", password = SecurityHost.Password })).StatusCode);
        foreach (var (status, code) in new[] { ("open", "paymentPending"), ("pending", "paymentPending"), ("authorized", "paymentPending"), ("failed", "paymentFailed"), ("canceled", "paymentCanceled"), ("expired", "paymentExpired") })
        {
            mollie.Status = status;
            var check = await host.Client.GetAsync(completion);
            Assert.Equal(HttpStatusCode.Conflict, check.StatusCode);
            Assert.Equal(code, (await check.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("code").GetString());
            Assert.Equal(HttpStatusCode.OK, (await callback.PostAsync(webhook, Form(id))).StatusCode);
        }
        mollie.Status = "paid"; mollie.AmountOverride = "0.01";
        Assert.Equal(HttpStatusCode.Conflict, (await host.Client.GetAsync(completion)).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await callback.PostAsync(webhook, Form(id))).StatusCode);
        mollie.AmountOverride = null;
        await using (var scope = host.App.Services.CreateAsyncScope()) Assert.Equal(0, await scope.ServiceProvider.GetRequiredService<MyShopDbContext>().Database.SqlQueryRaw<int>("SELECT COUNT(*) AS [Value] FROM [Orders]").SingleAsync());
        // Callback completes the order even if the customer has closed the payment page.
        Assert.Equal(HttpStatusCode.OK, (await callback.PostAsync(webhook, Form(id))).StatusCode);
        var completed = await host.Client.GetAsync(completion); Assert.Equal(HttpStatusCode.OK, completed.StatusCode);
        var receipt = await completed.Content.ReadFromJsonAsync<JsonElement>(); var orderId = receipt.GetProperty("id").GetGuid();
        Assert.Equal(HttpStatusCode.OK, (await callback.PostAsync(webhook, Form(id))).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await host.Client.GetAsync($"/api/customer/orders/{orderId}")).StatusCode);
        await using (var scope = host.App.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<MyShopDbContext>();
            Assert.Equal(2, await db.Database.SqlQueryRaw<int>("SELECT [Status] AS [Value] FROM [Orders]").SingleAsync());
            Assert.NotNull(await db.Database.SqlQueryRaw<string>("SELECT [CustomerUserId] AS [Value] FROM [Orders]").SingleAsync());
            Assert.Equal(1, await db.Database.SqlQueryRaw<int>("SELECT COUNT(*) AS [Value] FROM [EmailMessages] WHERE [Subject] LIKE '%bestelbevestiging%'").SingleAsync());
            Assert.Equal(21m, await db.Database.SqlQueryRaw<decimal>("SELECT [VatRate] AS [Value] FROM [OrderLines]").SingleAsync());
        }
        await host.Csrf(); Assert.Equal(HttpStatusCode.NoContent, (await host.Login()).StatusCode); await host.Csrf();
        var order = await host.Client.GetFromJsonAsync<JsonElement>($"/api/orders/{orderId}");
        var invoiceResponse = await host.Client.PostAsJsonAsync($"/api/orders/{orderId}/invoice", new { orderRevision = order.GetProperty("revision").GetGuid(), supplyDate = DateOnly.FromDateTime(DateTime.UtcNow), buyer = new { name = "Ada", addressLine = "Buyer street 2", postalCode = "2345 BC", city = "Amsterdam", countryCode = "NL" }, taxReviewed = true, taxStatement = "" });
        Assert.Equal(HttpStatusCode.OK, invoiceResponse.StatusCode);
        var invoice = await invoiceResponse.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("29.15", invoice.GetProperty("gross").GetString());
        Assert.Equal("24.09", invoice.GetProperty("net").GetString());
        Assert.Equal("5.06", invoice.GetProperty("vat").GetString());
        await host.Csrf(); Assert.Equal(HttpStatusCode.NoContent, (await host.Client.PostAsJsonAsync("/api/customer/auth/login", new { email = "mollie-client@example.test", password = SecurityHost.Password })).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await host.Client.GetAsync($"/api/customer/orders/{orderId}/invoice")).StatusCode);
    }
    private static FormUrlEncodedContent Form(string id) => new(new Dictionary<string,string> { ["id"] = id });
    private sealed class MollieHandler : HttpMessageHandler
    {
        public string Status = "open";
        public string? AmountOverride;
        public int Starts;
        private JsonElement request;
        private const string Id = "tr_cycle123";
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage message, CancellationToken cancellationToken)
        {
            if (message.Method == HttpMethod.Post)
            {
                Starts++; request = JsonSerializer.Deserialize<JsonElement>(await message.Content!.ReadAsStringAsync(cancellationToken));
                Assert.Equal("https://api.mollie.com/v2/payments", message.RequestUri!.AbsoluteUri);
                Assert.True(message.Headers.Contains("Idempotency-Key"));
            }
            else Assert.Equal("https://api.mollie.com/v2/payments/" + Id, message.RequestUri!.AbsoluteUri);
            var amount = request.GetProperty("amount");
            var body = JsonSerializer.Serialize(new { id = Id, mode = "test", status = Status, amount = new { currency = amount.GetProperty("currency").GetString(), value = AmountOverride ?? amount.GetProperty("value").GetString() }, metadata = request.GetProperty("metadata"), _links = new { checkout = new { href = "https://www.mollie.com/checkout/test" } } });
            return new(HttpStatusCode.OK) { Content = new StringContent(body, Encoding.UTF8, "application/json") };
        }
    }
}
