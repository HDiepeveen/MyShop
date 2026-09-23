using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using MyShop.Api.Catalog.ProductTypes;
using MyShop.Application.Catalog.Abstractions;
using UseCase = MyShop.Application.Catalog.ListProductTypes.ListProductTypes;

namespace MyShop.Api.Tests.Catalog.ProductTypes;

public sealed class ListProductTypesHttpTests
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
        Assert.Equal("Type", item.GetProperty("name").GetString());
        Assert.Equal(3, item.GetProperty("attributeDefinitionCount").GetInt32());
    }

    [Theory]
    [InlineData("?offset=-1")]
    [InlineData("?limit=0")]
    [InlineData("?limit=101")]
    [InlineData("?offset=invalid")]
    [InlineData("?limit=2147483648")]
    [InlineData("?search=%20")]
    public async Task BoundEndpoint_InvalidQueryIsRejectedBeforeRead(string query)
    {
        var repository = new RepositoryFake();
        await using var app = CreateApp(repository);
        Assert.Equal(400, (await CatalogListHttp.Execute(app, query)).Response.StatusCode);
        Assert.Equal(0, repository.Calls);
    }

    [Fact]
    public async Task BoundEndpoint_CombinesPagingWithTrimmedSearch()
    {
        var repository = new RepositoryFake();
        await using var app = CreateApp(repository);
        var response = await CatalogListHttp.Execute(app, "?offset=5&limit=10&search=%20shirt%20");
        Assert.Equal(200, response.Response.StatusCode);
        Assert.Equal("shirt", repository.Search);
        Assert.Equal(5, repository.Offset);
        Assert.Equal(10, repository.Limit);
        Assert.Equal(1, repository.Calls);
    }

    [Fact]
    public async Task BoundEndpoint_EmptyPageRemainsValid()
    {
        var repository = new RepositoryFake { Empty = true };
        await using var app = CreateApp(repository);
        var response = await CatalogListHttp.Execute(app, "?offset=100");
        Assert.Equal(200, response.Response.StatusCode);
        using var json = await JsonDocument.ParseAsync(response.Response.Body);
        Assert.Empty(json.RootElement.EnumerateArray());
    }

    private static WebApplication CreateApp(RepositoryFake repository)
    {
        var builder = WebApplication.CreateBuilder();
        builder.Services.AddSingleton(new UseCase(repository));
        var app = builder.Build();
        app.MapListProductTypes();
        return app;
    }

    private sealed class RepositoryFake : IProductTypeListRepository
    {
        public ProductTypeListItem Item { get; } = new(Guid.NewGuid(), "Type", 3);
        public bool Empty { get; init; }
        public int Calls { get; private set; }
        public int Offset { get; private set; }
        public int Limit { get; private set; }
        public string? Search { get; private set; }
        public CancellationToken Cancellation { get; private set; }
        public Task<IReadOnlyList<ProductTypeListItem>> ListAsync(int offset, int limit, string? searchTerm,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Calls++;
            Offset = offset;
            Limit = limit;
            Search = searchTerm;
            Cancellation = cancellationToken;
            return Task.FromResult<IReadOnlyList<ProductTypeListItem>>(Empty ? [] : [Item]);
        }
    }
}
