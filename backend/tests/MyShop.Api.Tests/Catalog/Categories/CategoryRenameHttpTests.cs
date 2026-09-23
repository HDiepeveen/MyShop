using MyShop.Domain.Catalog;

namespace MyShop.Api.Tests.Catalog.Categories;

public sealed class CategoryRenameHttpTests
{
    [Fact]
    public async Task RenameThenRead_RepeatingNameDoesNotWriteAgain()
    {
        await using var scenario = new CategoryManagementScenario();
        var category = Category.CreateChild("Old", CategoryId.New());
        scenario.Repository.Items.Add(category.Id, category);
        var parent = category.ParentCategoryId;
        for (var i = 0; i < 2; i++)
            Assert.Equal(204, (await scenario.Http.Send("RenameCategory",
                new { name = " New " }, category.Id.Value)).Response.StatusCode);
        var read = await scenario.Http.Send("GetCategory", id: category.Id.Value);
        using var json = await CatalogManagementHttp.Read(read);
        Assert.Equal(200, read.Response.StatusCode);
        Assert.Equal(" New ", json.RootElement.GetProperty("name").GetString());
        Assert.Equal(parent!.Value.Value, json.RootElement.GetProperty("parentCategoryId").GetGuid());
        Assert.Equal(1, scenario.Repository.Saves);
        Assert.Equal(0, scenario.Repository.Adds);
    }

    [Theory]
    [InlineData("{")]
    [InlineData("")]
    [InlineData("{\"name\":null}")]
    [InlineData("{\"name\":\"\"}")]
    [InlineData("{\"name\":\"  \"}")]
    public async Task InvalidRename_LeavesNameAndParentIntact(string body)
    {
        await using var scenario = new CategoryManagementScenario();
        var category = Category.CreateChild("Original", CategoryId.New());
        scenario.Repository.Items.Add(category.Id, category);
        var parent = category.ParentCategoryId;
        Assert.Equal(400, (await scenario.Http.Send("RenameCategory",
            id: category.Id.Value, rawBody: body)).Response.StatusCode);
        Assert.Equal("Original", category.Name);
        Assert.Equal(parent, category.ParentCategoryId);
        Assert.Equal(0, scenario.Repository.Saves);
    }

    [Fact]
    public async Task RenameUnknownCategory_ReturnsNotFound()
    {
        await using var scenario = new CategoryManagementScenario();
        var response = await scenario.Http.Send("RenameCategory", new { name = "New" }, Guid.NewGuid());
        Assert.Equal(404, response.Response.StatusCode);
        Assert.Equal(0, scenario.Repository.Saves);
        Assert.Empty(scenario.Repository.Items);
    }
}
