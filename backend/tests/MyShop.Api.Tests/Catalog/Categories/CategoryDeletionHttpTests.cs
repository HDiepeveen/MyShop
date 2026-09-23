using MyShop.Domain.Catalog;

namespace MyShop.Api.Tests.Catalog.Categories;

public sealed class CategoryDeletionHttpTests
{
    [Fact]
    public async Task DeleteThenRead_ReturnsNotFoundAndDoesNotDeleteTwice()
    {
        await using var scenario = new CategoryManagementScenario();
        var category = Category.CreateRoot("Unused");
        var other = Category.CreateRoot("Other");
        scenario.Repository.Items.Add(category.Id, category);
        scenario.Repository.Items.Add(other.Id, other);
        Assert.Equal(204, (await scenario.Http.Send("DeleteCategory", id: category.Id.Value)).Response.StatusCode);
        Assert.Equal(404, (await scenario.Http.Send("GetCategory", id: category.Id.Value)).Response.StatusCode);
        Assert.Equal(404, (await scenario.Http.Send("DeleteCategory", id: category.Id.Value)).Response.StatusCode);
        Assert.Same(other, Assert.Single(scenario.Repository.Items).Value);
        Assert.Equal(1, scenario.Repository.Deletes);
    }

    [Theory]
    [InlineData(1, 0)]
    [InlineData(0, 2)]
    [InlineData(1, 2)]
    public async Task InUseCategory_RemainsReadableUntilDependenciesAreRemoved(int children, int assignments)
    {
        await using var scenario = new CategoryManagementScenario();
        var category = Category.CreateRoot("Used");
        scenario.Repository.Items.Add(category.Id, category);
        for (var i = 0; i < children; i++)
        {
            var child = Category.CreateChild("Child", category.Id);
            scenario.Repository.Items.Add(child.Id, child);
        }
        scenario.Repository.Assignments[category.Id] = assignments;
        var response = await scenario.Http.Send("DeleteCategory", id: category.Id.Value);
        Assert.Equal(409, response.Response.StatusCode);
        using var json = await CatalogManagementHttp.Read(response);
        Assert.Equal("Category is in use", json.RootElement.GetProperty("title").GetString());
        Assert.Contains($"{children} direct children", json.RootElement.GetProperty("detail").GetString());
        Assert.Contains($"{assignments} product assignments", json.RootElement.GetProperty("detail").GetString());
        Assert.Equal(200, (await scenario.Http.Send("GetCategory", id: category.Id.Value)).Response.StatusCode);
        Assert.Equal(0, scenario.Repository.Deletes);

        foreach (var child in scenario.Repository.Items.Values.Where(item => item.ParentCategoryId == category.Id).ToArray())
            Assert.Equal(204, (await scenario.Http.Send("DeleteCategory", id: child.Id.Value)).Response.StatusCode);
        scenario.Repository.Assignments.Remove(category.Id);
        Assert.Equal(204, (await scenario.Http.Send("DeleteCategory", id: category.Id.Value)).Response.StatusCode);
        Assert.Empty(scenario.Repository.Items);
        Assert.Equal(children + 1, scenario.Repository.Deletes);
    }
}
