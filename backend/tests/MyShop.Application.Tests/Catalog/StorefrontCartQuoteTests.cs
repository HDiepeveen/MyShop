using MyShop.Application.Catalog.Abstractions;
using MyShop.Application.Catalog.QuoteStorefrontCart;
using MyShop.Domain.Catalog;

namespace MyShop.Application.Tests.Catalog;

public sealed class StorefrontCartQuoteTests
{
    private static readonly DateTimeOffset At = new(2026, 9, 30, 12, 0, 0, TimeSpan.Zero);

    [Theory]
    [InlineData(-1, 100)]
    [InlineData(0, 75)]
    [InlineData(1, 75)]
    [InlineData(2, 100)]
    public async Task QuotesOncePerProductAtOneInstantAndSeparatesCurrencies(int hours, int expected)
    {
        var repository = new Repository();
        var first = repository.Product.Variants.Single();
        repository.Product.SetVariantPrice(first.Id, Money.Create(100, "EUR"));
        repository.Product.AddVariantPriceRule(first.Id, PriceRule.Create("Offer", PriceAdjustmentType.PercentageDiscount, 25, 1, At, At.AddHours(1)));
        var second = repository.Product.AddVariant("Second");
        repository.Product.SetVariantPrice(second.Id, Money.Create(2.5m, "EUR"));
        var free = repository.Product.AddVariant("Free");
        repository.Product.SetVariantPrice(free.Id, Money.Create(0, "USD"));
        using var source = new CancellationTokenSource();
        var result = await new QuoteStorefrontCart(repository).ExecuteAsync(new([
            new(repository.Product.Id.Value, first.Id.Value, 3),
            new(repository.Product.Id.Value, second.Id.Value, 2),
            new(repository.Product.Id.Value, free.Id.Value, 99)], At.AddHours(hours)), source.Token);
        Assert.Equal(1, repository.Calls);
        Assert.Equal(source.Token, repository.Cancellation);
        Assert.Equal(At.AddHours(hours), result.At);
        Assert.Equal(expected * 3, result.Lines[0].Total);
        Assert.Equal(new[] { new CartQuotedTotal("EUR", expected * 3 + 5), new CartQuotedTotal("USD", 0) }, result.Totals);
        Assert.Equal(first.Id.Value, result.Lines[0].VariantId);
        Assert.All(result.Lines, line => Assert.Null(line.Failure));
    }

    [Theory]
    [InlineData("draft")]
    [InlineData("missingProduct")]
    [InlineData("missingVariant")]
    [InlineData("noPrice")]
    [InlineData("outOfStock")]
    public async Task WithholdsTotalsAndInternalDetailsWhenAnyLineIsUnavailable(string scenario)
    {
        var repository = new Repository();
        var first = repository.Product.Variants.Single();
        var valid = repository.Product.AddVariant("Priced");
        repository.Product.SetVariantPrice(valid.Id, Money.Create(10, "EUR"));
        if (scenario == "draft") repository.Product.SetPresentation(ProductPresentation.Draft);
        var line = new CartQuoteLine(scenario == "missingProduct" ? Guid.NewGuid() : repository.Product.Id.Value,
            scenario == "missingVariant" ? Guid.NewGuid() : first.Id.Value, 1);
        if (scenario == "outOfStock") repository.Product.SetVariantStockQuantity(first.Id, 0);
        var result = await new QuoteStorefrontCart(repository).ExecuteAsync(new([line, new(repository.Product.Id.Value, valid.Id.Value, 1)], At), CancellationToken.None);
        Assert.Empty(result.Totals);
        Assert.Equal(scenario switch { "noPrice" => "priceMissing", "outOfStock" => "outOfStock",
            _ => "unavailable" }, result.Lines[0].Failure);
        Assert.Null(result.Lines[0].Amount);
        Assert.Null(result.Lines[0].Total);
        if (scenario is not "noPrice" and not "outOfStock")
        {
            Assert.Null(result.Lines[0].Name);
            Assert.Null(result.Lines[0].Variant);
        }
    }

    [Fact]
    public async Task RetainsExactLargeAmountsAndSupportsEmptyCart()
    {
        var repository = new Repository();
        var variant = repository.Product.Variants.Single();
        repository.Product.SetVariantPrice(variant.Id, Money.Create(9999999999999999.99m, "EUR"));
        var result = await new QuoteStorefrontCart(repository).ExecuteAsync(new([new(repository.Product.Id.Value, variant.Id.Value, 99)], At), CancellationToken.None);
        Assert.Equal(989999999999999999.01m, Assert.Single(result.Totals).Amount);
        var empty = await new QuoteStorefrontCart(repository).ExecuteAsync(new([], At), CancellationToken.None);
        Assert.Empty(empty.Lines); Assert.Empty(empty.Totals);
        Assert.Equal(1, repository.Calls);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(100)]
    public async Task RejectsBadQuantitiesBeforeReading(int quantity)
    {
        var repository = new Repository();
        await Assert.ThrowsAsync<ArgumentException>(() => new QuoteStorefrontCart(repository).ExecuteAsync(new([new(repository.Product.Id.Value, repository.Product.Variants.Single().Id.Value, quantity)], At), CancellationToken.None));
        Assert.Equal(0, repository.Calls);
    }

    [Fact]
    public async Task RejectsNullEmptyIdsDuplicatesOversizeAndCancellationBeforeReading()
    {
        var repository = new Repository(); var useCase = new QuoteStorefrontCart(repository);
        var line = new CartQuoteLine(repository.Product.Id.Value, repository.Product.Variants.Single().Id.Value, 1);
        await Assert.ThrowsAsync<ArgumentNullException>(() => useCase.ExecuteAsync(null!, CancellationToken.None));
        await Assert.ThrowsAsync<ArgumentNullException>(() => useCase.ExecuteAsync(new(null!, At), CancellationToken.None));
        foreach (var lines in new IReadOnlyList<CartQuoteLine>[] { [null!], [line with { ProductId = Guid.Empty }], [line with { VariantId = Guid.Empty }], [line, line], Enumerable.Range(0, 21).Select(_ => line with { VariantId = Guid.NewGuid() }).ToArray() })
            await Assert.ThrowsAsync<ArgumentException>(() => useCase.ExecuteAsync(new(lines, At), CancellationToken.None));
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => useCase.ExecuteAsync(new([line], At), new CancellationToken(true)));
        Assert.Equal(0, repository.Calls);
    }

    private sealed class Repository : IProductRepository
    {
        public Product Product { get; } = Product.Create("Shirt", ProductTypeId.New(), "Small");
        public int Calls;
        public CancellationToken Cancellation;
        public Repository() => Product.SetPresentation(ProductPresentation.Create("Shirt", "https://example.com/shirt.jpg", "Shirt", true));
        public Task<ProductSnapshot?> GetByIdAsync(ProductId id, CancellationToken cancellationToken)
        {
            Calls++; Cancellation = cancellationToken;
            return Task.FromResult(id != Product.Id ? null : new ProductSnapshot(Product, ProductConcurrencyToken.Create(Product.Id, Guid.NewGuid())));
        }
        public Task<ProductConcurrencyToken> AddAsync(Product product, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<ProductConcurrencyToken> SaveAsync(Product product, ProductConcurrencyToken expectedToken, CancellationToken cancellationToken) => throw new NotSupportedException();
    }
}
