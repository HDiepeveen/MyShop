using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using MyShop.Infrastructure.Persistence;

namespace MyShop.Api.Tests.Security;

[Collection("SqlServer integration")]
public sealed class CustomerWishlistHttpTests
{
    [Fact]
    public async Task Customer_can_manage_own_wishlist_and_unpublished_item_remains_visible()
    {
        await using var host = await SecurityHost.Create();
        var productId = Guid.NewGuid();
        await SeedProduct(host, productId);
        await host.Csrf();
        Assert.Equal(HttpStatusCode.NoContent, (await host.Client.PostAsJsonAsync(
            "/api/customer/auth/register", new { email = "wishlist@example.com", password = SecurityHost.Password })).StatusCode);
        await host.Csrf();
        Assert.Equal(HttpStatusCode.Forbidden, (await host.Client.GetAsync("/api/products/export")).StatusCode);

        Assert.Equal(HttpStatusCode.NoContent,
            (await host.Client.PostAsJsonAsync($"/api/customer/wishlist/{productId}", new { })).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent,
            (await host.Client.PostAsJsonAsync($"/api/customer/wishlist/{productId}", new { })).StatusCode);

        var state = await host.Client.GetFromJsonAsync<JsonElement>($"/api/customer/wishlist/{productId}");
        Assert.True(state.GetProperty("saved").GetBoolean());
        var page = await host.Client.GetFromJsonAsync<JsonElement>("/api/customer/wishlist");
        Assert.Equal(1, page.GetProperty("totalCount").GetInt32());
        Assert.True(page.GetProperty("items")[0].GetProperty("isAvailable").GetBoolean());

        await SetPublished(host, productId, false);
        page = await host.Client.GetFromJsonAsync<JsonElement>("/api/customer/wishlist");
        Assert.False(page.GetProperty("items")[0].GetProperty("isAvailable").GetBoolean());

        Assert.Equal(HttpStatusCode.NoContent,
            (await host.Client.DeleteAsync($"/api/customer/wishlist/{productId}")).StatusCode);
        page = await host.Client.GetFromJsonAsync<JsonElement>("/api/customer/wishlist");
        Assert.Equal(0, page.GetProperty("totalCount").GetInt32());
    }

    [Fact]
    public async Task Wishlist_search_sort_and_paging_remain_customer_scoped()
    {
        await using var host = await SecurityHost.Create();
        var beta = Guid.NewGuid(); var alpha = Guid.NewGuid();
        await SeedProduct(host, beta, "Beta shirt"); await SeedProduct(host, alpha, "Alpha shirt");
        await host.Csrf(); Assert.Equal(HttpStatusCode.NoContent, (await host.Client.PostAsJsonAsync("/api/customer/auth/register", new { email = "browse-wishlist@example.com", password = SecurityHost.Password })).StatusCode); await host.Csrf();
        Assert.Equal(HttpStatusCode.NoContent, (await host.Client.PostAsJsonAsync($"/api/customer/wishlist/{beta}", new { })).StatusCode);
        await using (var scope = host.App.Services.CreateAsyncScope())
        {
            var database = scope.ServiceProvider.GetRequiredService<MyShopDbContext>();
            var past = new DateTimeOffset(2000, 1, 1, 0, 0, 0, TimeSpan.Zero);
            await database.Database.ExecuteSqlInterpolatedAsync($"UPDATE WishlistItems SET AddedAt = {past} WHERE ProductId = {beta}");
        }
        Assert.Equal(HttpStatusCode.NoContent, (await host.Client.PostAsJsonAsync($"/api/customer/wishlist/{alpha}", new { })).StatusCode);
        var newest = await host.Client.GetFromJsonAsync<JsonElement>("/api/customer/wishlist");
        Assert.Equal(alpha, newest.GetProperty("items")[0].GetProperty("productId").GetGuid());
        var names = await host.Client.GetFromJsonAsync<JsonElement>("/api/customer/wishlist?sort=name&search=shirt&limit=1&offset=1");
        Assert.Equal(2, names.GetProperty("totalCount").GetInt32());
        Assert.Equal(beta, Assert.Single(names.GetProperty("items").EnumerateArray()).GetProperty("productId").GetGuid());
        await SetPublished(host, beta, false);
        var hidden = await host.Client.GetFromJsonAsync<JsonElement>("/api/customer/wishlist?search=Beta&sort=name");
        Assert.Equal(1, hidden.GetProperty("totalCount").GetInt32()); Assert.False(hidden.GetProperty("items")[0].GetProperty("isAvailable").GetBoolean());
        var noMatch = await host.Client.GetFromJsonAsync<JsonElement>("/api/customer/wishlist?search=missing"); Assert.Equal(0, noMatch.GetProperty("totalCount").GetInt32());
        foreach (var query in new[] { "sort=unknown", "search=" + new string('x', 201) })
            Assert.Equal(HttpStatusCode.BadRequest, (await host.Client.GetAsync("/api/customer/wishlist?" + query)).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await host.Client.PostAsJsonAsync("/api/auth/logout", new { })).StatusCode); await host.Csrf();
        Assert.Equal(HttpStatusCode.NoContent, (await host.Client.PostAsJsonAsync("/api/customer/auth/register", new { email = "other-wishlist@example.com", password = SecurityHost.Password })).StatusCode); await host.Csrf();
        var other = await host.Client.GetFromJsonAsync<JsonElement>("/api/customer/wishlist?sort=name&search=shirt"); Assert.Equal(0, other.GetProperty("totalCount").GetInt32()); Assert.Empty(other.GetProperty("items").EnumerateArray());
    }
    [Fact]
    public async Task Wishlist_requires_customer_session()
    {
        await using var host = await SecurityHost.Create();
        Assert.Equal(HttpStatusCode.Unauthorized, (await host.Client.GetAsync("/api/customer/wishlist")).StatusCode);
    }

    private static async Task SeedProduct(SecurityHost host, Guid productId, string name = "Saved product")
    {
        await using var scope = host.App.Services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<MyShopDbContext>();
        var typeId = Guid.NewGuid();
        var version = Guid.NewGuid();
        await context.Database.ExecuteSqlInterpolatedAsync(
            $"INSERT INTO ProductTypes (Id, Name) VALUES ({typeId}, {"Wishlist type"})");
        await context.Database.ExecuteSqlInterpolatedAsync(
            $"INSERT INTO Products (Id, ProductTypeId, Name, Description, ImageUrl, ImageAlt, IsPublished, Version) VALUES ({productId}, {typeId}, {name}, {"Description"}, {null}, {"Saved product"}, {true}, {version})");
    }

    private static async Task SetPublished(SecurityHost host, Guid productId, bool published)
    {
        await using var scope = host.App.Services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<MyShopDbContext>();
        await context.Database.ExecuteSqlInterpolatedAsync(
            $"UPDATE Products SET IsPublished = {published} WHERE Id = {productId}");
    }
}
