using MyShop.Application.Catalog.Abstractions;
using MyShop.Application.Catalog.RemoveVariantAttributeValue;
using MyShop.Domain.Catalog;
using UseCase = MyShop.Application.Catalog.RemoveVariantAttributeValue.RemoveVariantAttributeValue;

namespace MyShop.Application.Tests.Catalog.RemoveVariantAttributeValue;

public sealed class RemoveVariantAttributeValueTests
{
    [Fact]
    public async Task ExecuteAsync_RemovesStoredValueWithoutDefinitionMetadataAndSavesSameProductOnce()
    {
        var scenario = new Scenario();
        using var source = new CancellationTokenSource();

        var result = await scenario.Handler.ExecuteAsync(scenario.Command, source.Token);

        Assert.True(result.IsSuccess);
        Assert.Null(result.Failure);
        Assert.Empty(scenario.Variant.AttributeValues);
        Assert.Equal(1, scenario.Products.GetCalls);
        Assert.Equal(scenario.Product.Id, scenario.Products.RequestedId);
        Assert.Equal(source.Token, scenario.Products.GetToken);
        Assert.Equal(1, scenario.Products.SaveCalls);
        Assert.Same(scenario.Product, scenario.Products.SavedProduct);
        Assert.Equal(source.Token, scenario.Products.SaveToken);
    }

