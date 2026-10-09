using System.Net;
using System.Data;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.DependencyInjection;
using MyShop.Api.Tests.Security;
using MyShop.Infrastructure.Persistence;

namespace MyShop.Api.Tests.Storefront;

public sealed class StorefrontVariantOptionsHttpTests
{
    [SecuritySqlFact]
    public async Task Public_options_are_scoped_to_current_variant_definitions_and_preserve_numeric_precision()
    {
        await using var host = await SecurityHost.Create();
        var type = Guid.NewGuid(); var product = Guid.NewGuid(); var variant = Guid.NewGuid();
        var size = Guid.NewGuid(); var weight = Guid.NewGuid(); var number = Guid.NewGuid(); var hidden = Guid.NewGuid();
        await using (var scope = host.App.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<MyShopDbContext>();
            await db.Database.ExecuteSqlInterpolatedAsync($"INSERT INTO ProductTypes (Id, Name) VALUES ({type}, {"Options"})");
            await db.Database.ExecuteSqlInterpolatedAsync($"INSERT INTO Products (Id, ProductTypeId, Name, Description, ImageUrl, ImageAlt, IsPublished, Version) VALUES ({product}, {type}, {"Product"}, {"Description"}, {"https://example.test/image.jpg"}, {"Product"}, {true}, {Guid.NewGuid()})");
            await db.Database.ExecuteSqlInterpolatedAsync($"INSERT INTO ProductVariants (Id, ProductId, Name, Ordinal, VatExempt, StockQuantity, Sku) VALUES ({variant}, {product}, {"Variant"}, {0}, {false}, {0}, {"PRIVATE-SKU"})");
            await db.Database.ExecuteSqlInterpolatedAsync($"INSERT INTO AttributeDefinitions (Id, ProductTypeId, Code, DisplayName, DataType, Scope, IsRequired, IsFilterable) VALUES ({size}, {type}, {"size"}, {"Maat"}, {5}, {1}, {true}, {false})");
            await db.Database.ExecuteSqlInterpolatedAsync($"INSERT INTO AttributeDefinitions (Id, ProductTypeId, Code, DisplayName, DataType, Scope, IsRequired, IsFilterable) VALUES ({weight}, {type}, {"weight"}, {"Gewicht"}, {2}, {1}, {true}, {false})");
            await db.Database.ExecuteSqlInterpolatedAsync($"INSERT INTO AttributeDefinitions (Id, ProductTypeId, Code, DisplayName, DataType, Scope, IsRequired, IsFilterable) VALUES ({number}, {type}, {"number"}, {"Nummer"}, {1}, {1}, {true}, {false})");
            await db.Database.ExecuteSqlInterpolatedAsync($"INSERT INTO AttributeDefinitions (Id, ProductTypeId, Code, DisplayName, DataType, Scope, IsRequired, IsFilterable) VALUES ({hidden}, {type}, {"hidden"}, {"Internal"}, {0}, {0}, {false}, {false})");
            await db.Database.ExecuteSqlInterpolatedAsync($"INSERT INTO ProductVariantAttributeValues (ProductVariantId, AttributeDefinitionId, DataType, Ordinal, ChoiceValue) VALUES ({variant}, {size}, {5}, {0}, {"M"})");
            var coefficient = new SqlParameter("@coefficient", SqlDbType.Decimal)
            {
                Precision = 29, Scale = 0, Value = 12345678901234567890123456789m
            };
            await db.Database.ExecuteSqlInterpolatedAsync($"INSERT INTO ProductVariantAttributeValues (ProductVariantId, AttributeDefinitionId, DataType, Ordinal, DecimalCoefficient, DecimalScale) VALUES ({variant}, {weight}, {2}, {1}, {coefficient}, {28})");
            await db.Database.ExecuteSqlInterpolatedAsync($"INSERT INTO ProductVariantAttributeValues (ProductVariantId, AttributeDefinitionId, DataType, Ordinal, IntegerValue) VALUES ({variant}, {number}, {1}, {2}, {long.MaxValue})");
            await db.Database.ExecuteSqlInterpolatedAsync($"INSERT INTO ProductVariantAttributeValues (ProductVariantId, AttributeDefinitionId, DataType, Ordinal, TextValue) VALUES ({variant}, {hidden}, {0}, {3}, {"Private"})");
            await db.Database.ExecuteSqlInterpolatedAsync($"INSERT INTO ProductAttributeValues (ProductId, AttributeDefinitionId, DataType, Ordinal, TextValue) VALUES ({product}, {hidden}, {0}, {0}, {"Opel"})");
            // A variant-scoped value must not leak into the product characteristics.
            await db.Database.ExecuteSqlInterpolatedAsync($"INSERT INTO ProductAttributeValues (ProductId, AttributeDefinitionId, DataType, Ordinal, ChoiceValue) VALUES ({product}, {size}, {5}, {1}, {"Hidden product value"})");
        }
        var features = Guid.NewGuid();
        await using (var scope = host.App.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<MyShopDbContext>();
            await db.Database.ExecuteSqlInterpolatedAsync($"INSERT INTO AttributeDefinitions (Id, ProductTypeId, Code, DisplayName, DataType, Scope, IsRequired, IsFilterable) VALUES ({features}, {type}, {"features"}, {"Uitrusting"}, {6}, {0}, {false}, {false})");
            await db.Database.ExecuteSqlInterpolatedAsync($"INSERT INTO ProductAttributeValues (ProductId, AttributeDefinitionId, DataType, Ordinal) VALUES ({product}, {features}, {6}, {2})");
            await db.Database.ExecuteSqlInterpolatedAsync($"INSERT INTO ProductAttributeMultiChoiceValues (ProductId, AttributeDefinitionId, Ordinal, Value) VALUES ({product}, {features}, {1}, {"Radio"})");
            await db.Database.ExecuteSqlInterpolatedAsync($"INSERT INTO ProductAttributeMultiChoiceValues (ProductId, AttributeDefinitionId, Ordinal, Value) VALUES ({product}, {features}, {0}, {"Airco"})");
        }
        using var visitor = new HttpClient { BaseAddress = host.Client.BaseAddress };
        var response = await visitor.GetAsync($"/api/shop/products/{product}");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var detail = await response.Content.ReadFromJsonAsync<JsonElement>();
        var productAttributes = detail.GetProperty("attributes").EnumerateArray().ToArray();
        Assert.Equal(2, productAttributes.Length);
        var equipment = productAttributes[0];
        Assert.Equal("Uitrusting", equipment.GetProperty("name").GetString());
        Assert.Equal("Airco, Radio", equipment.GetProperty("value").GetString());
        var productAttribute = productAttributes[1];
        Assert.Equal(hidden, productAttribute.GetProperty("attributeDefinitionId").GetGuid());
        Assert.Equal("Internal", productAttribute.GetProperty("name").GetString());
        Assert.Equal("Opel", productAttribute.GetProperty("value").GetString());
        Assert.Equal(3, detail.GetProperty("variantDefinitions").GetArrayLength());
        Assert.DoesNotContain(detail.GetProperty("variantDefinitions").EnumerateArray(), d => d.GetProperty("id").GetGuid() == hidden);
        var publicVariant = Assert.Single(detail.GetProperty("variants").EnumerateArray());
        Assert.False(publicVariant.GetProperty("isAvailable").GetBoolean());
        Assert.False(publicVariant.TryGetProperty("sku", out _));
        Assert.False(publicVariant.TryGetProperty("stockQuantity", out _));
        var options = publicVariant.GetProperty("attributes").EnumerateArray().ToDictionary(
            v => v.GetProperty("attributeDefinitionId").GetGuid(), v => v.GetProperty("value").GetString());
        Assert.Equal(3, options.Count);
        Assert.Equal("M", options[size]);
        Assert.Equal("1.2345678901234567890123456789", options[weight]);
        Assert.Equal("9223372036854775807", options[number]);
        await using (var scope = host.App.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<MyShopDbContext>();
            await db.Database.ExecuteSqlInterpolatedAsync($"DELETE FROM AttributeDefinitions WHERE Id = {size}");
        }
        var updated = await visitor.GetFromJsonAsync<JsonElement>($"/api/shop/products/{product}");
        Assert.Equal(2, updated.GetProperty("variants")[0].GetProperty("attributes").GetArrayLength());
        await using (var scope = host.App.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<MyShopDbContext>();
            await db.Database.ExecuteSqlInterpolatedAsync($"UPDATE Products SET IsPublished = {false} WHERE Id = {product}");
        }
        Assert.Equal(HttpStatusCode.NotFound, (await visitor.GetAsync($"/api/shop/products/{product}")).StatusCode);
    }
}
