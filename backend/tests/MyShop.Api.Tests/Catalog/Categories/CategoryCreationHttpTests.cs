using System.Text.Json;
using MyShop.Domain.Catalog;

namespace MyShop.Api.Tests.Catalog.Categories;

public sealed class CategoryCreationHttpTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task CreateThenRead_PreservesIdentityNameAndParent(bool child)
    {
        await using var scenario = new CategoryManagementScenario();
        var parent = Category.CreateRoot("Parent");
        scenario.Repository.Items.Add(parent.Id, parent);
        Guid? parentId = child ? parent.Id.Value : null;
        var response = await scenario.Http.Send("CreateCategory", new { name = " Shirts ", parentCategoryId = parentId });
        Assert.Equal(201, response.Response.StatusCode);
        using var created = await CatalogManagementHttp.Read(response);
        var id = created.RootElement.GetProperty("id").GetGuid();
        Assert.NotEqual(Guid.Empty, id);
        Assert.Equal($"/api/categories/{id}", response.Response.Headers.Location.ToString());

        var read = await scenario.Http.Send("GetCategory", id: id);
        Assert.Equal(200, read.Response.StatusCode);
        using var json = await CatalogManagementHttp.Read(read);
        Assert.Equal(id, json.RootElement.GetProperty("id").GetGuid());
        Assert.Equal(" Shirts ", json.RootElement.GetProperty("name").GetString());
        Assert.Equal(!child, json.RootElement.GetProperty("isRoot").GetBoolean());
        if (child)
            Assert.Equal(parentId, json.RootElement.GetProperty("parentCategoryId").GetGuid());
        else
            Assert.Equal(JsonValueKind.Null, json.RootElement.GetProperty("parentCategoryId").ValueKind);
        Assert.Equal(1, scenario.Repository.Adds);
        Assert.Equal(0, scenario.Repository.Saves);
    }

    [Theory]
    [InlineData("{")]
    [InlineData("")]
    [InlineData("{\"name\":null}")]
    [InlineData("{\"name\":\"  \"}")]
    [InlineData("{\"name\":\"Child\",\"parentCategoryId\":\"invalid\"}")]
    public async Task InvalidCreate_DoesNotAddCategory(string body)
    {
        await using var scenario = new CategoryManagementScenario();
        Assert.Equal(400, (await scenario.Http.Send("CreateCategory", rawBody: body)).Response.StatusCode);
        Assert.Empty(scenario.Repository.Items);
        Assert.Equal(0, scenario.Repository.Adds);
    }

    [Fact]
    public async Task MissingParent_DoesNotCreateOrphan()
    {
        await using var scenario = new CategoryManagementScenario();
        var response = await scenario.Http.Send("CreateCategory",
            new { name = "Child", parentCategoryId = Guid.NewGuid() });
        Assert.Equal(404, response.Response.StatusCode);
        using var json = await CatalogManagementHttp.Read(response);
        Assert.Equal("Parent category not found", json.RootElement.GetProperty("title").GetString());
        Assert.Empty(scenario.Repository.Items);
        Assert.Equal(0, scenario.Repository.Adds);
    }
}
