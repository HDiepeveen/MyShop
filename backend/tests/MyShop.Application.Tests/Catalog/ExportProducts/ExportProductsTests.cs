using MyShop.Application.Catalog.Abstractions;
using MyShop.Domain.Catalog;
using UseCase = MyShop.Application.Catalog.ExportProducts.ExportProducts;
using Query = MyShop.Application.Catalog.ExportProducts.ExportProductsQuery;
namespace MyShop.Application.Tests.Catalog.ExportProducts;
public sealed class ExportProductsTests
{
    [Fact]
    public async Task UsesBoundedReadAndPreservesTheSelection()
    {
        var repo = new Repository(); var type = ProductTypeId.New(); var category = CategoryId.New(); using var source = new CancellationTokenSource();
        await new UseCase(repo).ExecuteAsync(new(type, category, " shirt ", false, ProductStockFilter.Low), source.Token);
        Assert.Equal(0, repo.Offset); Assert.Equal(1000, repo.Limit); Assert.Equal(type, repo.Type); Assert.Equal(category, repo.Category);
        Assert.Equal("shirt", repo.Search); Assert.False(repo.Published); Assert.Equal(ProductStockFilter.Low, repo.Stock); Assert.Equal(source.Token, repo.Token);
    }
    [Fact]
    public async Task CapsReturnedRowsAndKeepsTotalCount()
    {
        var repo = new Repository { Page = new(Enumerable.Range(0, 1001).Select(i => new ProductListItem(Guid.NewGuid(), Guid.NewGuid(), "Product", 1)).ToArray(), 1001) };
        var page = await new UseCase(repo).ExecuteAsync(new(), default); Assert.Equal(1000, page.Items.Count); Assert.Equal(1001, page.TotalCount);
    }
    [Fact]
    public async Task InvalidOrCancelledRequestsDoNotRead()
    {
        var repo = new Repository(); var useCase = new UseCase(repo);
        await Assert.ThrowsAsync<ArgumentException>(() => useCase.ExecuteAsync(new(Search: "  "), default));
        await Assert.ThrowsAsync<ArgumentException>(() => useCase.ExecuteAsync(new(ProductTypeId: default(ProductTypeId)), default));
        await Assert.ThrowsAsync<ArgumentException>(() => useCase.ExecuteAsync(new(CategoryId: default(CategoryId)), default));
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => useCase.ExecuteAsync(new(Stock: (ProductStockFilter)99), default));
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => useCase.ExecuteAsync(new(), new CancellationToken(true))); Assert.Equal(0, repo.Calls);
    }
    private sealed class Repository : IProductListRepository
    {
        public ProductListPage Page = new([], 0); public int Calls, Offset, Limit; public ProductTypeId? Type; public CategoryId? Category;
        public string? Search; public bool? Published; public ProductStockFilter? Stock; public CancellationToken Token;
        public Task<ProductListPage> ListAsync(int offset, int limit, ProductTypeId? productTypeId, CategoryId? categoryId, string? searchTerm, CancellationToken cancellationToken, bool? isPublished = null, ProductStockFilter? stock = null)
        { Calls++; Offset = offset; Limit = limit; Type = productTypeId; Category = categoryId; Search = searchTerm; Published = isPublished; Stock = stock; Token = cancellationToken; return Task.FromResult(Page); }
    }
}
