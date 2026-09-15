using MyShop.Application.Catalog;
using MyShop.Application.Catalog.Abstractions;
using MyShop.Application.Catalog.SetProductAttributeValue;
using MyShop.Domain.Catalog;
using SetProductAttributeValueUseCase = MyShop.Application.Catalog.SetProductAttributeValue.SetProductAttributeValue;

namespace MyShop.Application.Tests.Catalog.SetProductAttributeValue;

public sealed class SetProductAttributeValueTests
{
    [Fact]
    public async Task ExecuteAsync_WhenSaveConflicts_PropagatesSameConcurrencyException()
    {
        var scenario = CreateScenario(AttributeDataType.Text);
        var expected = new ProductConcurrencyException(scenario.Product.Id);
        scenario.Products.SaveException = expected;

        var exception = await Assert.ThrowsAsync<ProductConcurrencyException>(() =>
            scenario.UseCase.ExecuteAsync(CreateCommand(scenario, new TextAttributeValueInput("New")), CancellationToken.None));

        Assert.Same(expected, exception);
        Assert.Equal(1, scenario.Products.SaveCallCount);
        Assert.Same(scenario.Products.ReadToken, scenario.Products.SavedExpectedToken);
    }

    public static IEnumerable<object[]> ValidAssignments()
    {
        yield return [new TextAttributeValueInput("A title"), typeof(TextAttributeValue)];
        yield return [new IntegerAttributeValueInput(42), typeof(IntegerAttributeValue)];
        yield return [new DecimalAttributeValueInput(19.95m), typeof(DecimalAttributeValue)];
        yield return [new BooleanAttributeValueInput(true), typeof(BooleanAttributeValue)];
        yield return [new DateAttributeValueInput(new DateOnly(2026, 8, 29)), typeof(DateAttributeValue)];
        yield return [new ChoiceAttributeValueInput("blue"), typeof(ChoiceAttributeValue)];
        yield return [new MultiChoiceAttributeValueInput(["blue", "red"]), typeof(MultiChoiceAttributeValue)];
    }

    [Fact]
    public void MultiChoiceInput_DefensivelyCopiesSourceValues()
    {
        // Arrange
        var sourceValues = new List<string> { "blue" };

        // Act
        var input = new MultiChoiceAttributeValueInput(sourceValues);
        sourceValues.Add("red");

        // Assert
        Assert.Equal(["blue"], input.Values);
        Assert.Throws<NotSupportedException>(() => ((IList<string>)input.Values)[0] = "green");
    }

