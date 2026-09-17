using MyShop.Application.Catalog.Abstractions;
using MyShop.Application.Catalog.GetProductBySku;
using MyShop.Domain.Catalog;
using UseCase = MyShop.Application.Catalog.GetProductBySku.GetProductBySku;

namespace MyShop.Application.Tests.Catalog.GetProductBySku;

public sealed class GetProductBySkuTests
{
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task ExecuteAsync_ReturnsLookupResultAndForwardsArguments(bool found)
    {
        var sku = Sku.Create("shirt-blue-medium");
        var owner = found ? new ProductSkuOwner(ProductId.New(), ProductVariantId.New()) : null;
        var lookup = new ProductSkuLookupFake { Owner = owner };
        var useCase = new UseCase(lookup);
        using var source = new CancellationTokenSource();

        var result = await useCase.ExecuteAsync(new GetProductBySkuQuery(sku), source.Token);

        Assert.Same(owner, result);
        Assert.Same(sku, lookup.Sku);
        Assert.Equal(source.Token, lookup.Token);
    }

    [Fact]
    public async Task ExecuteAsync_RejectsInvalidArgumentsBeforeLookup()
    {
        var lookup = new ProductSkuLookupFake();
        var useCase = new UseCase(lookup);

        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            useCase.ExecuteAsync(null!, CancellationToken.None));
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            useCase.ExecuteAsync(new GetProductBySkuQuery(null!), CancellationToken.None));

        Assert.Equal(0, lookup.FindCalls);
    }

    [Fact]
    public async Task ExecuteAsync_WhenPreCancelled_DoesNotAccessLookup()
    {
        var lookup = new ProductSkuLookupFake();
        var useCase = new UseCase(lookup);
        using var source = new CancellationTokenSource();
        source.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            useCase.ExecuteAsync(new GetProductBySkuQuery(Sku.Create("sku")), source.Token));

        Assert.Equal(0, lookup.FindCalls);
    }

    [Fact]
    public void Constructor_RejectsNullLookup() =>
        Assert.Throws<ArgumentNullException>(() => new UseCase(null!));

    private sealed class ProductSkuLookupFake : IProductSkuLookup
    {
        public ProductSkuOwner? Owner { get; set; }
        public int FindCalls { get; private set; }
        public Sku? Sku { get; private set; }
        public CancellationToken Token { get; private set; }

        public Task<ProductSkuOwner?> FindOwnerAsync(Sku sku, CancellationToken cancellationToken)
        {
            FindCalls++;
            Sku = sku;
            Token = cancellationToken;
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(Owner);
        }
    }
}
