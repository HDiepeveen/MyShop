using MyShop.Application.Catalog;
using MyShop.Application.Catalog.Abstractions;
using MyShop.Application.Catalog.SetVariantAttributeValue;
using MyShop.Domain.Catalog;
using UseCase = MyShop.Application.Catalog.SetVariantAttributeValue.SetVariantAttributeValue;

namespace MyShop.Application.Tests.Catalog.SetVariantAttributeValue;

public sealed class SetVariantAttributeValueTests
{
    [Theory]
    [InlineData(AttributeDataType.Text)]
    [InlineData(AttributeDataType.Integer)]
    [InlineData(AttributeDataType.MultiChoice)]
    public async Task ExecuteAsync_AssignsTypedValueAndSavesOnce(AttributeDataType dataType)
    {
        var scenario = new Scenario(dataType);
        CatalogAttributeValueInput input = dataType switch
        {
            AttributeDataType.Text => new TextAttributeValueInput("Blue"),
            AttributeDataType.Integer => new IntegerAttributeValueInput(42),
            _ => new MultiChoiceAttributeValueInput(["blue", "red"])
        };
        using var source = new CancellationTokenSource();

        var result = await scenario.Handler.ExecuteAsync(scenario.Command(input), source.Token);

        Assert.True(result.IsSuccess);
        Assert.Null(result.Failure);
        var value = Assert.Single(scenario.Variant.AttributeValues);
        Assert.Equal(scenario.DefinitionId, value.AttributeDefinitionId);
        switch (dataType)
        {
            case AttributeDataType.Text:
                Assert.Equal("Blue", Assert.IsType<TextAttributeValue>(value).Value);
                break;
            case AttributeDataType.Integer:
                Assert.Equal(42, Assert.IsType<IntegerAttributeValue>(value).Value);
                break;
            default:
                Assert.Equal(["blue", "red"],
                    Assert.IsType<MultiChoiceAttributeValue>(value).Values.Select(choice => choice.Value));
                break;
        }
        Assert.Equal(1, scenario.Products.SaveCalls);
        Assert.Same(scenario.Product, scenario.Products.SavedProduct);
        Assert.Equal(scenario.Product.Id, scenario.Products.RequestedId);
        Assert.Equal(scenario.Product.ProductTypeId, scenario.ProductTypes.RequestedId);
        Assert.Equal(source.Token, scenario.Products.GetToken);
        Assert.Equal(source.Token, scenario.ProductTypes.Token);
        Assert.Equal(source.Token, scenario.Products.SaveToken);
    }

    [Fact]
    public async Task ExecuteAsync_ReplacesWithoutDuplicatesAndKeepsOtherVariantAndProductValues()
    {
        var scenario = new Scenario();
        var other = scenario.Product.AddVariant("Other");
        var productValue = TextAttributeValue.Create(scenario.DefinitionId, "Product");
        scenario.Product.SetAttributeValue(productValue);
        scenario.Product.SetVariantAttributeValue(scenario.Variant.Id,
            TextAttributeValue.Create(scenario.DefinitionId, "Old"));

        var otherResult = await scenario.Handler.ExecuteAsync(
            scenario.Command(new TextAttributeValueInput("Other")) with { ProductVariantId = other.Id },
            CancellationToken.None);
        var result = await scenario.Handler.ExecuteAsync(
            scenario.Command(new TextAttributeValueInput("New")), CancellationToken.None);

        Assert.True(otherResult.IsSuccess);
        Assert.True(result.IsSuccess);
        Assert.Equal("New", Assert.IsType<TextAttributeValue>(Assert.Single(scenario.Variant.AttributeValues)).Value);
        Assert.Equal("Other", Assert.IsType<TextAttributeValue>(Assert.Single(other.AttributeValues)).Value);
        Assert.Same(productValue, Assert.Single(scenario.Product.AttributeValues));
        Assert.Equal(2, scenario.Products.SaveCalls);
    }

