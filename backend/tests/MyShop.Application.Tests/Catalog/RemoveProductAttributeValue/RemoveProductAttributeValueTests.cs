using MyShop.Application.Catalog.Abstractions;
using MyShop.Application.Catalog.RemoveProductAttributeValue;
using MyShop.Domain.Catalog;
using UseCase = MyShop.Application.Catalog.RemoveProductAttributeValue.RemoveProductAttributeValue;

namespace MyShop.Application.Tests.Catalog.RemoveProductAttributeValue;

public sealed class RemoveProductAttributeValueTests
{
    [Fact]
    public async Task ExecuteAsync_RemovesStoredValueWithoutDefinitionMetadataAndSavesSameProductOnce()
    {
        var scenario = new Scenario();
        using var source = new CancellationTokenSource();

        var result = await scenario.Handler.ExecuteAsync(scenario.Command, source.Token);

        Assert.True(result.IsSuccess);
        Assert.Null(result.Failure);
        Assert.Empty(scenario.Product.AttributeValues);
        Assert.Equal(1, scenario.Products.GetCalls);
        Assert.Equal(scenario.Product.Id, scenario.Products.RequestedId);
        Assert.Equal(source.Token, scenario.Products.GetToken);
        Assert.Equal(1, scenario.Products.SaveCalls);
        Assert.Same(scenario.Product, scenario.Products.SavedProduct);
        Assert.Equal(source.Token, scenario.Products.SaveToken);
    }

    [Fact]
    public async Task ExecuteAsync_PreservesOtherProductValuesAndValuesOnMultipleVariants()
    {
        var scenario = new Scenario();
        var remaining = TextAttributeValue.Create(AttributeDefinitionId.New(), "Remaining");
        scenario.Product.SetAttributeValue(remaining);
        var first = Assert.Single(scenario.Product.Variants);
        var second = scenario.Product.AddVariant("Other");
        scenario.Product.SetVariantAttributeValue(first.Id, scenario.Existing);
        scenario.Product.SetVariantAttributeValue(second.Id, scenario.Existing);
        scenario.Product.SetVariantAttributeValue(second.Id, remaining);

        var result = await scenario.Handler.ExecuteAsync(scenario.Command, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Same(remaining, Assert.Single(scenario.Product.AttributeValues));
        Assert.Same(scenario.Existing, Assert.Single(first.AttributeValues));
        Assert.Equal(2, second.AttributeValues.Count);
        Assert.Contains(second.AttributeValues, value => ReferenceEquals(value, scenario.Existing));
        Assert.Contains(second.AttributeValues, value => ReferenceEquals(value, remaining));
        Assert.Equal(1, scenario.Products.SaveCalls);
    }

    [Fact]
    public async Task ExecuteAsync_WhenProductIsMissing_ReturnsFailureAndNeverSaves()
    {
        var scenario = new Scenario();
        scenario.Products.Product = null;

        var result = await scenario.Handler.ExecuteAsync(scenario.Command, CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(RemoveProductAttributeValueFailure.ProductNotFound, result.Failure);
        Assert.Equal(1, scenario.Products.GetCalls);
        Assert.Equal(0, scenario.Products.SaveCalls);
        Assert.Same(scenario.Existing, Assert.Single(scenario.Product.AttributeValues));
    }

    [Fact]
    public async Task ExecuteAsync_WhenValueIsMissing_SucceedsWithoutMutatingOrSaving()
    {
        var scenario = new Scenario();
        var missingId = AttributeDefinitionId.New();
        var other = scenario.Product.AddVariant("Other");
        var otherValue = TextAttributeValue.Create(missingId, "Other");
        scenario.Product.SetVariantAttributeValue(other.Id, otherValue);

        var result = await scenario.Handler.ExecuteAsync(
            scenario.Command with { AttributeDefinitionId = missingId }, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Null(result.Failure);
        Assert.Same(scenario.Existing, Assert.Single(scenario.Product.AttributeValues));
        Assert.Same(otherValue, Assert.Single(other.AttributeValues));
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
        Assert.Empty(scenario.Product.AttributeValues);
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
        Assert.Same(scenario.Existing, Assert.Single(scenario.Product.AttributeValues));
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
            Assert.Empty(scenario.Product.AttributeValues);
        }
        else
        {
            Assert.Same(scenario.Existing, Assert.Single(scenario.Product.AttributeValues));
        }
    }

    private sealed class Scenario
    {
        public Scenario()
        {
            // A stored value does not require a current ProductType or AttributeDefinition.
            Product = Product.Create("Product", ProductTypeId.New(), "Standard");
            Existing = TextAttributeValue.Create(AttributeDefinitionId.New(), "Existing");
            Product.SetAttributeValue(Existing);
            Products = new ProductRepositoryFake(Product);
            Handler = new UseCase(Products);
        }

        public Product Product { get; }
        public TextAttributeValue Existing { get; }
        public ProductRepositoryFake Products { get; }
        public UseCase Handler { get; }
        public RemoveProductAttributeValueCommand Command =>
            new(Product.Id, Existing.AttributeDefinitionId);
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
