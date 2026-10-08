using MyShop.Application.Catalog;
using MyShop.Application.Catalog.Abstractions;
using MyShop.Application.Catalog.GenerateProductVariants;
using MyShop.Domain.Catalog;
using UseCase = MyShop.Application.Catalog.GenerateProductVariants.GenerateProductVariants;

namespace MyShop.Application.Tests.Catalog;

public sealed class GenerateProductVariantsTests
{
    [Fact]
    public async Task Adds_new_combinations_once_and_preserves_existing_price_stock_and_sku()
    {
        var s = new Scenario();
        var existing = s.Product.Variants.Single();
        s.Product.SetVariantAttributeValue(existing.Id, ChoiceAttributeValue.Create(s.Size, ChoiceValue.Create("M")));
        s.Product.SetVariantPrice(existing.Id, Money.Create(42, "EUR"), 21);
        s.Product.SetVariantStockQuantity(existing.Id, 7);
        s.Product.SetVariantSku(existing.Id, Sku.Create("TS-M"));
        var command = s.Command(s.Combination("M"), s.Combination("L")) with { NetAmount = 10, VatRate = 21, StockQuantity = 3 };
        var result = await s.Handler.ExecuteAsync(command, CancellationToken.None);
        Assert.Equal(1, result.Added);
        Assert.Equal(1, result.Skipped);
        Assert.Equal(1, s.Saves);
        Assert.Equal(42, existing.Price!.Value.Amount);
        Assert.Equal(7, existing.StockQuantity);
        Assert.Equal("TS-M", existing.Sku!.Value);
        var added = s.Product.Variants.Last();
        Assert.Equal(12.10m, added.Price!.Value.Amount);
        Assert.Equal(10m, added.NetPriceAmount);
        Assert.Equal(3, added.StockQuantity);
        var repeated = await s.Handler.ExecuteAsync(command with { Revision = s.Revision }, CancellationToken.None);
        Assert.Equal(0, repeated.Added);
        Assert.Equal(2, repeated.Skipped);
        Assert.Equal(1, s.Saves);
    }

    [Fact]
    public async Task Invalid_later_combination_does_not_partially_mutate_or_save()
    {
        var s = new Scenario();
        var invalid = new VariantCombination("Invalid", [new(s.Size, new IntegerAttributeValueInput(1))]);
        await Assert.ThrowsAsync<ArgumentException>(() => s.Handler.ExecuteAsync(s.Command(s.Combination("L"), invalid), CancellationToken.None));
        Assert.Single(s.Product.Variants);
        Assert.Equal(0, s.Saves);
    }

    [Fact]
    public async Task Duplicate_combinations_are_rejected_even_with_different_names_or_whitespace()
    {
        var s = new Scenario();
        await Assert.ThrowsAsync<ArgumentException>(() => s.Handler.ExecuteAsync(
            s.Command(s.Combination("L"), s.Combination(" L ") with { Name = "Other" }), CancellationToken.None));
        Assert.Single(s.Product.Variants);
    }

    [Fact]
    public async Task Stale_revision_and_unavailable_tax_do_not_change_product()
    {
        var s = new Scenario();
        await Assert.ThrowsAsync<ProductConcurrencyException>(() => s.Handler.ExecuteAsync(
            s.Command(s.Combination("L")) with { Revision = Guid.NewGuid() }, CancellationToken.None));
        s.RateAvailable = false;
        await Assert.ThrowsAsync<ArgumentException>(() => s.Handler.ExecuteAsync(
            s.Command(s.Combination("L")) with { NetAmount = 1, VatRate = 21 }, CancellationToken.None));
        Assert.Single(s.Product.Variants);
        Assert.Equal(0, s.Saves);
    }

    [Fact]
    public async Task Missing_dimensions_and_product_scope_are_rejected()
    {
        var s = new Scenario();
        s.Type.AddAttribute(AttributeDefinitionId.New(), AttributeCode.Create("colour"), "Colour", AttributeDataType.Text, false, false, AttributeScope.Variant);
        await Assert.ThrowsAsync<ArgumentException>(() => s.Handler.ExecuteAsync(s.Command(s.Combination("L")), CancellationToken.None));
        Assert.Single(s.Product.Variants);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(101)]
    public async Task Empty_or_excessive_batch_is_rejected(int count)
    {
        var s = new Scenario();
        await Assert.ThrowsAsync<ArgumentException>(() => s.Handler.ExecuteAsync(
            s.Command(Enumerable.Range(0, count).Select(n => s.Combination(n.ToString())).ToArray()), CancellationToken.None));
        Assert.Equal(0, s.Saves);
    }

    [Fact]
    public async Task Save_conflict_and_cancellation_are_propagated()
    {
        var s = new Scenario { Conflict = true };
        await Assert.ThrowsAsync<ProductConcurrencyException>(() => s.Handler.ExecuteAsync(s.Command(s.Combination("L")), CancellationToken.None));
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => s.Handler.ExecuteAsync(s.Command(s.Combination("XL")), cancellation.Token));
        Assert.Equal(1, s.Saves);
    }

    private sealed class Scenario : IProductRepository, IProductTypeRepository, IVatRateAvailability
    {
        public ProductType Type { get; } = ProductType.Create("Shirt");
        public AttributeDefinitionId Size { get; } = AttributeDefinitionId.New();
        public Product Product { get; }
        public Guid Revision { get; private set; } = Guid.NewGuid();
        public int Saves { get; private set; }
        public bool RateAvailable { get; set; } = true;
        public bool Conflict { get; set; }
        public UseCase Handler { get; }
        public Scenario()
        {
            Type.AddAttribute(Size, AttributeCode.Create("size"), "Size", AttributeDataType.Choice, true, true, AttributeScope.Variant);
            Product = Product.Create("Shirt", Type.Id, "Initial");
            Handler = new(this, this, this);
        }
        public VariantCombination Combination(string size) => new(size, [new(Size, new ChoiceAttributeValueInput(size))]);
        public GenerateProductVariantsCommand Command(params VariantCombination[] combinations) => new(Product.Id, Revision, combinations);
        public Task<ProductType?> GetByIdAsync(ProductTypeId id, CancellationToken ct) => Task.FromResult<ProductType?>(Type);
        public Task<ProductSnapshot?> GetByIdAsync(ProductId id, CancellationToken ct) =>
            Task.FromResult<ProductSnapshot?>(new(Product, ProductConcurrencyToken.Create(Product.Id, Revision)));
        public Task<ProductConcurrencyToken> AddAsync(Product product, CancellationToken ct) => throw new NotSupportedException();
        public Task<ProductConcurrencyToken> SaveAsync(Product product, ProductConcurrencyToken token, CancellationToken ct)
        {
            Saves++;
            if (Conflict) throw new ProductConcurrencyException(product.Id);
            Revision = Guid.NewGuid();
            return Task.FromResult(ProductConcurrencyToken.Create(product.Id, Revision));
        }
        public Task<bool> IsAvailableAsync(decimal rate, bool exempt, CancellationToken ct) => Task.FromResult(RateAvailable);
    }
}
