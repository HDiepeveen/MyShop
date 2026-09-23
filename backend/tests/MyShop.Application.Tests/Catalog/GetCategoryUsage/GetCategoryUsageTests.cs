using MyShop.Application.Catalog.Abstractions;
using MyShop.Application.Catalog.GetCategoryUsage;
using MyShop.Domain.Catalog;
using UseCase = MyShop.Application.Catalog.GetCategoryUsage.GetCategoryUsage;

namespace MyShop.Application.Tests.Catalog.GetCategoryUsage;

public sealed class GetCategoryUsageTests
{
    [Fact]
    public async Task ExistingEntityReturnsUsageAndForwardsIdAndTokenToBothReads()
    {
        var store = new Store();
        using var source = new CancellationTokenSource();
        var result = await new UseCase(store, store).ExecuteAsync(new(store.Entity!.Id), source.Token);
        Assert.True(result.IsSuccess);
        Assert.Null(result.Failure);
        Assert.Equal(store.Value, result.Usage);
        Assert.Equal(store.Entity.Id, store.EntityId);
        Assert.Equal(store.Entity.Id, store.UsageId);
        Assert.Equal(source.Token, store.EntityToken);
        Assert.Equal(source.Token, store.UsageToken);
        Assert.Equal(1, store.EntityReads);
        Assert.Equal(1, store.UsageReads);
    }

    [Fact]
    public async Task MissingEntityReturnsNotFoundWithoutCountingUsage()
    {
        var store = new Store { Entity = null };
        var result = await new UseCase(store, store).ExecuteAsync(new(CategoryId.New()), CancellationToken.None);
        Assert.False(result.IsSuccess);
        Assert.Equal(GetCategoryUsageFailure.CategoryNotFound, result.Failure);
        Assert.Null(result.Usage);
        Assert.Equal(0, store.UsageReads);
    }

    [Fact]
    public async Task InvalidQueryDoesNotRead()
    {
        var store = new Store();
        var useCase = new UseCase(store, store);
        await Assert.ThrowsAsync<ArgumentNullException>(() => useCase.ExecuteAsync(null!, CancellationToken.None));
        await Assert.ThrowsAsync<ArgumentException>(() => useCase.ExecuteAsync(new(default), CancellationToken.None));
        Assert.Equal(0, store.EntityReads);
        Assert.Equal(0, store.UsageReads);
    }

    [Fact]
    public async Task PreCancelledQueryDoesNotRead()
    {
        var store = new Store();
        using var source = new CancellationTokenSource();
        source.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            new UseCase(store, store).ExecuteAsync(new(store.Entity!.Id), source.Token));
        Assert.Equal(0, store.EntityReads);
        Assert.Equal(0, store.UsageReads);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task StorageFailuresAreNotConvertedToNotFound(bool usageFailure)
    {
        var expected = new InvalidOperationException("storage failure");
        var store = new Store { EntityError = usageFailure ? null : expected, UsageError = usageFailure ? expected : null };
        var actual = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            new UseCase(store, store).ExecuteAsync(new(store.Entity!.Id), CancellationToken.None));
        Assert.Same(expected, actual);
        Assert.Equal(usageFailure ? 1 : 0, store.UsageReads);
    }

    [Fact]
    public void RejectsNullDependenciesAndInvalidSuccessPayload()
    {
        var store = new Store();
        Assert.Throws<ArgumentNullException>(() => new UseCase(null!, store));
        Assert.Throws<ArgumentNullException>(() => new UseCase(store, null!));
        Assert.Throws<ArgumentNullException>(() => GetCategoryUsageResult.Succeeded(null!));
    }

    private sealed class Store : ICategoryRepository, ICategoryUsageRepository
    {
        public Category? Entity { get; init; } = Category.CreateRoot("Entity");
        public CategoryUsage Value { get; } = new CategoryUsage(2, 3);
        public Exception? EntityError { get; init; }
        public Exception? UsageError { get; init; }
        public int EntityReads { get; private set; }
        public int UsageReads { get; private set; }
        public CategoryId EntityId { get; private set; }
        public CategoryId UsageId { get; private set; }
        public CancellationToken EntityToken { get; private set; }
        public CancellationToken UsageToken { get; private set; }

        public Task<Category?> GetByIdAsync(CategoryId id, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            EntityReads++;
            EntityId = id;
            EntityToken = cancellationToken;
            return EntityError is null ? Task.FromResult(Entity) : Task.FromException<Category?>(EntityError);
        }

        public Task<CategoryUsage> GetUsageAsync(CategoryId id, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            UsageReads++;
            UsageId = id;
            UsageToken = cancellationToken;
            return UsageError is null ? Task.FromResult(Value) : Task.FromException<CategoryUsage>(UsageError);
        }
    }
}
