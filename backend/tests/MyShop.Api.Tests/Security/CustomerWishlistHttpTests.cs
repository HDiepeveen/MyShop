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
    public async Task Wishlist_requires_customer_session()
    {
        await using var host = await SecurityHost.Create();
        Assert.Equal(HttpStatusCode.Unauthorized, (await host.Client.GetAsync("/api/customer/wishlist")).StatusCode);
    }

    private static async Task SeedProduct(SecurityHost host, Guid productId)
    {
        await using var scope = host.App.Services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<MyShopDbContext>();
        var typeId = Guid.NewGuid();
        var version = Guid.NewGuid();
        await context.Database.ExecuteSqlInterpolatedAsync(
            $"INSERT INTO ProductTypes (Id, Name) VALUES ({typeId}, {"Wishlist type"})");
        await context.Database.ExecuteSqlInterpolatedAsync(
            $"INSERT INTO Products (Id, ProductTypeId, Name, Description, ImageUrl, ImageAlt, IsPublished, Version) VALUES ({productId}, {typeId}, {"Saved product"}, {"Description"}, {null}, {"Saved product"}, {true}, {version})");
    }

    private static async Task SetPublished(SecurityHost host, Guid productId, bool published)
    {
        await using var scope = host.App.Services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<MyShopDbContext>();
        await context.Database.ExecuteSqlInterpolatedAsync(
            $"UPDATE Products SET IsPublished = {published} WHERE Id = {productId}");
    }
}
