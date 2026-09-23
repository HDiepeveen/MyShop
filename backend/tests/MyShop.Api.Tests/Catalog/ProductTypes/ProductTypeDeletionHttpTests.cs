using MyShop.Domain.Catalog;

namespace MyShop.Api.Tests.Catalog.ProductTypes;

public sealed class ProductTypeDeletionHttpTests
{
    [Theory]
    [InlineData(1)]
    [InlineData(3)]
    public async Task InUseType_CanOnlyBeDeletedAfterLastProductIsRemoved(int productCount)
    {
        await using var scenario = new ProductTypeManagementScenario();
        var type = ProductType.Create("Used");
        var other = ProductType.Create("Other");
        scenario.Repository.Items.Add(type.Id, type);
        scenario.Repository.Items.Add(other.Id, other);
        scenario.Repository.ProductCounts[type.Id] = productCount;
        var response = await scenario.Http.Send("DeleteProductType", id: type.Id.Value);
        Assert.Equal(409, response.Response.StatusCode);
        using var json = await CatalogManagementHttp.Read(response);
        Assert.Equal("Product type is in use", json.RootElement.GetProperty("title").GetString());
        Assert.Contains($"{productCount} products", json.RootElement.GetProperty("detail").GetString());
        Assert.Equal(200, (await scenario.Http.Send("GetProductType", id: type.Id.Value)).Response.StatusCode);
        Assert.Equal(0, scenario.Repository.Deletes);

        scenario.Repository.ProductCounts.Remove(type.Id);
        Assert.Equal(204, (await scenario.Http.Send("DeleteProductType", id: type.Id.Value)).Response.StatusCode);
        Assert.Equal(404, (await scenario.Http.Send("GetProductType", id: type.Id.Value)).Response.StatusCode);
        Assert.Equal(404, (await scenario.Http.Send("DeleteProductType", id: type.Id.Value)).Response.StatusCode);
        Assert.Equal(1, scenario.Repository.Deletes);
        Assert.Same(other, Assert.Single(scenario.Repository.Items).Value);
    }

    [Fact]
    public async Task EmptyId_IsRejectedBeforeDeletionOrRead()
    {
        await using var scenario = new ProductTypeManagementScenario();
        Assert.Equal(400, (await scenario.Http.Send("DeleteProductType", id: Guid.Empty)).Response.StatusCode);
        Assert.Equal(0, scenario.Repository.Reads);
        Assert.Equal(0, scenario.Repository.Deletes);
    }

    [Theory]
    [InlineData("CreateProductType")]
    [InlineData("GetProductType")]
    [InlineData("RenameProductType")]
    [InlineData("DeleteProductType")]
    [InlineData("AddProductTypeAttribute")]
    [InlineData("RenameProductTypeAttribute")]
    [InlineData("ConfigureProductTypeAttribute")]
    [InlineData("RemoveProductTypeAttribute")]
    public async Task CancelledManagementRequest_DoesNotReadOrWrite(string endpoint)
    {
        await using var scenario = new ProductTypeManagementScenario();
        using var source = new CancellationTokenSource();
        source.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => scenario.Http.Send(endpoint,
            new { name = "New", code = "value", displayName = "Value", dataType = 0, scope = 0 },
            Guid.NewGuid(), Guid.NewGuid(), cancellationToken: source.Token));
        Assert.Equal(0, scenario.Repository.Reads);
        Assert.Equal(0, scenario.Repository.Adds);
        Assert.Equal(0, scenario.Repository.Saves);
        Assert.Equal(0, scenario.Repository.Deletes);
    }
}
