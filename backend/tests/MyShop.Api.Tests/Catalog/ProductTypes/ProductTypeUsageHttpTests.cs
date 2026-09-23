using MyShop.Domain.Catalog;

namespace MyShop.Api.Tests.Catalog.ProductTypes;

public sealed class ProductTypeUsageHttpTests
{
    [Theory]
    [InlineData(0)]
    [InlineData(3)]
    public async Task UsageReportsProductCountAndDoesNotWrite(int products)
    {
        await using var scenario = new ProductTypeManagementScenario();
        var type = ProductType.Create("Type");
        type.AddAttribute(AttributeDefinitionId.New(), AttributeCode.Create("value"), "Value",
            AttributeDataType.Text, true, true, AttributeScope.Product);
        scenario.Repository.Items.Add(type.Id, type);
        scenario.Repository.ProductCounts[type.Id] = products;
        var response = await scenario.Http.Send("GetProductTypeUsage", id: type.Id.Value);
        Assert.Equal(200, response.Response.StatusCode);
        using var json = await CatalogManagementHttp.Read(response);
        Assert.Equal(type.Id.Value, json.RootElement.GetProperty("productTypeId").GetGuid());
        Assert.Equal(products, json.RootElement.GetProperty("productCount").GetInt32());
        Assert.Equal(products > 0, json.RootElement.GetProperty("isInUse").GetBoolean());
        Assert.Equal(0, scenario.Repository.Saves);
        Assert.Equal(0, scenario.Repository.Deletes);
        Assert.Single(type.AttributeDefinitions);
    }

    [Fact]
    public async Task EarlierUnusedResponseDoesNotBypassDeletionGuard()
    {
        await using var scenario = new ProductTypeManagementScenario();
        var type = ProductType.Create("Type");
        scenario.Repository.Items.Add(type.Id, type);
        using (var json = await CatalogManagementHttp.Read(await scenario.Http.Send("GetProductTypeUsage", id: type.Id.Value)))
            Assert.False(json.RootElement.GetProperty("isInUse").GetBoolean());
        scenario.Repository.ProductCounts[type.Id] = 1;
        Assert.Equal(409, (await scenario.Http.Send("DeleteProductType", id: type.Id.Value)).Response.StatusCode);
        Assert.Equal(0, scenario.Repository.Deletes);
        scenario.Repository.ProductCounts.Remove(type.Id);
        Assert.Equal(204, (await scenario.Http.Send("DeleteProductType", id: type.Id.Value)).Response.StatusCode);
        Assert.Equal(404, (await scenario.Http.Send("GetProductTypeUsage", id: type.Id.Value)).Response.StatusCode);
        Assert.Equal(1, scenario.Repository.Deletes);
    }

    [Fact]
    public async Task EmptyIdAndMissingTypeAreDistinct()
    {
        await using var scenario = new ProductTypeManagementScenario();
        Assert.Equal(400, (await scenario.Http.Send("GetProductTypeUsage", id: Guid.Empty)).Response.StatusCode);
        Assert.Equal(0, scenario.Repository.Reads);
        Assert.Equal(404, (await scenario.Http.Send("GetProductTypeUsage", id: Guid.NewGuid())).Response.StatusCode);
        Assert.Equal(0, scenario.Repository.Saves);
    }
}
