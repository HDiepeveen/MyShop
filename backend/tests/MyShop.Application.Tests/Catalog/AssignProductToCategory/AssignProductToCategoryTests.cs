using MyShop.Application.Catalog.Abstractions;
using MyShop.Application.Catalog.AssignProductToCategory;
using MyShop.Domain.Catalog;
using UseCase = MyShop.Application.Catalog.AssignProductToCategory.AssignProductToCategory;

namespace MyShop.Application.Tests.Catalog.AssignProductToCategory;

public sealed class AssignProductToCategoryTests
{
    [Fact]
    public async Task ExecuteAsync_AssignsCategoryAndSavesSameProductOnce()
    {
        var scenario = new Scenario();

        var result = await scenario.Handler.ExecuteAsync(scenario.Command, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal([scenario.Category.Id], scenario.Product.CategoryIds);
        Assert.Equal(1, scenario.Products.SaveCalls);
        Assert.Same(scenario.Product, scenario.Products.SavedProduct);
    }

    [Fact]
    public async Task ExecuteAsync_PreservesExistingAssignments()
    {
        var scenario = new Scenario();
        var existing = Category.CreateRoot("Existing");
        scenario.Product.AssignToCategory(existing.Id);

        var result = await scenario.Handler.ExecuteAsync(scenario.Command, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal([existing.Id, scenario.Category.Id], scenario.Product.CategoryIds);
    }

    [Fact]
    public async Task ExecuteAsync_WhenProductIsMissing_ReturnsFailureWithoutCategoryLookupOrSave()
    {
        var scenario = new Scenario();
        scenario.Products.Product = null;

        var result = await scenario.Handler.ExecuteAsync(scenario.Command, CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(AssignProductToCategoryFailure.ProductNotFound, result.Failure);
        Assert.Equal(0, scenario.Categories.GetCalls);
        Assert.Equal(0, scenario.Products.SaveCalls);
    }

    [Fact]
    public async Task ExecuteAsync_WhenCategoryIsMissing_ReturnsFailureAndPreservesProduct()
    {
        var scenario = new Scenario();
        scenario.Categories.Category = null;
        var existing = CategoryId.New();
        scenario.Product.AssignToCategory(existing);

        var result = await scenario.Handler.ExecuteAsync(scenario.Command, CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(AssignProductToCategoryFailure.CategoryNotFound, result.Failure);
        Assert.Equal([existing], scenario.Product.CategoryIds);
        Assert.Equal(0, scenario.Products.SaveCalls);
    }

    [Fact]
    public async Task ExecuteAsync_WhenAlreadyAssigned_SucceedsWithoutSavingAndStillLooksUpCategory()
    {
        var scenario = new Scenario();
        scenario.Product.AssignToCategory(scenario.Category.Id);

        var result = await scenario.Handler.ExecuteAsync(scenario.Command, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(1, scenario.Categories.GetCalls);
        Assert.Equal(0, scenario.Products.SaveCalls);
        Assert.Equal([scenario.Category.Id], scenario.Product.CategoryIds);
    }

    [Fact]
    public async Task ExecuteAsync_WhenRepeated_SavesOnlyOnce()
    {
        var scenario = new Scenario();

        var first = await scenario.Handler.ExecuteAsync(scenario.Command, CancellationToken.None);
        var second = await scenario.Handler.ExecuteAsync(scenario.Command, CancellationToken.None);

        Assert.True(first.IsSuccess);
        Assert.True(second.IsSuccess);
        Assert.Equal(1, scenario.Products.SaveCalls);
    }

    [Fact]
    public async Task ExecuteAsync_ForwardsIdsAndCancellationToken()
    {
        var scenario = new Scenario();
        using var source = new CancellationTokenSource();

        await scenario.Handler.ExecuteAsync(scenario.Command, source.Token);

        Assert.Equal(scenario.Product.Id, scenario.Products.RequestedId);
        Assert.Equal(scenario.Category.Id, scenario.Categories.RequestedId);
        Assert.Equal(source.Token, scenario.Products.GetToken);
        Assert.Equal(source.Token, scenario.Categories.GetToken);
        Assert.Equal(source.Token, scenario.Products.SaveToken);
    }

    [Fact]
    public async Task ExecuteAsync_WithDefaultProductId_ThrowsBeforeRepositoryAccess()
    {
        var scenario = new Scenario();

        await Assert.ThrowsAsync<ArgumentException>(() => scenario.Handler.ExecuteAsync(
            scenario.Command with { ProductId = default }, CancellationToken.None));

        Assert.Equal(0, scenario.Products.GetCalls);
        Assert.Equal(0, scenario.Categories.GetCalls);
    }

    [Fact]
    public async Task ExecuteAsync_WithDefaultCategoryId_ThrowsBeforeRepositoryAccess()
    {
        var scenario = new Scenario();

        await Assert.ThrowsAsync<ArgumentException>(() => scenario.Handler.ExecuteAsync(
            scenario.Command with { CategoryId = default }, CancellationToken.None));

        Assert.Equal(0, scenario.Products.GetCalls);
        Assert.Equal(0, scenario.Categories.GetCalls);
    }

    [Fact]
    public async Task ExecuteAsync_WithNullCommand_ThrowsBeforeRepositoryAccess()
    {
        var scenario = new Scenario();

        await Assert.ThrowsAsync<ArgumentNullException>(() => scenario.Handler.ExecuteAsync(null!, CancellationToken.None));

        Assert.Equal(0, scenario.Products.GetCalls);
        Assert.Equal(0, scenario.Categories.GetCalls);
    }

    [Fact]
    public void Constructor_WithNullRepositories_Throws()
    {
        var scenario = new Scenario();

        Assert.Throws<ArgumentNullException>(() => new UseCase(null!, scenario.Categories));
        Assert.Throws<ArgumentNullException>(() => new UseCase(scenario.Products, null!));
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
        Assert.Equal(0, scenario.Categories.GetCalls);
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
        Assert.Equal(0, scenario.Categories.GetCalls);
    }

    [Fact]
    public async Task ExecuteAsync_PropagatesCategoryRepositoryCancellation()
    {
        var scenario = new Scenario();
        using var source = new CancellationTokenSource();
        var expected = new OperationCanceledException(source.Token);
        scenario.Categories.GetException = expected;

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
        Assert.Equal([scenario.Category.Id], scenario.Product.CategoryIds);
    }

    private sealed class Scenario
    {
        public Scenario()
        {
            Product = Product.Create("Product", ProductTypeId.New(), "Standard");
            Category = Category.CreateRoot("Category");
            Products = new ProductRepositoryFake(Product);
            Categories = new CategoryRepositoryFake(Category);
            Handler = new UseCase(Products, Categories);
        }

        public Product Product { get; }
        public Category Category { get; }
        public ProductRepositoryFake Products { get; }
        public CategoryRepositoryFake Categories { get; }
        public UseCase Handler { get; }
        public AssignProductToCategoryCommand Command => new(Product.Id, Category.Id);
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

    private sealed class CategoryRepositoryFake(Category? category) : ICategoryRepository
    {
        public Category? Category { get; set; } = category;
        public Exception? GetException { get; set; }
        public int GetCalls { get; private set; }
        public CategoryId RequestedId { get; private set; }
        public CancellationToken GetToken { get; private set; }

        public Task<Category?> GetByIdAsync(CategoryId id, CancellationToken cancellationToken)
        {
            GetCalls++;
            RequestedId = id;
            GetToken = cancellationToken;
            if (GetException is not null) return Task.FromException<Category?>(GetException);
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(Category);
        }
    }
}