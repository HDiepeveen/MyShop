using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using MyShop.Api.Tests.Security;
using MyShop.Infrastructure.Persistence;

namespace MyShop.Api.Tests.Catalog.Products;

public sealed class VariantCombinationsHttpTests
{
    [SecuritySqlFact]
    public async Task Batch_round_trip_preserves_existing_variants_is_atomic_and_checks_revision_and_auth()
    {
        await using var host = await SecurityHost.Create();
        var type = Guid.NewGuid(); var product = Guid.NewGuid(); var variant = Guid.NewGuid();
        var size = Guid.NewGuid(); var colour = Guid.NewGuid(); var revision = Guid.NewGuid();
        await using (var scope = host.App.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<MyShopDbContext>();
            await db.Database.ExecuteSqlInterpolatedAsync($"INSERT INTO ProductTypes (Id, Name) VALUES ({type}, {"Shirt"})");
            await db.Database.ExecuteSqlInterpolatedAsync($"INSERT INTO Products (Id, ProductTypeId, Name, Description, ImageUrl, ImageAlt, IsPublished, Version) VALUES ({product}, {type}, {"Shirt"}, {""}, {""}, {""}, {false}, {revision})");
            await db.Database.ExecuteSqlInterpolatedAsync($"INSERT INTO ProductVariants (Id, ProductId, Name, Ordinal, PriceAmount, PriceCurrency, VatExempt, StockQuantity, Sku) VALUES ({variant}, {product}, {"Existing"}, {0}, {42m}, {"EUR"}, {false}, {7}, {"SHIRT-EXISTING"})");
            await db.Database.ExecuteSqlInterpolatedAsync($"INSERT INTO AttributeDefinitions (Id, ProductTypeId, Code, DisplayName, DataType, Scope, IsRequired, IsFilterable) VALUES ({size}, {type}, {"size"}, {"Size"}, {5}, {1}, {true}, {true})");
            await db.Database.ExecuteSqlInterpolatedAsync($"INSERT INTO AttributeDefinitions (Id, ProductTypeId, Code, DisplayName, DataType, Scope, IsRequired, IsFilterable) VALUES ({colour}, {type}, {"colour"}, {"Colour"}, {0}, {1}, {true}, {true})");
        }
        var url = $"/api/products/{product}/variant-combinations";
        object Combination(string s, string c) => new { name = s + " / " + c, values = new object[] {
            new { attributeDefinitionId = size, value = new { dataType = 5, value = s } },
            new { attributeDefinitionId = colour, value = new { dataType = 0, value = c } } } };
        var combinations = new[] { Combination("M", "blue"), Combination("L", "blue") };
        await host.Csrf();
        Assert.Equal(HttpStatusCode.Unauthorized, (await host.Client.PostAsJsonAsync(url, new { revision, combinations })).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await host.Login()).StatusCode); await host.Csrf();
        var response = await host.Client.PostAsJsonAsync(url, new { revision, combinations, netAmount = 10, vatRate = 21, stockQuantity = 3 });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(2, (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("added").GetInt32());
        var detail = await host.Client.GetFromJsonAsync<JsonElement>($"/api/products/{product}");
        var variants = detail.GetProperty("variants");
        Assert.Equal(3, variants.GetArrayLength());
        var old = variants.EnumerateArray().Single(v => v.GetProperty("id").GetGuid() == variant);
        Assert.Equal(7, old.GetProperty("stockQuantity").GetInt32());
        Assert.Equal("SHIRT-EXISTING", old.GetProperty("sku").GetString());
        var fresh = variants.EnumerateArray().Single(v => v.GetProperty("name").GetString() == "L / blue");
        Assert.Equal("12.10", fresh.GetProperty("price").GetProperty("grossAmount").GetString());
        Assert.Equal(3, fresh.GetProperty("stockQuantity").GetInt32());
        Assert.Equal(2, fresh.GetProperty("attributeValues").GetArrayLength());
        Assert.Equal(HttpStatusCode.Conflict, (await host.Client.PostAsJsonAsync(url, new { revision, combinations })).StatusCode);
        var current = detail.GetProperty("revision").GetGuid();
        var retry = await host.Client.PostAsJsonAsync(url, new { revision = current, combinations, netAmount = 99, vatRate = 21, stockQuantity = 99 });
        Assert.Equal(HttpStatusCode.OK, retry.StatusCode);
        var result = await retry.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(0, result.GetProperty("added").GetInt32());
        Assert.Equal(2, result.GetProperty("skipped").GetInt32());
        var invalid = new object[] { Combination("XL", "blue"), new { name = "Invalid", values = new object[] {
            new { attributeDefinitionId = size, value = new { dataType = 1, value = 123 } } } } };
        Assert.Equal(HttpStatusCode.BadRequest, (await host.Client.PostAsJsonAsync(url, new { revision = current, combinations = invalid })).StatusCode);
        var after = await host.Client.GetFromJsonAsync<JsonElement>($"/api/products/{product}");
        Assert.Equal(current, after.GetProperty("revision").GetGuid());
        Assert.Equal(3, after.GetProperty("variants").GetArrayLength());
        Assert.Equal("12.10", after.GetProperty("variants").EnumerateArray().Single(v => v.GetProperty("name").GetString() == "L / blue").GetProperty("price").GetProperty("grossAmount").GetString());
    }
}
