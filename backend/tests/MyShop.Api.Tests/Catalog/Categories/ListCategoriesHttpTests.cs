using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using MyShop.Api.Catalog.Categories;
using MyShop.Application.Catalog.Abstractions;
using MyShop.Domain.Catalog;
using UseCase = MyShop.Application.Catalog.ListCategories.ListCategories;

namespace MyShop.Api.Tests.Catalog.Categories;

public sealed class ListCategoriesHttpTests
{
    [Theory]
    [InlineData("", 0, 50)]
    [InlineData("?offset=2&limit=1", 2, 1)]
    [InlineData("?limit=100", 0, 100)]
    [InlineData("?offset=2147483647", 2147483647, 50)]
    public async Task BoundEndpoint_ParsesPagingAndKeepsArrayResponse(string query, int offset, int limit)
    {
        var repository = new RepositoryFake();
        await using var app = CreateApp(repository);
        using var source = new CancellationTokenSource();

        var response = await CatalogListHttp.Execute(app, query, source.Token);

        Assert.Equal(200, response.Response.StatusCode);
        Assert.Equal(offset, repository.Offset);
        Assert.Equal(limit, repository.Limit);
        Assert.Equal(source.Token, repository.Cancellation);
        using var json = await JsonDocument.ParseAsync(response.Response.Body);
        Assert.Equal(JsonValueKind.Array, json.RootElement.ValueKind);
        var item = Assert.Single(json.RootElement.EnumerateArray());
        Assert.Equal(repository.Item.Id, item.GetProperty("id").GetGuid());
        Assert.Equal("Category", item.GetProperty("name").GetString());
        Assert.True(item.GetProperty("isRoot").GetBoolean());
        Assert.Equal(3, item.GetProperty("directChildCount").GetInt32());
    }

    [Theory]
    [InlineData("?offset=-1")]
    [InlineData("?limit=0")]
    [InlineData("?limit=101")]
    [InlineData("?offset=invalid")]
    [InlineData("?limit=2147483648")]
    [InlineData("?rootsOnly=invalid")]
    [InlineData("?parentCategoryId=invalid")]
    [InlineData("?search=%20")]
    public async Task BoundEndpoint_InvalidQueryIsRejectedBeforeRead(string query)
    {
        var repository = new RepositoryFake();
        await using var app = CreateApp(repository);
        Assert.Equal(400, (await CatalogListHttp.Execute(app, query)).Response.StatusCode);
        Assert.Equal(0, repository.Calls);
    }

    [Fact]
    public async Task BoundEndpoint_CombinesPagingWithTrimmedSearchAndParentFilter()
    {
        var repository = new RepositoryFake();
        await using var app = CreateApp(repository);
        var parent = Guid.NewGuid();
        var response = await CatalogListHttp.Execute(app,
            $"?offset=5&limit=10&search=%20shirt%20&parentCategoryId={parent}");
        Assert.Equal(200, response.Response.StatusCode);
        Assert.Equal("shirt", repository.Search);
        Assert.Equal(CategoryId.From(parent), repository.Parent);
        Assert.False(repository.RootsOnly);
        Assert.Equal(5, repository.Offset);
        Assert.Equal(10, repository.Limit);
        Assert.Equal(400, (await CatalogListHttp.Execute(app, $"?parentCategoryId={parent}&rootsOnly=true")).Response.StatusCode);
        Assert.Equal(1, repository.Calls);
    }

    [Fact]
    public async Task BoundEndpoint_EmptyPageAndRootFilterRemainValid()
    {
        var repository = new RepositoryFake { Empty = true };
        await using var app = CreateApp(repository);
        var response = await CatalogListHttp.Execute(app, "?offset=100&rootsOnly=true");
        Assert.Equal(200, response.Response.StatusCode);
        Assert.True(repository.RootsOnly);
        using var json = await JsonDocument.ParseAsync(response.Response.Body);
        Assert.Empty(json.RootElement.EnumerateArray());
    }

    private static WebApplication CreateApp(RepositoryFake repository)
    {
        var builder = WebApplication.CreateBuilder();
        builder.Services.AddSingleton(new UseCase(repository));
        var app = builder.Build();
        app.MapListCategories();
        return app;
    }

    private sealed class RepositoryFake : ICategoryListRepository
    {
        public CategoryListItem Item { get; } = new(Guid.NewGuid(), "Category", null, 3);
        public bool Empty { get; init; }
        public int Calls { get; private set; }
        public int Offset { get; private set; }
        public int Limit { get; private set; }
        public string? Search { get; private set; }
        public CategoryId? Parent { get; private set; }
        public bool RootsOnly { get; private set; }
        public CancellationToken Cancellation { get; private set; }
        public Task<IReadOnlyList<CategoryListItem>> ListAsync(int offset, int limit, string? searchTerm,
            CategoryId? parentCategoryId, bool rootsOnly, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Calls++;
            Offset = offset;
            Limit = limit;
            Search = searchTerm;
            Parent = parentCategoryId;
            RootsOnly = rootsOnly;
            Cancellation = cancellationToken;
            return Task.FromResult<IReadOnlyList<CategoryListItem>>(Empty ? [] : [Item]);
        }
    }
}