    [Theory]
    [InlineData(SetVariantAttributeValueFailure.ProductNotFound)]
    [InlineData(SetVariantAttributeValueFailure.VariantNotFound)]
    [InlineData(SetVariantAttributeValueFailure.ProductTypeNotFound)]
    [InlineData(SetVariantAttributeValueFailure.AttributeDefinitionNotFound)]
    [InlineData(SetVariantAttributeValueFailure.WrongAttributeScope)]
    [InlineData(SetVariantAttributeValueFailure.WrongAttributeDataType)]
    public async Task ExecuteAsync_ValidationFailurePreservesAggregateAndNeverSaves(
        SetVariantAttributeValueFailure failure)
    {
        var scenario = new Scenario(
            failure == SetVariantAttributeValueFailure.WrongAttributeDataType
                ? AttributeDataType.Integer : AttributeDataType.Text,
            failure == SetVariantAttributeValueFailure.WrongAttributeScope
                ? AttributeScope.Product : AttributeScope.Variant);
        var existing = TextAttributeValue.Create(scenario.DefinitionId, "Existing");
        var other = scenario.Product.AddVariant("Other");
        scenario.Product.SetAttributeValue(existing);
        scenario.Product.SetVariantAttributeValue(scenario.Variant.Id, existing);
        scenario.Product.SetVariantAttributeValue(other.Id, existing);
        var variants = scenario.Product.Variants.ToArray();
        var command = scenario.Command(new TextAttributeValueInput("New"));
        switch (failure)
        {
            case SetVariantAttributeValueFailure.ProductNotFound:
                scenario.Products.Product = null;
                break;
            case SetVariantAttributeValueFailure.VariantNotFound:
                var foreignProduct = Product.Create("Foreign", scenario.Product.ProductTypeId, "Foreign");
                command = command with { ProductVariantId = Assert.Single(foreignProduct.Variants).Id };
                break;
            case SetVariantAttributeValueFailure.ProductTypeNotFound:
                scenario.ProductTypes.ProductType = null;
                break;
            case SetVariantAttributeValueFailure.AttributeDefinitionNotFound:
                command = command with { AttributeDefinitionId = AttributeDefinitionId.New() };
                break;
        }

        var result = await scenario.Handler.ExecuteAsync(command, CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(failure, result.Failure);
        Assert.Equal(variants, scenario.Product.Variants);
        Assert.Same(existing, Assert.Single(scenario.Product.AttributeValues));
        foreach (var variant in scenario.Product.Variants)
            Assert.Same(existing, Assert.Single(variant.AttributeValues));
        Assert.Equal(1, scenario.Products.GetCalls);
        Assert.Equal(failure is SetVariantAttributeValueFailure.ProductNotFound
            or SetVariantAttributeValueFailure.VariantNotFound ? 0 : 1, scenario.ProductTypes.GetCalls);
        Assert.Equal(0, scenario.Products.SaveCalls);
    }

    [Fact]
    public async Task ExecuteAsync_ChecksScopeBeforeDataType()
    {
        var scenario = new Scenario(AttributeDataType.Integer, AttributeScope.Product);
        var result = await scenario.Handler.ExecuteAsync(
            scenario.Command(new TextAttributeValueInput("New")), CancellationToken.None);
        Assert.Equal(SetVariantAttributeValueFailure.WrongAttributeScope, result.Failure);
        Assert.Empty(scenario.Variant.AttributeValues);
        Assert.Equal(0, scenario.Products.SaveCalls);
    }

    [Theory]
    [InlineData("before")]
    [InlineData("product")]
    [InlineData("productType")]
    [InlineData("save")]
    public async Task ExecuteAsync_PropagatesCancellation(string stage)
    {
        var scenario = new Scenario();
        using var source = new CancellationTokenSource();
        var expected = new OperationCanceledException(source.Token);
        if (stage == "before") source.Cancel();
        if (stage == "product") scenario.Products.GetException = expected;
        if (stage == "productType") scenario.ProductTypes.GetException = expected;
        if (stage == "save") scenario.Products.SaveException = expected;

        var exception = await Assert.ThrowsAsync<OperationCanceledException>(() =>
            scenario.Handler.ExecuteAsync(scenario.Command(new TextAttributeValueInput("New")), source.Token));

        Assert.Equal(source.Token, exception.CancellationToken);
        if (stage != "before") Assert.Same(expected, exception);
        Assert.Equal(stage == "before" ? 0 : 1, scenario.Products.GetCalls);
        Assert.Equal(stage is "before" or "product" ? 0 : 1, scenario.ProductTypes.GetCalls);
        Assert.Equal(stage == "save" ? 1 : 0, scenario.Products.SaveCalls);
        if (stage != "before") Assert.Equal(source.Token, scenario.Products.GetToken);
        if (stage is "productType" or "save") Assert.Equal(source.Token, scenario.ProductTypes.Token);
        if (stage == "save") Assert.Equal(source.Token, scenario.Products.SaveToken);
        else Assert.Empty(scenario.Variant.AttributeValues);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task ExecuteAsync_GuardsNullCommandAndValue(bool nullCommand)
    {
        var scenario = new Scenario();
        await Assert.ThrowsAsync<ArgumentNullException>(() => scenario.Handler.ExecuteAsync(
            nullCommand ? null! : scenario.Command(null!), CancellationToken.None));
        Assert.Equal(0, scenario.Products.GetCalls);
        Assert.Equal(0, scenario.ProductTypes.GetCalls);
        Assert.Equal(0, scenario.Products.SaveCalls);
    }

    [Fact]
    public void Constructor_GuardsRepositories()
    {
        var scenario = new Scenario();
        Assert.Throws<ArgumentNullException>(() => new UseCase(null!, scenario.ProductTypes));
        Assert.Throws<ArgumentNullException>(() => new UseCase(scenario.Products, null!));
    }

    private sealed class Scenario
    {
        public Scenario(AttributeDataType dataType = AttributeDataType.Text,
            AttributeScope scope = AttributeScope.Variant)
        {
            var productType = ProductType.Create("Type");
            productType.AddAttribute(DefinitionId, AttributeCode.Create("attribute"), "Attribute",
                dataType, false, false, scope);
            Product = Product.Create("Product", productType.Id, "Standard");
            Variant = Assert.Single(Product.Variants);
            Products = new ProductRepositoryFake(Product);
            ProductTypes = new ProductTypeRepositoryFake(productType);
            Handler = new UseCase(Products, ProductTypes);
        }

        public AttributeDefinitionId DefinitionId { get; } = AttributeDefinitionId.New();
        public Product Product { get; }
        public ProductVariant Variant { get; }
        public ProductRepositoryFake Products { get; }
        public ProductTypeRepositoryFake ProductTypes { get; }
        public UseCase Handler { get; }
        public SetVariantAttributeValueCommand Command(CatalogAttributeValueInput input) =>
            new(Product.Id, Variant.Id, DefinitionId, input);
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

    private sealed class ProductTypeRepositoryFake(ProductType productType) : IProductTypeRepository
    {
        public ProductType? ProductType { get; set; } = productType;
        public Exception? GetException { get; set; }
        public int GetCalls { get; private set; }
        public ProductTypeId RequestedId { get; private set; }
        public CancellationToken Token { get; private set; }

        public Task<ProductType?> GetByIdAsync(ProductTypeId id, CancellationToken cancellationToken)
        {
            GetCalls++;
            RequestedId = id;
            Token = cancellationToken;
            if (GetException is not null) return Task.FromException<ProductType?>(GetException);
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(ProductType);
        }
    }
}
