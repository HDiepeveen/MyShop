using MyShop.Domain.Catalog;

namespace MyShop.Api.Tests.Catalog.ProductTypes;

public sealed class ProductTypeLifecycleHttpTests
{
    [Fact]
    public async Task CreateRenameRead_PreservesIdentityAndSkipsRepeatedRename()
    {
        await using var scenario = new ProductTypeManagementScenario();
        var response = await scenario.Http.Send("CreateProductType", new { name = "Clothing" });
        Assert.Equal(201, response.Response.StatusCode);
        using var created = await CatalogManagementHttp.Read(response);
        var id = created.RootElement.GetProperty("id").GetGuid();
        Assert.NotEqual(Guid.Empty, id);
        Assert.Equal("Clothing", created.RootElement.GetProperty("name").GetString());
        Assert.Equal($"/api/product-types/{id}", response.Response.Headers.Location.ToString());
        for (var i = 0; i < 2; i++)
            Assert.Equal(204, (await scenario.Http.Send("RenameProductType", new { name = " Shirts " }, id)).Response.StatusCode);
        var read = await scenario.Http.Send("GetProductType", id: id);
        Assert.Equal(200, read.Response.StatusCode);
        using var json = await CatalogManagementHttp.Read(read);
        Assert.Equal(id, json.RootElement.GetProperty("id").GetGuid());
        Assert.Equal(" Shirts ", json.RootElement.GetProperty("name").GetString());
        Assert.Empty(json.RootElement.GetProperty("attributeDefinitions").EnumerateArray());
        Assert.Equal(1, scenario.Repository.Adds);
        Assert.Equal(1, scenario.Repository.Saves);
    }

    [Theory]
    [InlineData("CreateProductType", "{")]
    [InlineData("CreateProductType", "")]
    [InlineData("CreateProductType", "{\"name\":null}")]
    [InlineData("CreateProductType", "{\"name\":\" \"}")]
    [InlineData("RenameProductType", "{")]
    [InlineData("RenameProductType", "")]
    [InlineData("RenameProductType", "{\"name\":null}")]
    [InlineData("RenameProductType", "{\"name\":\" \"}")]
    public async Task InvalidNameOrBody_DoesNotChangeCatalog(string endpoint, string body)
    {
        await using var scenario = new ProductTypeManagementScenario();
        var type = ProductType.Create("Original");
        scenario.Repository.Items.Add(type.Id, type);
        Assert.Equal(400, (await scenario.Http.Send(endpoint, id: type.Id.Value, rawBody: body)).Response.StatusCode);
        Assert.Same(type, Assert.Single(scenario.Repository.Items).Value);
        Assert.Equal("Original", type.Name);
        Assert.Equal(0, scenario.Repository.Adds);
        Assert.Equal(0, scenario.Repository.Saves);
    }

    [Fact]
    public async Task UnknownProductType_CannotBeReadOrRenamed()
    {
        await using var scenario = new ProductTypeManagementScenario();
        var id = Guid.NewGuid();
        Assert.Equal(404, (await scenario.Http.Send("GetProductType", id: id)).Response.StatusCode);
        Assert.Equal(404, (await scenario.Http.Send("RenameProductType", new { name = "New" }, id)).Response.StatusCode);
        Assert.Equal(0, scenario.Repository.Saves);
    }
}