    [Fact]
    public async Task ExecuteAsync_PreservesOtherValuesOnSelectedVariantOtherVariantAndProduct()
    {
        var scenario = new Scenario();
        var remaining = TextAttributeValue.Create(AttributeDefinitionId.New(), "Remaining");
        scenario.Product.SetVariantAttributeValue(scenario.Variant.Id, remaining);
        var other = scenario.Product.AddVariant("Other");
        scenario.Product.SetVariantAttributeValue(other.Id, scenario.Existing);
        scenario.Product.SetAttributeValue(scenario.Existing);
        scenario.Product.SetAttributeValue(remaining);

        var result = await scenario.Handler.ExecuteAsync(scenario.Command, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Same(remaining, Assert.Single(scenario.Variant.AttributeValues));
        Assert.Same(scenario.Existing, Assert.Single(other.AttributeValues));
        Assert.Equal(2, scenario.Product.AttributeValues.Count);
        Assert.Contains(scenario.Product.AttributeValues, value => ReferenceEquals(value, scenario.Existing));
        Assert.Contains(scenario.Product.AttributeValues, value => ReferenceEquals(value, remaining));
        Assert.Equal(1, scenario.Products.SaveCalls);
    }

    [Fact]
    public async Task ExecuteAsync_WhenProductIsMissing_ReturnsFailureAndNeverSaves()
    {
        var scenario = new Scenario();
        scenario.Products.Product = null;

        var result = await scenario.Handler.ExecuteAsync(scenario.Command, CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(RemoveVariantAttributeValueFailure.ProductNotFound, result.Failure);
        Assert.Equal(1, scenario.Products.GetCalls);
        Assert.Equal(0, scenario.Products.SaveCalls);
        Assert.Same(scenario.Existing, Assert.Single(scenario.Variant.AttributeValues));
    }

    [Fact]
    public async Task ExecuteAsync_WhenVariantBelongsToAnotherProduct_ReturnsFailureAndPreservesState()
    {
        var scenario = new Scenario();
        var foreignProduct = Product.Create("Foreign", ProductTypeId.New(), "Foreign");
        var foreignVariant = Assert.Single(foreignProduct.Variants);
        foreignProduct.SetVariantAttributeValue(foreignVariant.Id, scenario.Existing);
        scenario.Product.SetAttributeValue(scenario.Existing);

        var result = await scenario.Handler.ExecuteAsync(
            scenario.Command with { ProductVariantId = foreignVariant.Id }, CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(RemoveVariantAttributeValueFailure.VariantNotFound, result.Failure);
        Assert.Same(scenario.Existing, Assert.Single(scenario.Variant.AttributeValues));
        Assert.Same(scenario.Existing, Assert.Single(foreignVariant.AttributeValues));
        Assert.Same(scenario.Existing, Assert.Single(scenario.Product.AttributeValues));
        Assert.Equal(1, scenario.Products.GetCalls);
        Assert.Equal(0, scenario.Products.SaveCalls);
    }

    [Fact]
    public async Task ExecuteAsync_WhenValueIsMissing_SucceedsWithoutMutatingOrSaving()
    {
        var scenario = new Scenario();
        var missingId = AttributeDefinitionId.New();
        var other = scenario.Product.AddVariant("Other");
        var otherValue = TextAttributeValue.Create(missingId, "Other");
        scenario.Product.SetVariantAttributeValue(other.Id, otherValue);
        scenario.Product.SetAttributeValue(otherValue);

        var result = await scenario.Handler.ExecuteAsync(
            scenario.Command with { AttributeDefinitionId = missingId }, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Null(result.Failure);
        Assert.Same(scenario.Existing, Assert.Single(scenario.Variant.AttributeValues));
        Assert.Same(otherValue, Assert.Single(other.AttributeValues));
        Assert.Same(otherValue, Assert.Single(scenario.Product.AttributeValues));
        Assert.Equal(1, scenario.Products.GetCalls);
        Assert.Equal(0, scenario.Products.SaveCalls);
    }

    [Fact]
    public async Task ExecuteAsync_WhenRepeated_SucceedsTwiceAndSavesOnlyOnce()
    {
        var scenario = new Scenario();

        var first = await scenario.Handler.ExecuteAsync(scenario.Command, CancellationToken.None);
        var second = await scenario.Handler.ExecuteAsync(scenario.Command, CancellationToken.None);

        Assert.True(first.IsSuccess);
        Assert.True(second.IsSuccess);
        Assert.Empty(scenario.Variant.AttributeValues);
        Assert.Equal(2, scenario.Products.GetCalls);
        Assert.Equal(1, scenario.Products.SaveCalls);
    }

    [Fact]
    public async Task ExecuteAsync_WithDefaultDefinitionId_ThrowsBeforeRepositoryAccess()
    {
        var scenario = new Scenario();

        await Assert.ThrowsAsync<ArgumentException>(() => scenario.Handler.ExecuteAsync(
            scenario.Command with { AttributeDefinitionId = default }, CancellationToken.None));

        Assert.Equal(0, scenario.Products.GetCalls);
        Assert.Equal(0, scenario.Products.SaveCalls);
        Assert.Same(scenario.Existing, Assert.Single(scenario.Variant.AttributeValues));
    }

    [Fact]
    public async Task ExecuteAsync_WithNullCommand_ThrowsBeforeRepositoryAccess()
    {
        var scenario = new Scenario();

        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            scenario.Handler.ExecuteAsync(null!, CancellationToken.None));

        Assert.Equal(0, scenario.Products.GetCalls);
        Assert.Equal(0, scenario.Products.SaveCalls);
    }

    [Fact]
    public void Constructor_WithNullRepository_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => new UseCase(null!));
    }

    [Theory]
    [InlineData("before")]
    [InlineData("product")]
    [InlineData("save")]
    public async Task ExecuteAsync_PropagatesCancellation(string stage)
    {
        var scenario = new Scenario();
        using var source = new CancellationTokenSource();
        var expected = new OperationCanceledException(source.Token);
        if (stage == "before") source.Cancel();
        if (stage == "product") scenario.Products.GetException = expected;
        if (stage == "save") scenario.Products.SaveException = expected;

        var exception = await Assert.ThrowsAsync<OperationCanceledException>(() =>
            scenario.Handler.ExecuteAsync(scenario.Command, source.Token));

        Assert.Equal(source.Token, exception.CancellationToken);
        if (stage != "before") Assert.Same(expected, exception);
        Assert.Equal(stage == "before" ? 0 : 1, scenario.Products.GetCalls);
        Assert.Equal(stage == "save" ? 1 : 0, scenario.Products.SaveCalls);
        if (stage != "before") Assert.Equal(source.Token, scenario.Products.GetToken);
        if (stage == "save")
        {
            Assert.Equal(source.Token, scenario.Products.SaveToken);
            Assert.Same(scenario.Product, scenario.Products.SavedProduct);
            Assert.Empty(scenario.Variant.AttributeValues);
        }
        else
        {
            Assert.Same(scenario.Existing, Assert.Single(scenario.Variant.AttributeValues));
        }
    }

    private sealed class Scenario
    {
        public Scenario()
        {
            // A stored value does not require a current ProductType or AttributeDefinition.
            Product = Product.Create("Product", ProductTypeId.New(), "Standard");
            Variant = Assert.Single(Product.Variants);
            Existing = TextAttributeValue.Create(AttributeDefinitionId.New(), "Existing");
            Product.SetVariantAttributeValue(Variant.Id, Existing);
            Products = new ProductRepositoryFake(Product);
            Handler = new UseCase(Products);
        }

        public Product Product { get; }
        public ProductVariant Variant { get; }
        public TextAttributeValue Existing { get; }
        public ProductRepositoryFake Products { get; }
        public UseCase Handler { get; }
        public RemoveVariantAttributeValueCommand Command =>
            new(Product.Id, Variant.Id, Existing.AttributeDefinitionId);
    }

    private sealed class ProductRepositoryFake(Product product) : IProductRepository
    {
        public Product? Product { get; set; } = product;
        public Exception? GetException { get; set; }
        public Exception? SaveException { get; set; }
        public int GetCalls { get; private set; }
        public int SaveCalls { get; private set; }
        public ProductId RequestedId { get; private set; }
        public CancellationToken GetToken { get; private set; }
        public CancellationToken SaveToken { get; private set; }
        public Product? SavedProduct { get; private set; }

        public Task<Product?> GetByIdAsync(ProductId id, CancellationToken cancellationToken)
        {
            GetCalls++;
            RequestedId = id;
            GetToken = cancellationToken;
            if (GetException is not null) return Task.FromException<Product?>(GetException);
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(Product);
        }

        public Task SaveAsync(Product product, CancellationToken cancellationToken)
        {
            SaveCalls++;
            SaveToken = cancellationToken;
            SavedProduct = product;
            if (SaveException is not null) return Task.FromException(SaveException);
            cancellationToken.ThrowIfCancellationRequested();
            return Task.CompletedTask;
        }
    }
}
