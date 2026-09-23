using MyShop.Application.Catalog.Abstractions;
using MyShop.Application.Catalog.GetProductTypeUsage;
using MyShop.Domain.Catalog;
using UseCase = MyShop.Application.Catalog.GetProductTypeUsage.GetProductTypeUsage;

namespace MyShop.Application.Tests.Catalog.GetProductTypeUsage;

public sealed class GetProductTypeUsageTests
{
    [Fact]
    public async Task ExistingEntityReturnsUsageAndForwardsIdAndTokenToBothReads()
    {
        var store = new Store();
        using var source = new CancellationTokenSource();
        var result = await new UseCase(store, store).ExecuteAsync(new(store.Entity!.Id), source.Token);
        Assert.True(result.IsSuccess);
        Assert.Null(result.Failure);
        Assert.Equal(store.Value, result.ProductCount);
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
        var result = await new UseCase(store, store).ExecuteAsync(new(ProductTypeId.New()), CancellationToken.None);
        Assert.False(result.IsSuccess);
        Assert.Equal(GetProductTypeUsageFailure.ProductTypeNotFound, result.Failure);
        Assert.Null(result.ProductCount);
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
        Assert.Throws<ArgumentOutOfRangeException>(() => GetProductTypeUsageResult.Succeeded(-1));
    }

    private sealed class Store : IProductTypeRepository, IProductTypeUsageRepository
    {
        public ProductType? Entity { get; init; } = ProductType.Create("Entity");
        public int Value { get; } = 3;
        public Exception? EntityError { get; init; }
        public Exception? UsageError { get; init; }
        public int EntityReads { get; private set; }
        public int UsageReads { get; private set; }
        public ProductTypeId EntityId { get; private set; }
        public ProductTypeId UsageId { get; private set; }
        public CancellationToken EntityToken { get; private set; }
        public CancellationToken UsageToken { get; private set; }

        public Task<ProductType?> GetByIdAsync(ProductTypeId id, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            EntityReads++;
            EntityId = id;
            EntityToken = cancellationToken;
            return EntityError is null ? Task.FromResult(Entity) : Task.FromException<ProductType?>(EntityError);
        }

        public Task<int> CountProductsAsync(ProductTypeId id, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            UsageReads++;
            UsageId = id;
            UsageToken = cancellationToken;
            return UsageError is null ? Task.FromResult(Value) : Task.FromException<int>(UsageError);
        }
    }
}
