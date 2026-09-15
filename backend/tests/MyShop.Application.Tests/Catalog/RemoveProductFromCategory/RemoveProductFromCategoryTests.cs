using MyShop.Application.Catalog.Abstractions;
using MyShop.Application.Catalog.RemoveProductFromCategory;
using MyShop.Domain.Catalog;
using UseCase = MyShop.Application.Catalog.RemoveProductFromCategory.RemoveProductFromCategory;

namespace MyShop.Application.Tests.Catalog.RemoveProductFromCategory;

public sealed class RemoveProductFromCategoryTests
{
    [Fact]
    public async Task ExecuteAsync_WhenSaveConflicts_PropagatesSameConcurrencyException()
    {
        var scenario = new Scenario();
        var expected = new ProductConcurrencyException(scenario.Product.Id);
        scenario.Products.SaveException = expected;

        var exception = await Assert.ThrowsAsync<ProductConcurrencyException>(() =>
            scenario.Handler.ExecuteAsync(scenario.Command, CancellationToken.None));

        Assert.Same(expected, exception);
        Assert.Equal(1, scenario.Products.SaveCalls);
        Assert.Same(scenario.Products.ReadToken, scenario.Products.SavedExpectedToken);
    }

    [Fact]
    public async Task ExecuteAsync_RemovesCategoryAndSavesSameProductOnce()
    {
        var scenario = new Scenario();

        var result = await scenario.Handler.ExecuteAsync(scenario.Command, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Empty(scenario.Product.CategoryIds);
        Assert.Equal(1, scenario.Products.SaveCalls);
        Assert.Same(scenario.Product, scenario.Products.SavedProduct);
        Assert.Same(scenario.Products.ReadToken, scenario.Products.SavedExpectedToken);
    }

    [Fact]
    public async Task ExecuteAsync_PreservesOtherCategories()
    {
        var scenario = new Scenario();
        var other = CategoryId.New();
        scenario.Product.AssignToCategory(other);

        var result = await scenario.Handler.ExecuteAsync(scenario.Command, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal([other], scenario.Product.CategoryIds);
    }

    [Fact]
    public async Task ExecuteAsync_RemovingOneOfMultipleCategoriesPreservesTheRest()
    {
        var scenario = new Scenario();
        var first = CategoryId.New();
        var second = CategoryId.New();
        scenario.Product.AssignToCategory(first);
        scenario.Product.AssignToCategory(second);
        var command = scenario.Command with { CategoryId = first };

        var result = await scenario.Handler.ExecuteAsync(command, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal([scenario.CategoryId, second], scenario.Product.CategoryIds);
    }

    [Fact]
    public async Task ExecuteAsync_WhenProductIsMissing_ReturnsFailureWithoutSaving()
    {
        var scenario = new Scenario();
        scenario.Products.Product = null;

        var result = await scenario.Handler.ExecuteAsync(scenario.Command, CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(RemoveProductFromCategoryFailure.ProductNotFound, result.Failure);
        Assert.Equal(0, scenario.Products.SaveCalls);
    }

    [Fact]
    public async Task ExecuteAsync_WhenAssignmentIsMissing_SucceedsIdempotentlyWithoutSaving()
    {
        var scenario = new Scenario();
        scenario.Product.RemoveFromCategory(scenario.CategoryId);

        var result = await scenario.Handler.ExecuteAsync(scenario.Command, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Empty(scenario.Product.CategoryIds);
        Assert.Equal(0, scenario.Products.SaveCalls);
    }

    [Fact]
    public async Task ExecuteAsync_WhenRepeated_SucceedsTwiceWithOneSaveOverall()
    {
        var scenario = new Scenario();

        var first = await scenario.Handler.ExecuteAsync(scenario.Command, CancellationToken.None);
        var second = await scenario.Handler.ExecuteAsync(scenario.Command, CancellationToken.None);

        Assert.True(first.IsSuccess);
        Assert.True(second.IsSuccess);
        Assert.Equal(1, scenario.Products.SaveCalls);
    }

    [Fact]
    public async Task ExecuteAsync_RemovesStaleCategoryIdWithoutCategoryRepository()
    {
        var scenario = new Scenario();

        var result = await scenario.Handler.ExecuteAsync(scenario.Command, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Empty(scenario.Product.CategoryIds);
    }

    [Fact]
    public void Constructor_RequiresOnlyProductRepository()
    {
        var constructor = typeof(UseCase).GetConstructors().Single();

        var parameter = Assert.Single(constructor.GetParameters());

        Assert.Equal(typeof(IProductRepository), parameter.ParameterType);
    }

    [Fact]
    public async Task ExecuteAsync_WithDefaultProductId_ThrowsBeforeRepositoryAccess()
    {
        var scenario = new Scenario();

        await Assert.ThrowsAsync<ArgumentException>(() => scenario.Handler.ExecuteAsync(
            scenario.Command with { ProductId = default }, CancellationToken.None));

        Assert.Equal(0, scenario.Products.GetCalls);
        Assert.Equal(0, scenario.Products.SaveCalls);
    }

    [Fact]
    public async Task ExecuteAsync_WithDefaultCategoryId_ThrowsBeforeRepositoryAccess()
    {
        var scenario = new Scenario();

        await Assert.ThrowsAsync<ArgumentException>(() => scenario.Handler.ExecuteAsync(
            scenario.Command with { CategoryId = default }, CancellationToken.None));

        Assert.Equal(0, scenario.Products.GetCalls);
        Assert.Equal(0, scenario.Products.SaveCalls);
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

    [Fact]
    public async Task ExecuteAsync_WhenCancelledBeforeExecution_PropagatesWithoutRepositoryAccess()
    {
        var scenario = new Scenario();
        using var source = new CancellationTokenSource();
        source.Cancel();

        await Assert.ThrowsAsync<OperationCanceledException>(() =>
            scenario.Handler.ExecuteAsync(scenario.Command, source.Token));

        Assert.Equal(0, scenario.Products.GetCalls);
        Assert.Equal(0, scenario.Products.SaveCalls);
    }

    [Fact]
    public async Task ExecuteAsync_PropagatesProductRepositoryCancellation()
    {
        var scenario = new Scenario();
        using var source = new CancellationTokenSource();
        var expected = new OperationCanceledException(source.Token);
        scenario.Products.GetException = expected;

        var exception = await Assert.ThrowsAsync<OperationCanceledException>(() =>
            scenario.Handler.ExecuteAsync(scenario.Command, source.Token));

        Assert.Same(expected, exception);
        Assert.Equal(0, scenario.Products.SaveCalls);
    }

    [Fact]
    public async Task ExecuteAsync_PropagatesSaveCancellation()
    {
        var scenario = new Scenario();
        using var source = new CancellationTokenSource();
        var expected = new OperationCanceledException(source.Token);
        scenario.Products.SaveException = expected;

        var exception = await Assert.ThrowsAsync<OperationCanceledException>(() =>
            scenario.Handler.ExecuteAsync(scenario.Command, source.Token));

        Assert.Same(expected, exception);
        Assert.Equal(1, scenario.Products.SaveCalls);
        Assert.Empty(scenario.Product.CategoryIds);
    }

    [Fact]
    public async Task ExecuteAsync_ForwardsProductIdAndCancellationTokenToLookup()
    {
        var scenario = new Scenario();
        using var source = new CancellationTokenSource();

        await scenario.Handler.ExecuteAsync(scenario.Command, source.Token);

        Assert.Equal(scenario.Product.Id, scenario.Products.RequestedId);
        Assert.Equal(source.Token, scenario.Products.GetToken);
    }

    [Fact]
    public async Task ExecuteAsync_ForwardsCancellationTokenToSave()
    {
        var scenario = new Scenario();
        using var source = new CancellationTokenSource();

        await scenario.Handler.ExecuteAsync(scenario.Command, source.Token);

        Assert.Equal(source.Token, scenario.Products.SaveToken);
    }

    private sealed class Scenario
    {
        public Scenario()
        {
            Product = Product.Create("Product", ProductTypeId.New(), "Standard");
            CategoryId = CategoryId.New();
            Product.AssignToCategory(CategoryId);
            Products = new ProductRepositoryFake(Product);
            Handler = new UseCase(Products);
        }

        public Product Product { get; }
        public CategoryId CategoryId { get; }
        public ProductRepositoryFake Products { get; }
        public UseCase Handler { get; }
        public RemoveProductFromCategoryCommand Command => new(Product.Id, CategoryId);
    }

    private sealed class ProductRepositoryFake(Product? product) : IProductRepository
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
        public ProductConcurrencyToken? ReadToken { get; private set; }
        public ProductConcurrencyToken? SavedExpectedToken { get; private set; }
        private ProductConcurrencyToken? _currentToken;
        private int _revision = 1;

        public Task<ProductSnapshot?> GetByIdAsync(ProductId id, CancellationToken cancellationToken)
        {
            GetCalls++;
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
            SaveCalls++;
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
}