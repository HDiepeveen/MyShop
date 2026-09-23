using MyShop.Domain.Catalog;

namespace MyShop.Api.Tests.Catalog.ProductTypes;

public sealed class ProductTypeAttributeRemovalHttpTests
{
    [Fact]
    public async Task RemoveThenRead_PreservesOtherDefinitionAndReturnsNotFoundOnRepeat()
    {
        await using var scenario = new ProductTypeManagementScenario();
        var type = ProductType.Create("Type");
        var removed = type.AddAttribute(AttributeDefinitionId.New(), AttributeCode.Create("color"), "Color",
            AttributeDataType.Text, false, true, AttributeScope.Product);
        var retained = type.AddAttribute(AttributeDefinitionId.New(), AttributeCode.Create("size"), "Size",
            AttributeDataType.Choice, true, true, AttributeScope.Variant);
        scenario.Repository.Items.Add(type.Id, type);
        Assert.Equal(204, (await scenario.Http.Send("RemoveProductTypeAttribute",
            id: type.Id.Value, attributeId: removed.Id.Value)).Response.StatusCode);
        var read = await scenario.Http.Send("GetProductType", id: type.Id.Value);
        Assert.Equal(200, read.Response.StatusCode);
        using var json = await CatalogManagementHttp.Read(read);
        var definition = Assert.Single(json.RootElement.GetProperty("attributeDefinitions").EnumerateArray());
        Assert.Equal(retained.Id.Value, definition.GetProperty("id").GetGuid());
        Assert.Equal("size", definition.GetProperty("code").GetString());
        Assert.Same(retained, Assert.Single(type.AttributeDefinitions));
        Assert.Equal(404, (await scenario.Http.Send("RemoveProductTypeAttribute",
            id: type.Id.Value, attributeId: removed.Id.Value)).Response.StatusCode);
        Assert.Equal(1, scenario.Repository.Saves);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task WrongOrMissingType_DoesNotRemoveAnotherTypesDefinition(bool missing)
    {
        await using var scenario = new ProductTypeManagementScenario();
        var owner = ProductType.Create("Owner");
        var other = ProductType.Create("Other");
        var attribute = owner.AddAttribute(AttributeDefinitionId.New(), AttributeCode.Create("color"), "Color",
            AttributeDataType.Text, false, false, AttributeScope.Product);
        scenario.Repository.Items.Add(owner.Id, owner);
        scenario.Repository.Items.Add(other.Id, other);
        var response = await scenario.Http.Send("RemoveProductTypeAttribute",
            id: missing ? Guid.NewGuid() : other.Id.Value, attributeId: attribute.Id.Value);
        Assert.Equal(404, response.Response.StatusCode);
        using var json = await CatalogManagementHttp.Read(response);
        Assert.Equal(missing ? "Product type not found" : "Attribute not found",
            json.RootElement.GetProperty("title").GetString());
        Assert.Same(attribute, Assert.Single(owner.AttributeDefinitions));
        Assert.Equal(0, scenario.Repository.Saves);
    }

    [Fact]
    public async Task RemovingLastDefinition_LeavesAnEmptyDefinitionArray()
    {
        await using var scenario = new ProductTypeManagementScenario();
        var type = ProductType.Create("Type");
        var attribute = type.AddAttribute(AttributeDefinitionId.New(), AttributeCode.Create("color"), "Color",
            AttributeDataType.Text, false, false, AttributeScope.Product);
        scenario.Repository.Items.Add(type.Id, type);
        Assert.Equal(204, (await scenario.Http.Send("RemoveProductTypeAttribute",
            id: type.Id.Value, attributeId: attribute.Id.Value)).Response.StatusCode);
        using var json = await CatalogManagementHttp.Read(await scenario.Http.Send("GetProductType", id: type.Id.Value));
        Assert.Empty(json.RootElement.GetProperty("attributeDefinitions").EnumerateArray());
        Assert.Equal(1, scenario.Repository.Saves);
    }
}
