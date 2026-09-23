using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using MyShop.Api.Catalog.Products;
using MyShop.Application.Catalog.Abstractions;
using MyShop.Domain.Catalog;
using UseCase = MyShop.Application.Catalog.ListProducts.ListProducts;

namespace MyShop.Api.Tests.Catalog.Products;

public sealed class ListProductsHttpTests
{
    [Theory]
    [InlineData("", 0, 50)]
    [InlineData("?offset=2&limit=1", 2, 1)]
    [InlineData("?limit=100", 0, 100)]
    [InlineData("?offset=2147483647", 2147483647, 50)]
    public async Task BoundEndpoint_ParsesPageAndPreservesPageMetadata(string query, int offset, int limit)
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
        Assert.Equal(offset, json.RootElement.GetProperty("offset").GetInt32());
        Assert.Equal(limit, json.RootElement.GetProperty("limit").GetInt32());
        Assert.Equal(123, json.RootElement.GetProperty("totalCount").GetInt32());
        var item = Assert.Single(json.RootElement.GetProperty("items").EnumerateArray());
        Assert.Equal(repository.Item.Id, item.GetProperty("id").GetGuid());
        Assert.Equal(repository.Item.ProductTypeId, item.GetProperty("productTypeId").GetGuid());
        Assert.Equal(3, item.GetProperty("variantCount").GetInt32());
    }

    [Theory]
    [InlineData("?offset=-1")]
    [InlineData("?limit=0")]
    [InlineData("?limit=101")]
    [InlineData("?offset=invalid")]
    [InlineData("?limit=2147483648")]
    [InlineData("?productTypeId=invalid")]
    [InlineData("?categoryId=invalid")]
    [InlineData("?categoryId=00000000-0000-0000-0000-000000000000")]
    [InlineData("?search=%20")]
    public async Task BoundEndpoint_InvalidQueryIsRejectedBeforeRead(string query)
    {
        var repository = new RepositoryFake();
        await using var app = CreateApp(repository);
        Assert.Equal(400, (await CatalogListHttp.Execute(app, query)).Response.StatusCode);
        Assert.Equal(0, repository.Calls);
    }

    [Fact]
    public async Task BoundEndpoint_CombinesPageAndAllFilters()
    {
        var repository = new RepositoryFake();
        await using var app = CreateApp(repository);
        var productType = Guid.NewGuid();
        var category = Guid.NewGuid();
        var response = await CatalogListHttp.Execute(app,
            $"?offset=5&limit=10&search=%20shirt%20&productTypeId={productType}&categoryId={category}");
        Assert.Equal(200, response.Response.StatusCode);
        Assert.Equal("shirt", repository.Search);
        Assert.Equal(ProductTypeId.From(productType), repository.ProductType);
        Assert.Equal(CategoryId.From(category), repository.Category);
        Assert.Equal(5, repository.Offset);
        Assert.Equal(10, repository.Limit);
    }

    [Fact]
    public async Task BoundEndpoint_EmptyPageKeepsTotalCount()
    {
        var repository = new RepositoryFake { Empty = true };
        await using var app = CreateApp(repository);
        var response = await CatalogListHttp.Execute(app, "?offset=200");
        Assert.Equal(200, response.Response.StatusCode);
        using var json = await JsonDocument.ParseAsync(response.Response.Body);
        Assert.Empty(json.RootElement.GetProperty("items").EnumerateArray());
        Assert.Equal(123, json.RootElement.GetProperty("totalCount").GetInt32());
    }

    private static WebApplication CreateApp(RepositoryFake repository)
    {
        var builder = WebApplication.CreateBuilder();
        builder.Services.AddSingleton(new UseCase(repository));
        var app = builder.Build();
        app.MapListProducts();
        return app;
    }

    private sealed class RepositoryFake : IProductListRepository
    {
        public ProductListItem Item { get; } = new(Guid.NewGuid(), Guid.NewGuid(), "Product", 3);
        public bool Empty { get; init; }
        public int Calls { get; private set; }
        public int Offset { get; private set; }
        public int Limit { get; private set; }
        public string? Search { get; private set; }
        public ProductTypeId? ProductType { get; private set; }
        public CategoryId? Category { get; private set; }
        public CancellationToken Cancellation { get; private set; }
        public Task<ProductListPage> ListAsync(int offset, int limit, ProductTypeId? productTypeId,
            CategoryId? categoryId, string? searchTerm, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Calls++;
            Offset = offset;
            Limit = limit;
            Search = searchTerm;
            ProductType = productTypeId;
            Category = categoryId;
            Cancellation = cancellationToken;
            return Task.FromResult(new ProductListPage(Empty ? [] : [Item], 123));
        }
    }
}
