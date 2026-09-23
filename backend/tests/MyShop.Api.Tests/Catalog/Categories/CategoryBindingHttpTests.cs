using MyShop.Domain.Catalog;

namespace MyShop.Api.Tests.Catalog.Categories;

public sealed class CategoryBindingHttpTests
{
    [Theory]
    [InlineData("CreateCategory")]
    [InlineData("RenameCategory")]
    [InlineData("MoveCategory")]
    public async Task UnsupportedContentType_IsRejectedBeforeRepositoryAccess(string endpoint)
    {
        await using var scenario = new CategoryManagementScenario();
        var category = Category.CreateRoot("Original");
        scenario.Repository.Items.Add(category.Id, category);
        var response = await scenario.Http.Send(endpoint, id: category.Id.Value,
            rawBody: "{}", contentType: "text/plain");
        Assert.Equal(415, response.Response.StatusCode);
        Assert.Equal(0, scenario.Repository.Reads);
        Assert.Equal(0, scenario.Repository.Adds);
        Assert.Equal(0, scenario.Repository.Saves);
        Assert.Equal("Original", category.Name);
        Assert.True(category.IsRoot);
    }

    [Theory]
    [InlineData("GetCategory")]
    [InlineData("DeleteCategory")]
    [InlineData("RenameCategory")]
    [InlineData("MoveCategory")]
    public async Task EmptyId_IsRejectedWithoutRepositoryAccess(string endpoint)
    {
        await using var scenario = new CategoryManagementScenario();
        var response = await scenario.Http.Send(endpoint,
            new { name = "Name", parentCategoryId = (Guid?)null }, Guid.Empty);
        Assert.Equal(400, response.Response.StatusCode);
        Assert.Equal(0, scenario.Repository.Reads);
        Assert.Equal(0, scenario.Repository.Deletes);
        Assert.Equal(0, scenario.Repository.Saves);
    }

    [Theory]
    [InlineData("CreateCategory")]
    [InlineData("GetCategory")]
    [InlineData("RenameCategory")]
    [InlineData("MoveCategory")]
    [InlineData("DeleteCategory")]
    public async Task CancelledRequest_DoesNotReadOrWrite(string endpoint)
    {
        await using var scenario = new CategoryManagementScenario();
        var category = Category.CreateRoot("Original");
        scenario.Repository.Items.Add(category.Id, category);
        using var source = new CancellationTokenSource();
        source.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => scenario.Http.Send(endpoint,
            new { name = "New", parentCategoryId = (Guid?)null }, category.Id.Value,
            cancellationToken: source.Token));
        Assert.Equal(0, scenario.Repository.Reads);
        Assert.Equal(0, scenario.Repository.Adds);
        Assert.Equal(0, scenario.Repository.Saves);
        Assert.Equal(0, scenario.Repository.Deletes);
        Assert.Equal("Original", category.Name);
    }

    [Fact]
    public async Task ActiveCancellationToken_ReachesReadAndWrite()
    {
        await using var scenario = new CategoryManagementScenario();
        using var source = new CancellationTokenSource();
        var response = await scenario.Http.Send("CreateCategory", new { name = "New" },
            cancellationToken: source.Token);
        Assert.Equal(201, response.Response.StatusCode);
        Assert.Equal(source.Token, scenario.Repository.LastToken);
        var category = Assert.Single(scenario.Repository.Items).Value;
        Assert.Equal(200, (await scenario.Http.Send("GetCategory", id: category.Id.Value,
            cancellationToken: source.Token)).Response.StatusCode);
        Assert.Equal(source.Token, scenario.Repository.LastToken);
    }
}
