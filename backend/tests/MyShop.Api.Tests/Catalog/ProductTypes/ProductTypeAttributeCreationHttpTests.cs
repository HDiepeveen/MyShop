using MyShop.Domain.Catalog;

namespace MyShop.Api.Tests.Catalog.ProductTypes;

public sealed class ProductTypeAttributeCreationHttpTests
{
    public static TheoryData<int, string, int, string> TypesAndScopes
    {
        get
        {
            var data = new TheoryData<int, string, int, string>();
            // Wire values are explicit so enum changes cannot silently rewrite the expected contract.
            (int Value, string Name)[] types =
                [(0, "Text"), (1, "Integer"), (2, "Decimal"), (3, "Boolean"),
                 (4, "Date"), (5, "Choice"), (6, "MultiChoice")];
            (int Value, string Name)[] scopes = [(0, "Product"), (1, "Variant")];
            foreach (var type in types)
                foreach (var scope in scopes)
                    data.Add(type.Value, type.Name, scope.Value, scope.Name);
            return data;
        }
    }

    [Theory]
    [MemberData(nameof(TypesAndScopes))]
    public async Task AddThenRead_PreservesDefinitionAcrossRequestAndResponseContracts(
        int dataType, string expectedDataType, int scope, string expectedScope)
    {
        await using var scenario = new ProductTypeManagementScenario();
        var type = ProductType.Create("Type");
        scenario.Repository.Items.Add(type.Id, type);
        var response = await scenario.Http.Send("AddProductTypeAttribute",
            new { code = "value", displayName = " Value ", dataType, scope, isRequired = true, isFilterable = true },
            type.Id.Value);
        Assert.Equal(201, response.Response.StatusCode);
        using var created = await CatalogManagementHttp.Read(response);
        var id = created.RootElement.GetProperty("id").GetGuid();
        Assert.NotEqual(Guid.Empty, id);
        Assert.Equal($"/api/product-types/{type.Id.Value}/attributes/{id}", response.Response.Headers.Location.ToString());
        Assert.Equal(dataType, created.RootElement.GetProperty("dataType").GetInt32());
        Assert.Equal(scope, created.RootElement.GetProperty("scope").GetInt32());
        var read = await scenario.Http.Send("GetProductType", id: type.Id.Value);
        Assert.Equal(200, read.Response.StatusCode);
        using var json = await CatalogManagementHttp.Read(read);
        var definition = Assert.Single(json.RootElement.GetProperty("attributeDefinitions").EnumerateArray());
        Assert.Equal(id, definition.GetProperty("id").GetGuid());
        Assert.Equal("value", definition.GetProperty("code").GetString());
        Assert.Equal(" Value ", definition.GetProperty("displayName").GetString());
        Assert.Equal(expectedDataType, definition.GetProperty("dataType").GetString());
        Assert.Equal(expectedScope, definition.GetProperty("scope").GetString());
        Assert.True(definition.GetProperty("isRequired").GetBoolean());
        Assert.True(definition.GetProperty("isFilterable").GetBoolean());
        Assert.Equal(1, scenario.Repository.Saves);
    }

    [Fact]
    public async Task DuplicateCode_ConflictsWithoutReplacingExistingDefinition()
    {
        await using var scenario = new ProductTypeManagementScenario();
        var type = ProductType.Create("Type");
        scenario.Repository.Items.Add(type.Id, type);
        var body = new { code = "color", displayName = "Color", dataType = 0, scope = 0, isRequired = false, isFilterable = false };
        Assert.Equal(201, (await scenario.Http.Send("AddProductTypeAttribute", body, type.Id.Value)).Response.StatusCode);
        var original = Assert.Single(type.AttributeDefinitions);
        Assert.Equal(409, (await scenario.Http.Send("AddProductTypeAttribute", body, type.Id.Value)).Response.StatusCode);
        Assert.Same(original, Assert.Single(type.AttributeDefinitions));
        Assert.Equal(1, scenario.Repository.Saves);
    }

    [Theory]
    [InlineData(999, 0)]
    [InlineData(0, 999)]
    [InlineData(-1, 0)]
    [InlineData(0, -1)]
    public async Task UnknownEnums_AreRejectedBeforeRepositoryAccess(int dataType, int scope)
    {
        await using var scenario = new ProductTypeManagementScenario();
        var response = await scenario.Http.Send("AddProductTypeAttribute",
            new { code = "value", displayName = "Value", dataType, scope }, Guid.NewGuid());
        Assert.Equal(400, response.Response.StatusCode);
        Assert.Equal(0, scenario.Repository.Reads);
        Assert.Equal(0, scenario.Repository.Saves);
    }

    [Fact]
    public async Task MissingType_DoesNotCreateDefinition()
    {
        await using var scenario = new ProductTypeManagementScenario();
        var response = await scenario.Http.Send("AddProductTypeAttribute",
            new { code = "value", displayName = "Value", dataType = 0, scope = 0 }, Guid.NewGuid());
        Assert.Equal(404, response.Response.StatusCode);
        Assert.Equal(0, scenario.Repository.Saves);
    }
}
