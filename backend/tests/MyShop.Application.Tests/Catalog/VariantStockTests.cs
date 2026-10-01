using MyShop.Application.Catalog.Abstractions;
using MyShop.Application.Catalog.ClearProductVariantStock;
using MyShop.Application.Catalog.SetProductVariantStock;
using MyShop.Domain.Catalog;

namespace MyShop.Application.Tests.Catalog;

public sealed class VariantStockTests
{
    [Fact]
    public async Task SetsAndClearsTrackedStockThroughProductConcurrencyBoundary()
    {
        var repository = new Repository();
        var variant = repository.Product.Variants.Single();
        var set = await new SetProductVariantStock(repository).ExecuteAsync(
            new(repository.Product.Id, variant.Id, 0), CancellationToken.None);
        Assert.Null(set.Failure);
        Assert.Equal(0, variant.StockQuantity);
        Assert.Equal(1, repository.Saves);

        var clear = await new ClearProductVariantStock(repository).ExecuteAsync(
            new(repository.Product.Id, variant.Id), CancellationToken.None);
        Assert.Null(clear.Failure);
        Assert.Null(variant.StockQuantity);
        Assert.Equal(2, repository.Saves);
    }

    [Fact]
    public async Task RejectsNegativeStockBeforeReading()
    {
        var repository = new Repository();
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() =>
            new SetProductVariantStock(repository).ExecuteAsync(new(repository.Product.Id,
                repository.Product.Variants.Single().Id, -1), CancellationToken.None));
        Assert.Equal(0, repository.Reads);
    }

    private sealed class Repository : IProductRepository
    {
        public Product Product { get; } = Product.Create("Shirt", ProductTypeId.New(), "Small");
        public int Reads { get; private set; }
        public int Saves { get; private set; }
        public Task<ProductSnapshot?> GetByIdAsync(ProductId id, CancellationToken cancellationToken)
        {
            Reads++;
            return Task.FromResult<ProductSnapshot?>(new(Product,
                ProductConcurrencyToken.Create(Product.Id, Guid.NewGuid())));
        }
        public Task<ProductConcurrencyToken> SaveAsync(Product product,
            ProductConcurrencyToken expectedToken, CancellationToken cancellationToken)
        {
            Saves++;
            return Task.FromResult(ProductConcurrencyToken.Create(product.Id, Guid.NewGuid()));
        }
        public Task<ProductConcurrencyToken> AddAsync(Product product,
            CancellationToken cancellationToken) => throw new NotSupportedException();
    }
}
