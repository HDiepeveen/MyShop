using MyShop.Domain.Catalog;

namespace MyShop.Api.Tests.Catalog.Categories;

public sealed class CategoryUsageHttpTests
{
    [Fact]
    public async Task UsageReflectsDirectChildrenAndAssignmentsWithoutWriting()
    {
        await using var scenario = new CategoryManagementScenario();
        var root = Category.CreateRoot("Root");
        var child = Category.CreateChild("Child", root.Id);
        var grandchild = Category.CreateChild("Grandchild", child.Id);
        foreach (var item in new[] { root, child, grandchild }) scenario.Repository.Items.Add(item.Id, item);
        scenario.Repository.Assignments[root.Id] = 3;
        var response = await scenario.Http.Send("GetCategoryUsage", id: root.Id.Value);
        Assert.Equal(200, response.Response.StatusCode);
        using (var json = await CatalogManagementHttp.Read(response))
        {
            Assert.Equal(root.Id.Value, json.RootElement.GetProperty("categoryId").GetGuid());
            Assert.Equal(1, json.RootElement.GetProperty("directChildCount").GetInt32());
            Assert.Equal(3, json.RootElement.GetProperty("productAssignmentCount").GetInt32());
            Assert.True(json.RootElement.GetProperty("isInUse").GetBoolean());
        }
        Assert.Equal(0, scenario.Repository.Saves);
        Assert.Equal(0, scenario.Repository.Deletes);
        Assert.Equal(409, (await scenario.Http.Send("DeleteCategory", id: root.Id.Value)).Response.StatusCode);

        Assert.Equal(204, (await scenario.Http.Send("MoveCategory",
            new { parentCategoryId = (Guid?)null }, child.Id.Value)).Response.StatusCode);
        scenario.Repository.Assignments.Remove(root.Id);
        using (var json = await CatalogManagementHttp.Read(await scenario.Http.Send("GetCategoryUsage", id: root.Id.Value)))
        {
            Assert.Equal(0, json.RootElement.GetProperty("directChildCount").GetInt32());
            Assert.Equal(0, json.RootElement.GetProperty("productAssignmentCount").GetInt32());
            Assert.False(json.RootElement.GetProperty("isInUse").GetBoolean());
        }
        Assert.Equal(1, scenario.Repository.Saves);
        Assert.Equal(204, (await scenario.Http.Send("DeleteCategory", id: root.Id.Value)).Response.StatusCode);
        Assert.Equal(404, (await scenario.Http.Send("GetCategoryUsage", id: root.Id.Value)).Response.StatusCode);
        Assert.Equal(2, scenario.Repository.Items.Count);
    }

    [Fact]
    public async Task EarlierUnusedResponseDoesNotBypassDeletionGuard()
    {
        await using var scenario = new CategoryManagementScenario();
        var root = Category.CreateRoot("Root");
        scenario.Repository.Items.Add(root.Id, root);
        using (var json = await CatalogManagementHttp.Read(await scenario.Http.Send("GetCategoryUsage", id: root.Id.Value)))
            Assert.False(json.RootElement.GetProperty("isInUse").GetBoolean());
        Assert.Equal(201, (await scenario.Http.Send("CreateCategory",
            new { name = "New child", parentCategoryId = root.Id.Value })).Response.StatusCode);
        Assert.Equal(409, (await scenario.Http.Send("DeleteCategory", id: root.Id.Value)).Response.StatusCode);
        Assert.True(scenario.Repository.Items.ContainsKey(root.Id));
        Assert.Equal(0, scenario.Repository.Deletes);
    }

    [Fact]
    public async Task EmptyIdAndMissingCategoryAreDistinct()
    {
        await using var scenario = new CategoryManagementScenario();
        Assert.Equal(400, (await scenario.Http.Send("GetCategoryUsage", id: Guid.Empty)).Response.StatusCode);
        Assert.Equal(0, scenario.Repository.Reads);
        Assert.Equal(404, (await scenario.Http.Send("GetCategoryUsage", id: Guid.NewGuid())).Response.StatusCode);
        Assert.Equal(0, scenario.Repository.Saves);
    }
}
