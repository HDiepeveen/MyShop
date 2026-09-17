using MyShop.Application.Catalog.Abstractions;
using MyShop.Application.Catalog.GetCategory;
using MyShop.Domain.Catalog;
using UseCase = MyShop.Application.Catalog.GetCategory.GetCategory;

namespace MyShop.Application.Tests.Catalog.GetCategory;

public sealed class GetCategoryTests
{
    [Fact]
    public async Task ExecuteAsync_WhenCategoryExists_ReturnsRepositoryEntity()
    {
        var scenario = new Scenario();

        var result = await scenario.UseCase.ExecuteAsync(scenario.Query, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Null(result.Failure);
        Assert.Same(scenario.Category, result.Category);
    }

    [Fact]
    public async Task ExecuteAsync_WhenCategoryDoesNotExist_ReturnsFailure()
    {
        var scenario = new Scenario { Category = null };

        var result = await scenario.UseCase.ExecuteAsync(scenario.Query, CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(GetCategoryFailure.CategoryNotFound, result.Failure);
        Assert.Null(result.Category);
    }

    [Fact]
    public async Task ExecuteAsync_ForwardsIdAndCancellationToken()
    {
        var scenario = new Scenario();
        using var source = new CancellationTokenSource();

        await scenario.UseCase.ExecuteAsync(scenario.Query, source.Token);

        Assert.Equal(scenario.Query.CategoryId, scenario.Repository.RequestedId);
        Assert.Equal(source.Token, scenario.Repository.Token);
    }

    [Fact]
    public async Task ExecuteAsync_RejectsInvalidArgumentsBeforeRepositoryAccess()
    {
        var scenario = new Scenario();

        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            scenario.UseCase.ExecuteAsync(null!, CancellationToken.None));
        await Assert.ThrowsAsync<ArgumentException>(() =>
            scenario.UseCase.ExecuteAsync(new GetCategoryQuery(default), CancellationToken.None));

        Assert.Equal(0, scenario.Repository.GetCalls);
    }

    [Fact]
    public async Task ExecuteAsync_WhenPreCancelled_DoesNotAccessRepository()
    {
        var scenario = new Scenario();
        using var source = new CancellationTokenSource();
        source.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            scenario.UseCase.ExecuteAsync(scenario.Query, source.Token));

        Assert.Equal(0, scenario.Repository.GetCalls);
    }

    [Fact]
    public async Task ExecuteAsync_PropagatesRepositoryException()
    {
        var scenario = new Scenario();
        var expected = new InvalidOperationException("failure");
        scenario.Repository.Exception = expected;

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            scenario.UseCase.ExecuteAsync(scenario.Query, CancellationToken.None));

        Assert.Same(expected, exception);
    }

    [Fact]
    public void Constructor_RejectsNullRepository() =>
        Assert.Throws<ArgumentNullException>(() => new UseCase(null!));

    private sealed class Scenario
    {
        public Scenario()
        {
            Category = MyShop.Domain.Catalog.Category.CreateRoot("Clothing");
            Repository = new CategoryRepositoryFake(this);
            UseCase = new UseCase(Repository);
            Query = new GetCategoryQuery(Category.Id);
        }

        public Category? Category { get; set; }
        public CategoryRepositoryFake Repository { get; }
        public UseCase UseCase { get; }
        public GetCategoryQuery Query { get; }
    }

    private sealed class CategoryRepositoryFake(Scenario scenario) : ICategoryRepository
    {
        public Exception? Exception { get; set; }
        public int GetCalls { get; private set; }
        public CategoryId RequestedId { get; private set; }
        public CancellationToken Token { get; private set; }

        public Task<Category?> GetByIdAsync(CategoryId id, CancellationToken cancellationToken)
        {
            GetCalls++;
            RequestedId = id;
            Token = cancellationToken;
            if (Exception is not null)
                return Task.FromException<Category?>(Exception);
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(scenario.Category);
        }
    }
}
