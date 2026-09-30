using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Routing;
using MyShop.Api.Tests.Security;

namespace MyShop.Api.Tests.Storefront;

public sealed class StorefrontHttpTests
{
    [SecuritySqlFact]
    public async Task PublicReadsExposeOnlyPublishedProductsAndRespectWithdrawal()
    {
        await using var host = await SecurityHost.Create();
        using var visitor = new HttpClient(new HttpClientHandler { UseCookies = false, AllowAutoRedirect = false }) { BaseAddress = host.Client.BaseAddress };
        var publicEndpoints = ((IEndpointRouteBuilder)host.App).DataSources.SelectMany(s => s.Endpoints).OfType<RouteEndpoint>()
            .Where(e => e.RoutePattern.RawText!.StartsWith("/api/shop/")).ToArray();
        Assert.Equal(4, publicEndpoints.Length);
        Assert.All(publicEndpoints, e =>
        {
            Assert.Equal("GET", Assert.Single(e.Metadata.GetMetadata<HttpMethodMetadata>()!.HttpMethods));
            Assert.NotNull(e.Metadata.GetMetadata<IAllowAnonymous>());
        });
        await host.Csrf();
        Assert.Equal(HttpStatusCode.NoContent, (await host.Login()).StatusCode);
        await host.Csrf();
        var type = await (await host.Client.PostAsJsonAsync("/api/product-types", new { name = "Clothing" })).Content.ReadFromJsonAsync<JsonElement>();
        async Task<Guid> Create(string name, bool publish)
        {
            var created = await (await host.Client.PostAsJsonAsync("/api/products", new { productTypeId = type.GetProperty("id").GetGuid(), name, initialVariantName = "Small" })).Content.ReadFromJsonAsync<JsonElement>();
            var id = created.GetProperty("id").GetGuid();
            if (publish) Assert.Equal(HttpStatusCode.NoContent, (await host.Client.PutAsJsonAsync($"/api/products/{id}/presentation", new { description = "A comfortable shirt.", imageUrl = "https://example.com/shirt.jpg", imageAlt = "Linen shirt", isPublished = true, revision = created.GetProperty("revision").GetGuid() })).StatusCode);
            return id;
        }
        var alpha = await Create("Alpha", true);
        var beta = await Create("Beta", true);
        var draft = await Create("Onlydraft", false);
        Assert.Equal(HttpStatusCode.Created, (await host.Client.PostAsJsonAsync($"/api/products/{alpha}/variants", new { name = "Large" })).StatusCode);
        var pageResponse = await visitor.GetAsync("/api/shop/products?limit=1&offset=1");
        Assert.Equal(HttpStatusCode.OK, pageResponse.StatusCode);
        Assert.True(pageResponse.Headers.CacheControl?.NoStore);
        var page = await pageResponse.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(2, page.GetProperty("totalCount").GetInt32());
        var item = Assert.Single(page.GetProperty("items").EnumerateArray());
        Assert.Equal(beta, item.GetProperty("id").GetGuid());
        Assert.Equal(new[] { "id", "imageAlt", "imageUrl", "name" }, item.EnumerateObject().Select(p => p.Name).Order().ToArray());
        var product = await visitor.GetFromJsonAsync<JsonElement>($"/api/shop/products/{alpha}");
        Assert.Equal(new[] { "description", "id", "imageAlt", "imageUrl", "name", "variants" }, product.EnumerateObject().Select(p => p.Name).Order().ToArray());
        Assert.Equal(new[] { "Small", "Large" }, product.GetProperty("variants").EnumerateArray().Select(v => v.GetProperty("name").GetString()).ToArray());
        Assert.All(product.GetProperty("variants").EnumerateArray(), v => Assert.Equal(new[] { "id", "name" }, v.EnumerateObject().Select(p => p.Name).Order().ToArray()));
        foreach (var id in new[] { draft.ToString(), Guid.NewGuid().ToString(), Guid.Empty.ToString(), "invalid" })
            Assert.Equal(HttpStatusCode.NotFound, (await visitor.GetAsync($"/api/shop/products/{id}")).StatusCode);
        var variantId = product.GetProperty("variants")[0].GetProperty("id").GetGuid();
        Assert.Equal(HttpStatusCode.NoContent, (await host.Client.PutAsJsonAsync($"/api/products/{alpha}/variants/{variantId}/price", new { amount = 100m, currency = "EUR" })).StatusCode);
        Assert.Equal(HttpStatusCode.Created, (await host.Client.PostAsJsonAsync($"/api/products/{alpha}/variants/{variantId}/price-rules", new { name = "Current offer", adjustmentType = 1, value = 25m, priority = 0, startsAt = DateTimeOffset.UtcNow.AddDays(-1), endsAt = DateTimeOffset.UtcNow.AddDays(1) })).StatusCode);
        var before = DateTimeOffset.UtcNow;
        var pricesResponse = await visitor.GetAsync($"/api/shop/products/{alpha}/prices?at=2000-01-01T00:00:00Z");
        Assert.Equal(HttpStatusCode.OK, pricesResponse.StatusCode);
        Assert.True(pricesResponse.Headers.CacheControl?.NoStore);
        var prices = await pricesResponse.Content.ReadFromJsonAsync<JsonElement>();
        Assert.InRange(prices.GetProperty("at").GetDateTimeOffset(), before, DateTimeOffset.UtcNow);
        Assert.Equal(new[] { "at", "variants" }, prices.EnumerateObject().Select(p => p.Name).Order().ToArray());
        var priced = prices.GetProperty("variants")[0];
        Assert.Equal(new[] { "amount", "currency", "variantId" }, priced.EnumerateObject().Select(p => p.Name).Order().ToArray());
        Assert.Equal(75m, priced.GetProperty("amount").GetDecimal());
        Assert.Equal("EUR", priced.GetProperty("currency").GetString());
        Assert.Equal(JsonValueKind.Null, prices.GetProperty("variants")[1].GetProperty("amount").ValueKind);
        foreach (var id in new[] { draft.ToString(), Guid.NewGuid().ToString(), Guid.Empty.ToString(), "invalid" })
            Assert.Equal(HttpStatusCode.NotFound, (await visitor.GetAsync($"/api/shop/products/{id}/prices")).StatusCode);
        var quoteUrl = $"/api/shop/cart/quote?lines={alpha}:{variantId}:3";
        var quoteResponse = await visitor.GetAsync(quoteUrl + "&at=2000-01-01T00:00:00Z&amount=0");
        Assert.Equal(HttpStatusCode.OK, quoteResponse.StatusCode);
        Assert.True(quoteResponse.Headers.CacheControl?.NoStore);
        var quote = await quoteResponse.Content.ReadFromJsonAsync<JsonElement>();
        Assert.InRange(quote.GetProperty("at").GetDateTimeOffset(), before, DateTimeOffset.UtcNow);
        Assert.Equal(new[] { "at", "lines", "totals" }, quote.EnumerateObject().Select(p => p.Name).Order().ToArray());
        var quotedLine = Assert.Single(quote.GetProperty("lines").EnumerateArray());
        Assert.Equal(new[] { "amount", "currency", "failure", "name", "productId", "quantity", "total", "variant", "variantId" }, quotedLine.EnumerateObject().Select(p => p.Name).Order().ToArray());
        Assert.Equal("75.00", quotedLine.GetProperty("amount").GetString());
        Assert.Equal("225.00", quotedLine.GetProperty("total").GetString());
        Assert.Equal("225.00", Assert.Single(quote.GetProperty("totals").EnumerateArray()).GetProperty("amount").GetString());
        var unpricedId = product.GetProperty("variants")[1].GetProperty("id").GetGuid();
        foreach (var invalidLine in new[] { $"{draft}:{variantId}:1", $"{alpha}:{Guid.NewGuid()}:1", $"{alpha}:{unpricedId}:1" })
        {
            var incomplete = await visitor.GetFromJsonAsync<JsonElement>(quoteUrl + "&lines=" + invalidLine);
            Assert.Empty(incomplete.GetProperty("totals").EnumerateArray());
            Assert.Equal(JsonValueKind.Null, incomplete.GetProperty("lines")[1].GetProperty("amount").ValueKind);
        }
        foreach (var bad in new[] { "invalid", $"{alpha}:{variantId}:0", $"{alpha}:{variantId}:100", $"{Guid.Empty}:{variantId}:1", $"{alpha}:{variantId}:1.5", $"{alpha}:{variantId}:1&lines={alpha}:{variantId}:1", string.Join("&lines=", Enumerable.Repeat($"{alpha}:{variantId}:1", 21)) })
            Assert.Equal(HttpStatusCode.BadRequest, (await visitor.GetAsync("/api/shop/cart/quote?lines=" + bad)).StatusCode);
        var emptyQuote = await visitor.GetFromJsonAsync<JsonElement>("/api/shop/cart/quote");
        Assert.Empty(emptyQuote.GetProperty("lines").EnumerateArray());
        Assert.Equal(HttpStatusCode.NoContent, (await host.Client.PutAsJsonAsync($"/api/products/{alpha}/variants/{unpricedId}/price", new { amount = 9999999999999999.99m, currency = "EUR" })).StatusCode);
        var largeQuote = await visitor.GetFromJsonAsync<JsonElement>($"/api/shop/cart/quote?lines={alpha}:{unpricedId}:99");
        Assert.Equal("989999999999999999.01", largeQuote.GetProperty("totals")[0].GetProperty("amount").GetString());
        var hidden = await visitor.GetFromJsonAsync<JsonElement>("/api/shop/products?search=Onlydraft");
        Assert.Equal(0, hidden.GetProperty("totalCount").GetInt32());
        foreach (var query in new[] { "offset=-1", "limit=0", "limit=101", "offset=invalid", "search=" + new string('a', 201) })
            Assert.Equal(HttpStatusCode.BadRequest, (await visitor.GetAsync("/api/shop/products?" + query)).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await visitor.GetAsync($"/api/products/{alpha}")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await visitor.PostAsJsonAsync("/api/shop/products", new { name = "Injected" })).StatusCode);
        var stored = await host.Client.GetFromJsonAsync<JsonElement>($"/api/products/{alpha}");
        Assert.Equal(HttpStatusCode.NoContent, (await host.Client.PutAsJsonAsync($"/api/products/{alpha}/presentation", new { description = "Withdrawn", imageUrl = "https://example.com/shirt.jpg", imageAlt = "Linen shirt", isPublished = false, revision = stored.GetProperty("revision").GetGuid() })).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await visitor.GetAsync($"/api/shop/products/{alpha}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await visitor.GetAsync($"/api/shop/products/{alpha}/prices")).StatusCode);
        var withdrawnQuote = await visitor.GetFromJsonAsync<JsonElement>(quoteUrl);
        Assert.Empty(withdrawnQuote.GetProperty("totals").EnumerateArray());
        Assert.Equal(JsonValueKind.Null, withdrawnQuote.GetProperty("lines")[0].GetProperty("name").ValueKind);
        page = await visitor.GetFromJsonAsync<JsonElement>("/api/shop/products");
        Assert.Equal(1, page.GetProperty("totalCount").GetInt32());
    }
}
