using MyShop.Domain.Catalog;

namespace MyShop.Api.Tests.Catalog.ProductTypes;

public sealed class ProductTypeAttributeEditingHttpTests
{
    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public async Task RenameAndConfigure_ReadsUpdatedDefinitionAndSkipsRepeatedWrites(bool required, bool filterable)
    {
        await using var scenario = new ProductTypeManagementScenario();
        var type = ProductType.Create("Type");
        var attribute = type.AddAttribute(AttributeDefinitionId.New(), AttributeCode.Create("color"), "Old",
            AttributeDataType.Choice, !required, !filterable, AttributeScope.Variant);
        scenario.Repository.Items.Add(type.Id, type);
        for (var i = 0; i < 2; i++)
        {
            Assert.Equal(204, (await scenario.Http.Send("RenameProductTypeAttribute",
                new { displayName = " Colour " }, type.Id.Value, attribute.Id.Value)).Response.StatusCode);
            Assert.Equal(204, (await scenario.Http.Send("ConfigureProductTypeAttribute",
                new { isRequired = required, isFilterable = filterable }, type.Id.Value, attribute.Id.Value)).Response.StatusCode);
        }
        var read = await scenario.Http.Send("GetProductType", id: type.Id.Value);
        using var json = await CatalogManagementHttp.Read(read);
        var definition = Assert.Single(json.RootElement.GetProperty("attributeDefinitions").EnumerateArray());
        Assert.Equal(attribute.Id.Value, definition.GetProperty("id").GetGuid());
        Assert.Equal("color", definition.GetProperty("code").GetString());
        Assert.Equal(" Colour ", definition.GetProperty("displayName").GetString());
        Assert.Equal("Choice", definition.GetProperty("dataType").GetString());
        Assert.Equal("Variant", definition.GetProperty("scope").GetString());
        Assert.Equal(required, definition.GetProperty("isRequired").GetBoolean());
        Assert.Equal(filterable, definition.GetProperty("isFilterable").GetBoolean());
        Assert.Equal(2, scenario.Repository.Saves);
    }

    [Theory]
    [InlineData("RenameProductTypeAttribute", "{\"displayName\":\" \"}")]
    [InlineData("RenameProductTypeAttribute", "{\"displayName\":null}")]
    [InlineData("ConfigureProductTypeAttribute", "{\"isRequired\":\"yes\"}")]
    [InlineData("ConfigureProductTypeAttribute", "{")]
    public async Task InvalidEdit_PreservesExistingDefinition(string endpoint, string body)
    {
        await using var scenario = new ProductTypeManagementScenario();
        var type = ProductType.Create("Type");
        var attribute = type.AddAttribute(AttributeDefinitionId.New(), AttributeCode.Create("color"), "Original",
            AttributeDataType.Text, true, true, AttributeScope.Product);
        scenario.Repository.Items.Add(type.Id, type);
        Assert.Equal(400, (await scenario.Http.Send(endpoint, id: type.Id.Value,
            attributeId: attribute.Id.Value, rawBody: body)).Response.StatusCode);
        Assert.Same(attribute, Assert.Single(type.AttributeDefinitions));
        Assert.Equal("Original", attribute.DisplayName);
        Assert.True(attribute.IsRequired);
        Assert.True(attribute.IsFilterable);
        Assert.Equal(0, scenario.Repository.Saves);
    }

    [Theory]
    [InlineData("RenameProductTypeAttribute")]
    [InlineData("ConfigureProductTypeAttribute")]
    public async Task DefinitionFromOtherType_IsNotModified(string endpoint)
    {
        await using var scenario = new ProductTypeManagementScenario();
        var first = ProductType.Create("First");
        var second = ProductType.Create("Second");
        var attribute = second.AddAttribute(AttributeDefinitionId.New(), AttributeCode.Create("color"), "Original",
            AttributeDataType.Text, true, true, AttributeScope.Product);
        scenario.Repository.Items.Add(first.Id, first);
        scenario.Repository.Items.Add(second.Id, second);
        var response = await scenario.Http.Send(endpoint,
            new { displayName = "Changed", isRequired = false, isFilterable = false }, first.Id.Value, attribute.Id.Value);
        Assert.Equal(404, response.Response.StatusCode);
        using var json = await CatalogManagementHttp.Read(response);
        Assert.Equal("Attribute not found", json.RootElement.GetProperty("title").GetString());
        Assert.Equal("Original", attribute.DisplayName);
        Assert.True(attribute.IsRequired);
        Assert.True(attribute.IsFilterable);
        Assert.Equal(0, scenario.Repository.Saves);
    }
}
