using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using MyShop.Infrastructure.Persistence;
using MyShop.Api.Tests.Security;
namespace MyShop.Api.Tests.Catalog.Products;
public sealed class VariantVatHttpTests
{
    [SecuritySqlFact]
    public async Task Net_input_is_saved_as_gross_and_public_prices_remain_totals_only()
    {
        await using var host = await SecurityHost.Create();
        var typeId = Guid.NewGuid(); var productId = Guid.NewGuid(); var variantId = Guid.NewGuid(); var version = Guid.NewGuid();
        await using (var scope = host.App.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<MyShopDbContext>();
            await db.Database.ExecuteSqlInterpolatedAsync($"INSERT INTO ProductTypes (Id, Name) VALUES ({typeId}, {"VAT type"})");
            await db.Database.ExecuteSqlInterpolatedAsync($"INSERT INTO Products (Id, ProductTypeId, Name, Description, ImageUrl, ImageAlt, IsPublished, Version) VALUES ({productId}, {typeId}, {"VAT product"}, {"Description"}, {"https://example.test/product.png"}, {"VAT product"}, {true}, {version})");
            await db.Database.ExecuteSqlInterpolatedAsync($"INSERT INTO ProductVariants (Id, ProductId, Name, Ordinal, PriceAmount, PriceCurrency, VatExempt) VALUES ({variantId}, {productId}, {"Variant"}, {0}, {121m}, {"EUR"}, {false})");
        }
        await host.Csrf(); Assert.Equal(HttpStatusCode.NoContent, (await host.Login()).StatusCode); await host.Csrf();
        var legacy = await host.Client.GetFromJsonAsync<JsonElement>($"/api/products/{productId}");
        Assert.Equal(JsonValueKind.Null, legacy.GetProperty("variants")[0].GetProperty("price").GetProperty("vatRate").ValueKind);
        Assert.Equal(HttpStatusCode.NoContent, (await host.Client.PutAsJsonAsync($"/api/products/{productId}/variants/{variantId}/price", new { netAmount = 100, currency = "EUR", vatRate = 21 })).StatusCode);
        var detail = await host.Client.GetFromJsonAsync<JsonElement>($"/api/products/{productId}");
        var price = detail.GetProperty("variants")[0].GetProperty("price");
        Assert.Equal("100.00", price.GetProperty("netAmount").GetString());
        Assert.Equal("21.00", price.GetProperty("vatAmount").GetString());
        Assert.Equal("121.00", price.GetProperty("grossAmount").GetString());
        var shop = await host.Client.GetFromJsonAsync<JsonElement>($"/api/shop/products/{productId}/prices");
        var publicPrice = shop.GetProperty("variants")[0];
        Assert.Equal("121.00", publicPrice.GetProperty("amount").GetString());
        Assert.False(publicPrice.TryGetProperty("vatRate", out _));
        Assert.False(publicPrice.TryGetProperty("netAmount", out _));
        Assert.Equal(HttpStatusCode.BadRequest, (await host.Client.PutAsJsonAsync($"/api/products/{productId}/variants/{variantId}/price", new { netAmount = 100, currency = "EUR", vatRate = 21, vatExempt = true })).StatusCode);
    }
}
