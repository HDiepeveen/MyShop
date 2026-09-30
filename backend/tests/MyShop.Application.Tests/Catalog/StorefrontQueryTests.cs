using MyShop.Application.Catalog.Abstractions;
using MyShop.Application.Catalog.BrowseStorefront;
using MyShop.Application.Catalog.GetStorefrontProduct;
using MyShop.Domain.Catalog;

namespace MyShop.Application.Tests.Catalog;

public sealed class StorefrontQueryTests
{
    [Fact]
    public async Task NormalizesSearchAndForwardsPaginationAndCancellation()
    {
        var catalog = new Catalog();
        var useCase = new BrowseStorefront(catalog);
        using var source = new CancellationTokenSource();
        await useCase.ExecuteAsync(new(20, 10, " shirt "), source.Token);
        Assert.Equal((20, 10, "shirt"), catalog.Request);
        Assert.Equal(source.Token, catalog.Cancellation);
        await useCase.ExecuteAsync(new(0, 20, " "), CancellationToken.None);
        Assert.Null(catalog.Request.Search);
    }
    [Theory]
    [InlineData(-1, 20)]
    [InlineData(0, 0)]
    [InlineData(0, 101)]
    public async Task RejectsInvalidPagingBeforeReading(int offset, int limit)
    {
        var catalog = new Catalog();
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => new BrowseStorefront(catalog).ExecuteAsync(new(offset, limit), CancellationToken.None));
        Assert.Equal(0, catalog.Calls);
    }
    [Fact]
    public async Task RejectsLongSearchAndCancelledRequests()
    {
        var catalog = new Catalog();
        var useCase = new BrowseStorefront(catalog);
        await Assert.ThrowsAsync<ArgumentException>(() => useCase.ExecuteAsync(new(0, 20, new string('a', 201)), CancellationToken.None));
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => useCase.ExecuteAsync(new(), new CancellationToken(true)));
        Assert.Equal(0, catalog.Calls);
    }
    [Fact]
    public async Task DetailForwardsIdAndPreservesMissingResult()
    {
        var catalog = new Catalog();
        var useCase = new GetStorefrontProduct(catalog);
        var id = ProductId.New();
        Assert.Null(await useCase.ExecuteAsync(new(id), CancellationToken.None));
        Assert.Equal(id, catalog.Id);
        await Assert.ThrowsAsync<ArgumentException>(() => useCase.ExecuteAsync(new(default), CancellationToken.None));
    }
    private sealed class Catalog : IStorefrontCatalog
    {
        internal int Calls;
        internal (int Offset, int Limit, string? Search) Request;
        internal CancellationToken Cancellation;
        internal ProductId Id;
        public Task<StorefrontPage> ListAsync(int offset, int limit, string? search, CancellationToken cancellationToken)
        { Calls++; Request = (offset, limit, search); Cancellation = cancellationToken; return Task.FromResult(new StorefrontPage([], 0, offset, limit)); }
        public Task<StorefrontProduct?> GetAsync(ProductId id, CancellationToken cancellationToken)
        { Calls++; Id = id; return Task.FromResult<StorefrontProduct?>(null); }
    }
}
