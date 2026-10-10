using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.RateLimiting;
using MyShop.Api.Tests.Security;

namespace MyShop.Api.Tests.Storefront;

public sealed class StorefrontHttpTests
{
    [SecuritySqlFact]
    public async Task BrowsingSortsAndFiltersStockBeforeCountingAndPaging()
    {
        await using var host = await SecurityHost.Create();
        using var visitor = new HttpClient { BaseAddress = host.Client.BaseAddress };
        await host.Csrf(); Assert.Equal(HttpStatusCode.NoContent, (await host.Login()).StatusCode); await host.Csrf();
        var type = await (await host.Client.PostAsJsonAsync("/api/product-types", new { name = "Stock browsing" })).Content.ReadFromJsonAsync<JsonElement>();
        var category = await (await host.Client.PostAsJsonAsync("/api/categories", new { name = "Stock category" })).Content.ReadFromJsonAsync<JsonElement>();
        var categoryId = category.GetProperty("id").GetGuid();
        async Task<Guid> Create(string name, int? stock, bool published = true)
        {
            var created = await (await host.Client.PostAsJsonAsync("/api/products", new { productTypeId = type.GetProperty("id").GetGuid(), name, initialVariantName = "Standard" })).Content.ReadFromJsonAsync<JsonElement>();
            var id = created.GetProperty("id").GetGuid();
            if (stock is not null)
            {
                var detail = await host.Client.GetFromJsonAsync<JsonElement>($"/api/products/{id}");
                var variant = detail.GetProperty("variants")[0].GetProperty("id").GetGuid();
                Assert.Equal(HttpStatusCode.NoContent, (await host.Client.PutAsJsonAsync($"/api/products/{id}/variants/{variant}/stock", new { quantity = stock.Value })).StatusCode);
            }
            var current = await host.Client.GetFromJsonAsync<JsonElement>($"/api/products/{id}");
            if (published) Assert.Equal(HttpStatusCode.NoContent, (await host.Client.PutAsJsonAsync($"/api/products/{id}/presentation", new { description = "Stock test", imageUrl = "https://example.com/stock.jpg", imageAlt = "Stock", isPublished = true, revision = current.GetProperty("revision").GetGuid() })).StatusCode);
            Assert.Equal(HttpStatusCode.NoContent, (await host.Client.PutAsync($"/api/products/{id}/categories/{categoryId}", null)).StatusCode);
            return id;
        }
        var unavailable = await Create("Alpha", 0);
        var tracked = await Create("Beta", 1);
        var unlimited = await Create("Gamma", null);
        var draft = await Create("Zeta draft", null, false);
        var managedPublished = await host.Client.GetFromJsonAsync<JsonElement>($"/api/products?published=true&categoryId={categoryId}&productTypeId={type.GetProperty("id").GetGuid()}&limit=1&offset=1");
        Assert.Equal(3, managedPublished.GetProperty("totalCount").GetInt32());
        Assert.Equal(tracked, Assert.Single(managedPublished.GetProperty("items").EnumerateArray()).GetProperty("id").GetGuid());
        using var exportResponse = await host.Client.GetAsync($"/api/products/export?published=false&search=Zeta&categoryId={categoryId}");
        Assert.Equal(HttpStatusCode.OK, exportResponse.StatusCode); Assert.Equal("text/csv", exportResponse.Content.Headers.ContentType!.MediaType);
        var exportCsv = await exportResponse.Content.ReadAsStringAsync(); Assert.Contains(draft.ToString(), exportCsv); Assert.Contains("Zeta draft", exportCsv); Assert.DoesNotContain(tracked.ToString(), exportCsv);
        Assert.Equal(HttpStatusCode.Unauthorized, (await visitor.GetAsync("/api/products/export")).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await host.Client.GetAsync("/api/products/export?stock=invalid")).StatusCode);
        var managedDrafts = await host.Client.GetFromJsonAsync<JsonElement>($"/api/products?published=false&search=Zeta&categoryId={categoryId}");
        Assert.Equal(1, managedDrafts.GetProperty("totalCount").GetInt32());
        Assert.Equal(draft, Assert.Single(managedDrafts.GetProperty("items").EnumerateArray()).GetProperty("id").GetGuid());
        var lowStock = await host.Client.GetFromJsonAsync<JsonElement>($"/api/products?published=true&stock=low&categoryId={categoryId}&productTypeId={type.GetProperty("id").GetGuid()}&limit=1&offset=1");
        Assert.Equal(2, lowStock.GetProperty("totalCount").GetInt32());
        Assert.Equal(tracked, Assert.Single(lowStock.GetProperty("items").EnumerateArray()).GetProperty("id").GetGuid());
        var managedTracked = await host.Client.GetFromJsonAsync<JsonElement>($"/api/products/{tracked}");
        var trackedVariant = managedTracked.GetProperty("variants")[0].GetProperty("id").GetGuid();
        Assert.Equal(HttpStatusCode.NoContent, (await host.Client.PutAsJsonAsync($"/api/products/{tracked}/variants/{trackedVariant}/stock", new { quantity = 5 })).StatusCode);
        Assert.Equal(1, (await host.Client.GetFromJsonAsync<JsonElement>("/api/products?stock=low&search=Beta")).GetProperty("totalCount").GetInt32());
        Assert.Equal(HttpStatusCode.NoContent, (await host.Client.PutAsJsonAsync($"/api/products/{tracked}/variants/{trackedVariant}/stock", new { quantity = 6 })).StatusCode);
        Assert.Equal(0, (await host.Client.GetFromJsonAsync<JsonElement>("/api/products?stock=low&search=Beta")).GetProperty("totalCount").GetInt32());
        var outOfStock = await host.Client.GetFromJsonAsync<JsonElement>("/api/products?stock=out&search=Alpha");
        Assert.Equal(unavailable, Assert.Single(outOfStock.GetProperty("items").EnumerateArray()).GetProperty("id").GetGuid());
        var untracked = await host.Client.GetFromJsonAsync<JsonElement>("/api/products?stock=untracked&published=true");
        Assert.Equal(unlimited, Assert.Single(untracked.GetProperty("items").EnumerateArray()).GetProperty("id").GetGuid());
        var untrackedDraft = await host.Client.GetFromJsonAsync<JsonElement>("/api/products?stock=untracked&published=false");
        Assert.Equal(draft, Assert.Single(untrackedDraft.GetProperty("items").EnumerateArray()).GetProperty("id").GetGuid());
        var draftPage = await host.Client.GetFromJsonAsync<JsonElement>("/api/products?published=false&offset=1&limit=1");
        Assert.Equal(1, draftPage.GetProperty("totalCount").GetInt32()); Assert.Empty(draftPage.GetProperty("items").EnumerateArray());
        var descending = await visitor.GetFromJsonAsync<JsonElement>("/api/shop/products?sort=nameDesc");
        Assert.Equal(new[] { unlimited, tracked, unavailable }, descending.GetProperty("items").EnumerateArray().Select(item => item.GetProperty("id").GetGuid()).ToArray());
        var first = await visitor.GetFromJsonAsync<JsonElement>($"/api/shop/products?availableOnly=true&sort=nameDesc&categoryId={categoryId}&limit=1");
        Assert.Equal(2, first.GetProperty("totalCount").GetInt32());
        Assert.Equal(unlimited, Assert.Single(first.GetProperty("items").EnumerateArray()).GetProperty("id").GetGuid());
        var second = await visitor.GetFromJsonAsync<JsonElement>($"/api/shop/products?availableOnly=true&sort=nameDesc&categoryId={categoryId}&limit=1&offset=1");
        Assert.Equal(tracked, Assert.Single(second.GetProperty("items").EnumerateArray()).GetProperty("id").GetGuid());
        var missing = await visitor.GetFromJsonAsync<JsonElement>("/api/shop/products?availableOnly=true&search=Alpha");
        Assert.Equal(0, missing.GetProperty("totalCount").GetInt32());
        var defaultPage = await visitor.GetFromJsonAsync<JsonElement>("/api/shop/products?availableOnly=false&sort=nameAsc");
        Assert.Equal(3, defaultPage.GetProperty("totalCount").GetInt32());
        Assert.Equal(unavailable, defaultPage.GetProperty("items")[0].GetProperty("id").GetGuid());
        Assert.Equal(HttpStatusCode.Created, (await host.Client.PostAsJsonAsync($"/api/products/{unavailable}/variants", new { name = "Unlimited variant" })).StatusCode);
        var mixed = await visitor.GetFromJsonAsync<JsonElement>("/api/shop/products?availableOnly=true&search=Alpha");
        Assert.Equal(1, mixed.GetProperty("totalCount").GetInt32());
        Assert.Equal(1, (await host.Client.GetFromJsonAsync<JsonElement>("/api/products?stock=out&search=Alpha")).GetProperty("totalCount").GetInt32());
        Assert.Equal(1, (await host.Client.GetFromJsonAsync<JsonElement>("/api/products?stock=untracked&search=Alpha")).GetProperty("totalCount").GetInt32());
        var sameName = await Create("Beta", null);
        var betaPage = await visitor.GetFromJsonAsync<JsonElement>("/api/shop/products?sort=nameDesc&search=Beta");
        var betaIds = betaPage.GetProperty("items").EnumerateArray().Select(item => item.GetProperty("id").GetGuid()).ToArray();
        Assert.Equal(2, betaIds.Length); Assert.Contains(tracked, betaIds); Assert.Contains(sameName, betaIds);
        for (var offset = 0; offset < 2; offset++)
        {
            var tied = await visitor.GetFromJsonAsync<JsonElement>($"/api/shop/products?sort=nameDesc&search=Beta&limit=1&offset={offset}");
            Assert.Equal(betaIds[offset], Assert.Single(tied.GetProperty("items").EnumerateArray()).GetProperty("id").GetGuid());
        }
        foreach (var query in new[] { "sort=price", "sort=NAMEASC", "availableOnly=invalid" })
            Assert.Equal(HttpStatusCode.BadRequest, (await visitor.GetAsync("/api/shop/products?" + query)).StatusCode);
    }
    [SecuritySqlFact]
    public async Task PublicReadsExposeOnlyPublishedProductsAndRespectWithdrawal()
    {
        await using var host = await SecurityHost.Create();
        using var visitor = new HttpClient(new HttpClientHandler { UseCookies = false, AllowAutoRedirect = false }) { BaseAddress = host.Client.BaseAddress };
        var publicEndpoints = ((IEndpointRouteBuilder)host.App).DataSources.SelectMany(s => s.Endpoints).OfType<RouteEndpoint>()
            .Where(e => e.RoutePattern.RawText!.StartsWith("/api/shop/")).ToArray();
        Assert.Equal(11, publicEndpoints.Length);
        Assert.All(publicEndpoints, e =>
        {
            Assert.Equal(e.RoutePattern.RawText is "/api/shop/orders" or "/api/shop/online-payments" ? "POST" : "GET",
                Assert.Single(e.Metadata.GetMetadata<HttpMethodMetadata>()!.HttpMethods));
            Assert.NotNull(e.Metadata.GetMetadata<IAllowAnonymous>());
            if (e.RoutePattern.RawText is "/api/shop/orders" or "/api/shop/online-payments")
                Assert.Equal("storefront-order",
                    e.Metadata.GetMetadata<EnableRateLimitingAttribute>()!.PolicyName);
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
        var clothing = await (await host.Client.PostAsJsonAsync("/api/categories", new { name = "Clothing" }))
            .Content.ReadFromJsonAsync<JsonElement>();
        var drafts = await (await host.Client.PostAsJsonAsync("/api/categories", new { name = "Drafts" }))
            .Content.ReadFromJsonAsync<JsonElement>();
        var clothingId = clothing.GetProperty("id").GetGuid();
        var draftsId = drafts.GetProperty("id").GetGuid();
        Assert.Equal(HttpStatusCode.NoContent,
            (await host.Client.PutAsync($"/api/products/{alpha}/categories/{clothingId}", null)).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent,
            (await host.Client.PutAsync($"/api/products/{draft}/categories/{draftsId}", null)).StatusCode);
        var categoriesResponse = await visitor.GetAsync("/api/shop/categories");
        Assert.True(categoriesResponse.Headers.CacheControl?.NoStore);
        var categories = await categoriesResponse.Content.ReadFromJsonAsync<JsonElement>();
        var category = Assert.Single(categories.EnumerateArray());
        Assert.Equal(new[] { "id", "name" }, category.EnumerateObject().Select(property => property.Name)
            .Order().ToArray());
        Assert.Equal(clothingId, category.GetProperty("id").GetGuid());
        Assert.Equal("Clothing", category.GetProperty("name").GetString());
        var categoryPage = await visitor.GetFromJsonAsync<JsonElement>(
            $"/api/shop/products?categoryId={clothingId}");
        Assert.Equal(alpha, Assert.Single(categoryPage.GetProperty("items").EnumerateArray())
            .GetProperty("id").GetGuid());
        Assert.Equal(0, (await visitor.GetFromJsonAsync<JsonElement>(
            $"/api/shop/products?categoryId={draftsId}")).GetProperty("totalCount").GetInt32());
        Assert.Equal(0, (await visitor.GetFromJsonAsync<JsonElement>(
            $"/api/shop/products?categoryId={Guid.NewGuid()}")).GetProperty("totalCount").GetInt32());
        Assert.Equal(HttpStatusCode.Created, (await host.Client.PostAsJsonAsync($"/api/products/{alpha}/variants", new { name = "Large" })).StatusCode);
        var pageRequestedAt = DateTimeOffset.UtcNow;
        var pageResponse = await visitor.GetAsync("/api/shop/products?limit=1&offset=1");
        Assert.Equal(HttpStatusCode.OK, pageResponse.StatusCode);
        Assert.True(pageResponse.Headers.CacheControl?.NoStore);
        var page = await pageResponse.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(new[] { "at", "heading", "items", "limit", "offset", "seoTitle", "totalCount" }, page.EnumerateObject()
            .Select(property => property.Name).Order().ToArray());
        Assert.InRange(page.GetProperty("at").GetDateTimeOffset(), pageRequestedAt, DateTimeOffset.UtcNow);
        Assert.Equal(2, page.GetProperty("totalCount").GetInt32());
        var item = Assert.Single(page.GetProperty("items").EnumerateArray());
        Assert.Equal(beta, item.GetProperty("id").GetGuid());
        Assert.Equal(new[] { "id", "imageAlt", "imageUrl", "isAvailable", "name", "prices", "webAddress" }, item.EnumerateObject().Select(p => p.Name).Order().ToArray());
        Assert.True(item.GetProperty("isAvailable").GetBoolean());
        Assert.Empty(item.GetProperty("prices").EnumerateArray());
        var product = await visitor.GetFromJsonAsync<JsonElement>($"/api/shop/products/{alpha}");
        Assert.Equal(new[] { "attributes", "categories", "checkoutEnabled", "description", "id", "imageAlt", "images", "imageUrl", "name", "seoDescription", "seoTitle", "variantDefinitions", "variants", "webAddress" }, product.EnumerateObject().Select(p => p.Name).Order().ToArray());
        var productCategory = Assert.Single(product.GetProperty("categories").EnumerateArray());
        Assert.Equal(new[] { "id", "name" }, productCategory.EnumerateObject()
            .Select(property => property.Name).Order().ToArray());
        Assert.Equal(clothingId, productCategory.GetProperty("id").GetGuid());
        Assert.Equal("Clothing", productCategory.GetProperty("name").GetString());
        Assert.Equal(new[] { "Small", "Large" }, product.GetProperty("variants").EnumerateArray().Select(v => v.GetProperty("name").GetString()).ToArray());
        Assert.All(product.GetProperty("variants").EnumerateArray(), v => Assert.Equal(new[] { "attributes", "id", "isAvailable", "name" }, v.EnumerateObject().Select(p => p.Name).Order().ToArray()));
        foreach (var id in new[] { draft.ToString(), Guid.NewGuid().ToString(), Guid.Empty.ToString(), "invalid" })
            Assert.Equal(HttpStatusCode.NotFound, (await visitor.GetAsync($"/api/shop/products/{id}")).StatusCode);
        var variantId = product.GetProperty("variants")[0].GetProperty("id").GetGuid();
        Assert.Equal(HttpStatusCode.NoContent, (await host.Client.PutAsJsonAsync($"/api/products/{alpha}/variants/{variantId}/price", new { amount = 100m, currency = "EUR" })).StatusCode);
        Assert.Equal(HttpStatusCode.Created, (await host.Client.PostAsJsonAsync($"/api/products/{alpha}/variants/{variantId}/price-rules", new { name = "Current offer", adjustmentType = 1, value = 25m, priority = 0, startsAt = DateTimeOffset.UtcNow.AddDays(-1), endsAt = DateTimeOffset.UtcNow.AddDays(1) })).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await host.Client.PutAsJsonAsync(
            $"/api/products/{alpha}/variants/{variantId}/stock", new { quantity = 3 })).StatusCode);
        var managedWithStock = await host.Client.GetFromJsonAsync<JsonElement>($"/api/products/{alpha}");
        Assert.Equal(3, managedWithStock.GetProperty("variants")[0].GetProperty("stockQuantity").GetInt32());
        var before = DateTimeOffset.UtcNow;
        var pricesResponse = await visitor.GetAsync($"/api/shop/products/{alpha}/prices?at=2000-01-01T00:00:00Z");
        Assert.Equal(HttpStatusCode.OK, pricesResponse.StatusCode);
        Assert.True(pricesResponse.Headers.CacheControl?.NoStore);
        var prices = await pricesResponse.Content.ReadFromJsonAsync<JsonElement>();
        Assert.InRange(prices.GetProperty("at").GetDateTimeOffset(), before, DateTimeOffset.UtcNow);
        Assert.Equal(new[] { "at", "variants" }, prices.EnumerateObject().Select(p => p.Name).Order().ToArray());
        var priced = prices.GetProperty("variants")[0];
        Assert.Equal(new[] { "amount", "currency", "variantId" }, priced.EnumerateObject().Select(p => p.Name).Order().ToArray());
        Assert.Equal("75.00", priced.GetProperty("amount").GetString());
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
        var shortStock = await visitor.GetFromJsonAsync<JsonElement>(
            $"/api/shop/cart/quote?lines={alpha}:{variantId}:4");
        Assert.Equal("outOfStock", shortStock.GetProperty("lines")[0].GetProperty("failure").GetString());
        Assert.Empty(shortStock.GetProperty("totals").EnumerateArray());
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
        var exactPrices = await visitor.GetFromJsonAsync<JsonElement>($"/api/shop/products/{alpha}/prices");
        Assert.Equal("9999999999999999.99", exactPrices.GetProperty("variants")[1]
            .GetProperty("amount").GetString());
        var largeQuote = await visitor.GetFromJsonAsync<JsonElement>($"/api/shop/cart/quote?lines={alpha}:{unpricedId}:99");
        Assert.Equal("989999999999999999.01", largeQuote.GetProperty("totals")[0].GetProperty("amount").GetString());
        var pricedPage = await visitor.GetFromJsonAsync<JsonElement>("/api/shop/products?search=Alpha");
        var range = Assert.Single(Assert.Single(pricedPage.GetProperty("items").EnumerateArray())
            .GetProperty("prices").EnumerateArray());
        Assert.Equal(new[] { "currency", "maximumAmount", "minimumAmount" }, range.EnumerateObject()
            .Select(property => property.Name).Order().ToArray());
        Assert.Equal("EUR", range.GetProperty("currency").GetString());
        Assert.Equal("75.00", range.GetProperty("minimumAmount").GetString());
        Assert.Equal("9999999999999999.99", range.GetProperty("maximumAmount").GetString());
        Assert.Equal(HttpStatusCode.NoContent, (await host.Client.PutAsJsonAsync(
            $"/api/products/{alpha}/variants/{variantId}/stock", new { quantity = 0 })).StatusCode);
        var unavailableProduct = await visitor.GetFromJsonAsync<JsonElement>($"/api/shop/products/{alpha}");
        Assert.False(unavailableProduct.GetProperty("variants")[0].GetProperty("isAvailable").GetBoolean());
        Assert.Equal(HttpStatusCode.NoContent,
            (await host.Client.DeleteAsync($"/api/products/{alpha}/variants/{variantId}/stock")).StatusCode);
        var untrackedProduct = await host.Client.GetFromJsonAsync<JsonElement>($"/api/products/{alpha}");
        Assert.Equal(JsonValueKind.Null,
            untrackedProduct.GetProperty("variants")[0].GetProperty("stockQuantity").ValueKind);
        var hidden = await visitor.GetFromJsonAsync<JsonElement>("/api/shop/products?search=Onlydraft");
        Assert.Equal(0, hidden.GetProperty("totalCount").GetInt32());
        foreach (var query in new[] { "offset=-1", "limit=0", "limit=101", "offset=invalid",
                     "categoryId=invalid", $"categoryId={Guid.Empty}", "search=" + new string('a', 201) })
            Assert.Equal(HttpStatusCode.BadRequest, (await visitor.GetAsync("/api/shop/products?" + query)).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await visitor.GetAsync($"/api/products/{alpha}")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await visitor.PostAsJsonAsync("/api/shop/products", new { name = "Injected" })).StatusCode);
        var stored = await host.Client.GetFromJsonAsync<JsonElement>($"/api/products/{alpha}");
        Assert.Equal(HttpStatusCode.NoContent, (await host.Client.PutAsJsonAsync($"/api/products/{alpha}/presentation", new { description = "Withdrawn", imageUrl = "https://example.com/shirt.jpg", imageAlt = "Linen shirt", isPublished = false, revision = stored.GetProperty("revision").GetGuid() })).StatusCode);
        Assert.Empty((await visitor.GetFromJsonAsync<JsonElement>("/api/shop/categories")).EnumerateArray());
        Assert.Equal(HttpStatusCode.NotFound, (await visitor.GetAsync($"/api/shop/products/{alpha}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await visitor.GetAsync($"/api/shop/products/{alpha}/prices")).StatusCode);
        var withdrawnQuote = await visitor.GetFromJsonAsync<JsonElement>(quoteUrl);
        Assert.Empty(withdrawnQuote.GetProperty("totals").EnumerateArray());
        Assert.Equal(JsonValueKind.Null, withdrawnQuote.GetProperty("lines")[0].GetProperty("name").ValueKind);
        page = await visitor.GetFromJsonAsync<JsonElement>("/api/shop/products");
        Assert.Equal(1, page.GetProperty("totalCount").GetInt32());
    }
}