    [Theory]
    [MemberData(nameof(ValidAssignments))]
    public async Task ExecuteAsync_WithValidInput_SetsTypedValueAndSavesOnce(
        CatalogAttributeValueInput input,
        Type expectedValueType)
    {
        // Arrange
        var scenario = CreateScenario(input.DataType);
        var command = CreateCommand(scenario, input);

        // Act
        var result = await scenario.UseCase.ExecuteAsync(command, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Null(result.Failure);
        Assert.IsType(expectedValueType, Assert.Single(scenario.Product.AttributeValues));
        Assert.Equal(1, scenario.Products.SaveCallCount);
        Assert.Same(scenario.Product, scenario.Products.SavedProduct);
        Assert.Same(scenario.Products.ReadToken, scenario.Products.SavedExpectedToken);
    }

    [Fact]
    public async Task ExecuteAsync_WithExistingValue_ReplacesIt()
    {
        // Arrange
        var scenario = CreateScenario(AttributeDataType.Text);
        scenario.Product.SetAttributeValue(TextAttributeValue.Create(scenario.AttributeDefinitionId, "Old title"));
        var command = CreateCommand(
            scenario,
            new TextAttributeValueInput("New title"));

        // Act
        var result = await scenario.UseCase.ExecuteAsync(command, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        var value = Assert.IsType<TextAttributeValue>(Assert.Single(scenario.Product.AttributeValues));
        Assert.Equal("New title", value.Value);
        Assert.Equal(1, scenario.Products.SaveCallCount);
    }

    [Fact]
    public async Task ExecuteAsync_WhenProductIsMissing_ReturnsFailureWithoutLoadingProductTypeOrSaving()
    {
        // Arrange
        var products = new ProductRepositoryFake(null);
        var productTypes = new ProductTypeRepositoryFake(null);
        var useCase = new SetProductAttributeValueUseCase(products, productTypes);
        var command = new SetProductAttributeValueCommand(
            ProductId.New(),
            AttributeDefinitionId.New(),
            new TextAttributeValueInput("Title"));

        // Act
        var result = await useCase.ExecuteAsync(command, CancellationToken.None);

        // Assert
        AssertFailure(result, SetProductAttributeValueFailure.ProductNotFound);
        Assert.Equal(1, products.GetByIdCallCount);
        Assert.Equal(0, productTypes.GetByIdCallCount);
        Assert.Equal(0, products.SaveCallCount);
    }

    [Fact]
    public async Task ExecuteAsync_WhenProductTypeIsMissing_ReturnsFailureWithoutMutatingOrSaving()
    {
        // Arrange
        var product = Product.Create("Product", ProductTypeId.New(), "Standard");
        var existing = TextAttributeValue.Create(AttributeDefinitionId.New(), "Existing");
        product.SetAttributeValue(existing);
        var products = new ProductRepositoryFake(product);
        var useCase = new SetProductAttributeValueUseCase(products, new ProductTypeRepositoryFake(null));
        var command = new SetProductAttributeValueCommand(
            product.Id,
            AttributeDefinitionId.New(),
            new TextAttributeValueInput("New"));

        // Act
        var result = await useCase.ExecuteAsync(command, CancellationToken.None);

        // Assert
        AssertFailure(result, SetProductAttributeValueFailure.ProductTypeNotFound);
        Assert.Same(existing, Assert.Single(product.AttributeValues));
        Assert.Equal(0, products.SaveCallCount);
    }

    [Fact]
    public async Task ExecuteAsync_WhenAttributeDefinitionIsMissing_ReturnsFailureWithoutMutatingOrSaving()
    {
        // Arrange
        var scenario = CreateScenarioWithoutDefinition();
        var existing = TextAttributeValue.Create(AttributeDefinitionId.New(), "Existing");
        scenario.Product.SetAttributeValue(existing);
        var command = new SetProductAttributeValueCommand(
            scenario.Product.Id,
            AttributeDefinitionId.New(),
            new TextAttributeValueInput("New"));

        // Act
        var result = await scenario.UseCase.ExecuteAsync(command, CancellationToken.None);

        // Assert
        AssertFailure(result, SetProductAttributeValueFailure.AttributeDefinitionNotFound);
        Assert.Same(existing, Assert.Single(scenario.Product.AttributeValues));
        Assert.Equal(0, scenario.Products.SaveCallCount);
    }

    [Fact]
    public async Task ExecuteAsync_WhenDefinitionHasVariantScope_ReturnsFailureWithoutMutatingOrSaving()
    {
        // Arrange
        var scenario = CreateScenario(AttributeDataType.Text, AttributeScope.Variant);
        var existing = TextAttributeValue.Create(AttributeDefinitionId.New(), "Existing");
        scenario.Product.SetAttributeValue(existing);
        var command = CreateCommand(scenario, new TextAttributeValueInput("New"));

        // Act
        var result = await scenario.UseCase.ExecuteAsync(command, CancellationToken.None);

        // Assert
        AssertFailure(result, SetProductAttributeValueFailure.WrongAttributeScope);
        Assert.Same(existing, Assert.Single(scenario.Product.AttributeValues));
        Assert.Equal(0, scenario.Products.SaveCallCount);
    }

    [Fact]
    public async Task ExecuteAsync_WhenDefinitionDataTypeDoesNotMatch_ReturnsFailureWithoutMutatingOrSaving()
    {
        // Arrange
        var scenario = CreateScenario(AttributeDataType.Integer);
        var existing = TextAttributeValue.Create(AttributeDefinitionId.New(), "Existing");
        scenario.Product.SetAttributeValue(existing);
        var command = CreateCommand(scenario, new TextAttributeValueInput("New"));

        // Act
        var result = await scenario.UseCase.ExecuteAsync(command, CancellationToken.None);

        // Assert
        AssertFailure(result, SetProductAttributeValueFailure.WrongAttributeDataType);
        Assert.Same(existing, Assert.Single(scenario.Product.AttributeValues));
        Assert.Equal(0, scenario.Products.SaveCallCount);
    }

    [Fact]
    public async Task ExecuteAsync_WhenCancellationIsRequestedBeforeExecution_PropagatesWithoutRepositoryCalls()
    {
        // Arrange
        var scenario = CreateScenario(AttributeDataType.Text);
        using var cancellationSource = new CancellationTokenSource();
        cancellationSource.Cancel();
        var command = CreateCommand(scenario, new TextAttributeValueInput("Title"));

        // Act
        var exception = await Record.ExceptionAsync(() =>
            scenario.UseCase.ExecuteAsync(command, cancellationSource.Token));

        // Assert
        Assert.IsType<OperationCanceledException>(exception);
        Assert.Equal(0, scenario.Products.GetByIdCallCount);
        Assert.Equal(0, scenario.ProductTypes.GetByIdCallCount);
        Assert.Equal(0, scenario.Products.SaveCallCount);
    }

    [Fact]
    public async Task ExecuteAsync_WhenRepositoryCancels_PropagatesWithoutSaving()
    {
        // Arrange
        var scenario = CreateScenario(AttributeDataType.Text);
        using var cancellationSource = new CancellationTokenSource();
        scenario.Products.GetException = new OperationCanceledException(cancellationSource.Token);
        var command = CreateCommand(scenario, new TextAttributeValueInput("Title"));

        // Act
        var exception = await Record.ExceptionAsync(() =>
            scenario.UseCase.ExecuteAsync(command, cancellationSource.Token));

        // Assert
        Assert.IsType<OperationCanceledException>(exception);
        Assert.Equal(1, scenario.Products.GetByIdCallCount);
        Assert.Equal(0, scenario.ProductTypes.GetByIdCallCount);
        Assert.Equal(0, scenario.Products.SaveCallCount);
        Assert.Empty(scenario.Product.AttributeValues);
    }

    private static void AssertFailure(
        SetProductAttributeValueResult result,
        SetProductAttributeValueFailure failure)
    {
        Assert.False(result.IsSuccess);
        Assert.Equal(failure, result.Failure);
    }

    private static SetProductAttributeValueCommand CreateCommand(
        Scenario scenario,
        CatalogAttributeValueInput input) =>
        new(scenario.Product.Id, scenario.AttributeDefinitionId, input);

    private static Scenario CreateScenario(
        AttributeDataType dataType,
        AttributeScope scope = AttributeScope.Product)
    {
        var productType = ProductType.Create("Product type");
        var attributeDefinitionId = AttributeDefinitionId.New();
        productType.AddAttribute(
            attributeDefinitionId,
            AttributeCode.Create("attribute"),
            "Attribute",
            dataType,
            false,
            false,
            scope);
        var product = Product.Create("Product", productType.Id, "Standard");
        var products = new ProductRepositoryFake(product);
        var productTypes = new ProductTypeRepositoryFake(productType);

        return new Scenario(
            product,
            attributeDefinitionId,
            products,
            productTypes,
            new SetProductAttributeValueUseCase(products, productTypes));
    }

    private static Scenario CreateScenarioWithoutDefinition()
    {
        var productType = ProductType.Create("Product type");
        var product = Product.Create("Product", productType.Id, "Standard");
        var products = new ProductRepositoryFake(product);
        var productTypes = new ProductTypeRepositoryFake(productType);

        return new Scenario(
            product,
            AttributeDefinitionId.New(),
            products,
            productTypes,
            new SetProductAttributeValueUseCase(products, productTypes));
    }

    private sealed record Scenario(
        Product Product,
        AttributeDefinitionId AttributeDefinitionId,
        ProductRepositoryFake Products,
        ProductTypeRepositoryFake ProductTypes,
        SetProductAttributeValueUseCase UseCase);

    private sealed class ProductRepositoryFake(Product? product) : IProductRepository
    {
        public Product? Product { get; set; } = product;
        public Exception? GetException { get; set; }
        public Exception? SaveException { get; set; }
        public int GetByIdCallCount { get; private set; }
        public int SaveCallCount { get; private set; }
        public ProductId RequestedId { get; private set; }
        public CancellationToken GetToken { get; private set; }
        public CancellationToken SaveToken { get; private set; }
        public Product? SavedProduct { get; private set; }
        public ProductConcurrencyToken? ReadToken { get; private set; }
        public ProductConcurrencyToken? SavedExpectedToken { get; private set; }
        private ProductConcurrencyToken? _currentToken;
        private int _revision = 1;

        public Task<ProductSnapshot?> GetByIdAsync(ProductId id, CancellationToken cancellationToken)
        {
            GetByIdCallCount++;
            RequestedId = id;
            GetToken = cancellationToken;
            if (GetException is not null) return Task.FromException<ProductSnapshot?>(GetException);
            cancellationToken.ThrowIfCancellationRequested();
            if (Product is null) return Task.FromResult<ProductSnapshot?>(null);
            if (_currentToken is null || _currentToken.ProductId != Product.Id)
                _currentToken = CreateToken(Product.Id);
            ReadToken = _currentToken;
            return Task.FromResult<ProductSnapshot?>(new ProductSnapshot(Product, ReadToken));
        }

        public Task<ProductConcurrencyToken> AddAsync(Product product, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<ProductConcurrencyToken> SaveAsync(
            Product product,
            ProductConcurrencyToken expectedToken,
            CancellationToken cancellationToken)
        {
            SaveCallCount++;
            SavedProduct = product;
            SavedExpectedToken = expectedToken;
            SaveToken = cancellationToken;
            if (SaveException is not null) return Task.FromException<ProductConcurrencyToken>(SaveException);
            cancellationToken.ThrowIfCancellationRequested();
            _currentToken = CreateToken(product.Id);
            return Task.FromResult(_currentToken);
        }

        private ProductConcurrencyToken CreateToken(ProductId productId) =>
            ProductConcurrencyToken.Create(productId, new Guid(_revision++, 0, 0, new byte[8]));
    }

    private sealed class ProductTypeRepositoryFake(ProductType? productType) : IProductTypeRepository
    {
        public int GetByIdCallCount { get; private set; }

        public Task<ProductType?> GetByIdAsync(ProductTypeId id, CancellationToken cancellationToken)
        {
            GetByIdCallCount++;
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(productType);
        }
    }
}
