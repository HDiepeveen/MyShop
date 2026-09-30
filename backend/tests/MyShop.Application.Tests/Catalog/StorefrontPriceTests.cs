using MyShop.Application.Catalog.Abstractions;
using MyShop.Application.Catalog.GetStorefrontPrices;
using MyShop.Domain.Catalog;

namespace MyShop.Application.Tests.Catalog;

public sealed class StorefrontPriceTests
{
    private static readonly DateTimeOffset Start = new(2026, 9, 30, 12, 0, 0, TimeSpan.Zero);

    [Theory]
    [InlineData(-1, 90)]
    [InlineData(0, 75)]
    [InlineData(1, 75)]
    [InlineData(2, 90)]
    public async Task UsesExistingPriorityAndInclusiveDateRulesAtOneInstant(int hours, int expected)
    {
        var repository = new Repository();
        var variant = repository.Product.Variants.Single();
        repository.Product.SetVariantPrice(variant.Id, Money.Create(100, "EUR"));
        repository.Product.AddVariantPriceRule(variant.Id, PriceRule.Create("Always", PriceAdjustmentType.FixedDiscount, 10, 0));
        repository.Product.AddVariantPriceRule(variant.Id, PriceRule.Create("Scheduled", PriceAdjustmentType.PercentageDiscount, 25, 5, Start, Start.AddHours(1)));
        var free = repository.Product.AddVariant("Free");
        repository.Product.SetVariantPrice(free.Id, Money.Create(0, "USD"));
        var missing = repository.Product.AddVariant("Unpriced");
        using var source = new CancellationTokenSource();
        var result = await new GetStorefrontPrices(repository).ExecuteAsync(new(repository.Product.Id, Start.AddHours(hours)), source.Token);
        Assert.NotNull(result);
        Assert.Equal(Start.AddHours(hours), result.At);
        Assert.Equal(new StorefrontVariantPrice(variant.Id.Value, expected, "EUR"), result.Variants[0]);
        Assert.Equal(new StorefrontVariantPrice(free.Id.Value, 0, "USD"), result.Variants[1]);
        Assert.Equal(new StorefrontVariantPrice(missing.Id.Value, null, null), result.Variants[2]);
        Assert.Equal(source.Token, repository.Cancellation);
        Assert.Equal(100m, variant.Price!.Value.Amount);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task HidesDraftAndMissingProducts(bool missing)
    {
        var repository = new Repository { Missing = missing };
        repository.Product.SetPresentation(ProductPresentation.Draft);
        Assert.Null(await new GetStorefrontPrices(repository).ExecuteAsync(new(repository.Product.Id, Start), CancellationToken.None));
    }

    [Fact]
    public async Task ValidatesInputAndCancellationBeforeReading()
    {
        var repository = new Repository();
        var useCase = new GetStorefrontPrices(repository);
        await Assert.ThrowsAsync<ArgumentNullException>(() => useCase.ExecuteAsync(null!, CancellationToken.None));
        await Assert.ThrowsAsync<ArgumentException>(() => useCase.ExecuteAsync(new(default, Start), CancellationToken.None));
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => useCase.ExecuteAsync(new(repository.Product.Id, Start), new CancellationToken(true)));
        Assert.Equal(0, repository.Calls);
    }

    private sealed class Repository : IProductRepository
    {
        public Product Product { get; } = Product.Create("Shirt", ProductTypeId.New(), "Small");
        public bool Missing { get; init; }
        public int Calls;
        public CancellationToken Cancellation;
        public Repository() => Product.SetPresentation(ProductPresentation.Create("Shirt", "https://example.com/shirt.jpg", "Shirt", true));
        public Task<ProductSnapshot?> GetByIdAsync(ProductId id, CancellationToken cancellationToken)
        {
            Assert.Equal(Product.Id, id);
            Calls++;
            Cancellation = cancellationToken;
            return Task.FromResult(Missing ? null : new ProductSnapshot(Product, ProductConcurrencyToken.Create(Product.Id, Guid.NewGuid())));
        }
        public Task<ProductConcurrencyToken> AddAsync(Product product, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<ProductConcurrencyToken> SaveAsync(Product product, ProductConcurrencyToken expectedToken, CancellationToken cancellationToken) => throw new NotSupportedException();
    }
}
