using MyShop.Domain.Catalog;

namespace MyShop.Api.Tests.Catalog.ProductTypes;

public sealed class ProductTypeAttributeReadHttpTests
{
    [Theory]
    [InlineData(0, "Product")]
    [InlineData(1, "Variant")]
    public async Task CreatedLocationIdentifiesReadableEditableAndRemovableDefinition(int scope, string expectedScope)
    {
        await using var scenario = new ProductTypeManagementScenario();
        var type = ProductType.Create("Type");
        scenario.Repository.Items.Add(type.Id, type);
        var created = await scenario.Http.Send("AddProductTypeAttribute",
            new { code = "color", displayName = "Color", dataType = 5, scope, isRequired = false, isFilterable = true },
            type.Id.Value);
        Assert.Equal(201, created.Response.StatusCode);
        var location = created.Response.Headers.Location.ToString().Split('/');
        Assert.Equal(new[] { "", "api", "product-types", type.Id.Value.ToString(), "attributes" }, location[..^1]);
        var attributeId = Guid.Parse(location[^1]);
        var read = await scenario.Http.Send("GetProductTypeAttribute", id: type.Id.Value, attributeId: attributeId);
        Assert.Equal(200, read.Response.StatusCode);
        using (var json = await CatalogManagementHttp.Read(read))
        {
            Assert.Equal(attributeId, json.RootElement.GetProperty("id").GetGuid());
            Assert.Equal("Color", json.RootElement.GetProperty("displayName").GetString());
            Assert.Equal("Choice", json.RootElement.GetProperty("dataType").GetString());
            Assert.Equal(expectedScope, json.RootElement.GetProperty("scope").GetString());
            Assert.True(json.RootElement.GetProperty("isFilterable").GetBoolean());
            Assert.False(json.RootElement.GetProperty("isRequired").GetBoolean());
        }
        Assert.Equal(1, scenario.Repository.Saves);
        Assert.Equal(204, (await scenario.Http.Send("RenameProductTypeAttribute",
            new { displayName = "Colour" }, type.Id.Value, attributeId)).Response.StatusCode);
        using (var json = await CatalogManagementHttp.Read(
            await scenario.Http.Send("GetProductTypeAttribute", id: type.Id.Value, attributeId: attributeId)))
            Assert.Equal("Colour", json.RootElement.GetProperty("displayName").GetString());
        Assert.Equal(2, scenario.Repository.Saves);
        Assert.Equal(204, (await scenario.Http.Send("RemoveProductTypeAttribute",
            id: type.Id.Value, attributeId: attributeId)).Response.StatusCode);
        Assert.Equal(404, (await scenario.Http.Send("GetProductTypeAttribute",
            id: type.Id.Value, attributeId: attributeId)).Response.StatusCode);
        Assert.Equal(3, scenario.Repository.Saves);
    }

    [Fact]
    public async Task AttributeFromAnotherTypeCannotBeRead()
    {
        await using var scenario = new ProductTypeManagementScenario();
        var owner = ProductType.Create("Owner");
        var other = ProductType.Create("Other");
        var attribute = owner.AddAttribute(AttributeDefinitionId.New(), AttributeCode.Create("value"), "Value",
            AttributeDataType.Text, false, false, AttributeScope.Product);
        scenario.Repository.Items.Add(owner.Id, owner);
        scenario.Repository.Items.Add(other.Id, other);
        var response = await scenario.Http.Send("GetProductTypeAttribute",
            id: other.Id.Value, attributeId: attribute.Id.Value);
        Assert.Equal(404, response.Response.StatusCode);
        using var json = await CatalogManagementHttp.Read(response);
        Assert.Equal("Attribute not found", json.RootElement.GetProperty("title").GetString());
        Assert.Equal(0, scenario.Repository.Saves);
    }
}
