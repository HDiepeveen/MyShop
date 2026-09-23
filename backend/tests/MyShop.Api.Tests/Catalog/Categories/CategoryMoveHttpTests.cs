using System.Text.Json;
using MyShop.Domain.Catalog;

namespace MyShop.Api.Tests.Catalog.Categories;

public sealed class CategoryMoveHttpTests
{
    [Fact]
    public async Task MoveBetweenParentsThenRoot_PreservesIdentityAndSkipsRepeatedMoves()
    {
        await using var scenario = new CategoryManagementScenario();
        var first = Category.CreateRoot("First");
        var second = Category.CreateRoot("Second");
        var child = Category.CreateChild("Child", first.Id);
        foreach (var item in new[] { first, second, child }) scenario.Repository.Items.Add(item.Id, item);
        foreach (Guid? parent in new Guid?[] { second.Id.Value, second.Id.Value, null, null })
        {
            Assert.Equal(204, (await scenario.Http.Send("MoveCategory",
                new { parentCategoryId = parent }, child.Id.Value)).Response.StatusCode);
            var read = await scenario.Http.Send("GetCategory", id: child.Id.Value);
            Assert.Equal(200, read.Response.StatusCode);
            using var json = await CatalogManagementHttp.Read(read);
            Assert.Equal(child.Id.Value, json.RootElement.GetProperty("id").GetGuid());
            Assert.Equal("Child", json.RootElement.GetProperty("name").GetString());
            Assert.Equal(parent is null, json.RootElement.GetProperty("isRoot").GetBoolean());
            if (parent is null)
                Assert.Equal(JsonValueKind.Null, json.RootElement.GetProperty("parentCategoryId").ValueKind);
            else
                Assert.Equal(parent.Value, json.RootElement.GetProperty("parentCategoryId").GetGuid());
            Assert.Equal(parent, child.ParentCategoryId?.Value);
        }
        Assert.Equal(2, scenario.Repository.Saves);
        Assert.True(first.IsRoot);
        Assert.True(second.IsRoot);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task MoveUnderSelfOrDescendant_RejectsCycleWithoutMutation(bool descendant)
    {
        await using var scenario = new CategoryManagementScenario();
        var root = Category.CreateRoot("Root");
        var child = Category.CreateChild("Child", root.Id);
        var leaf = Category.CreateChild("Leaf", child.Id);
        foreach (var item in new[] { root, child, leaf }) scenario.Repository.Items.Add(item.Id, item);
        var response = await scenario.Http.Send("MoveCategory",
            new { parentCategoryId = descendant ? leaf.Id.Value : root.Id.Value }, root.Id.Value);
        Assert.Equal(409, response.Response.StatusCode);
        using var json = await CatalogManagementHttp.Read(response);
        Assert.Equal("Category hierarchy cycle", json.RootElement.GetProperty("title").GetString());
        Assert.True(root.IsRoot);
        Assert.Equal(root.Id, child.ParentCategoryId);
        Assert.Equal(child.Id, leaf.ParentCategoryId);
        Assert.Equal(0, scenario.Repository.Saves);
    }

    [Fact]
    public async Task MissingParent_LeavesExistingParentIntact()
    {
        await using var scenario = new CategoryManagementScenario();
        var child = Category.CreateChild("Child", CategoryId.New());
        scenario.Repository.Items.Add(child.Id, child);
        var original = child.ParentCategoryId;
        Assert.Equal(404, (await scenario.Http.Send("MoveCategory",
            new { parentCategoryId = Guid.NewGuid() }, child.Id.Value)).Response.StatusCode);
        Assert.Equal(original, child.ParentCategoryId);
        Assert.Equal(0, scenario.Repository.Saves);
    }

    [Fact]
    public async Task MissingCategory_DoesNotAlterParent()
    {
        await using var scenario = new CategoryManagementScenario();
        var parent = Category.CreateRoot("Parent");
        scenario.Repository.Items.Add(parent.Id, parent);
        Assert.Equal(404, (await scenario.Http.Send("MoveCategory",
            new { parentCategoryId = parent.Id.Value }, Guid.NewGuid())).Response.StatusCode);
        Assert.True(parent.IsRoot);
        Assert.Equal(0, scenario.Repository.Saves);
    }
}
