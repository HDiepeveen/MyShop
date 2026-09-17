using MyShop.Application.Catalog.Abstractions;
using MyShop.Application.Catalog.GetProductType;
using MyShop.Domain.Catalog;
using UseCase = MyShop.Application.Catalog.GetProductType.GetProductType;

namespace MyShop.Application.Tests.Catalog.GetProductType;

public sealed class GetProductTypeTests
{
    [Fact]
    public async Task ExecuteAsync_WhenProductTypeExists_ReturnsRepositoryEntity()
    {
        var scenario = new Scenario();

        var result = await scenario.UseCase.ExecuteAsync(scenario.Query, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Null(result.Failure);
        Assert.Same(scenario.ProductType, result.ProductType);
    }

    [Fact]
    public async Task ExecuteAsync_WhenProductTypeDoesNotExist_ReturnsFailure()
    {
        var scenario = new Scenario { ProductType = null };

        var result = await scenario.UseCase.ExecuteAsync(scenario.Query, CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(GetProductTypeFailure.ProductTypeNotFound, result.Failure);
        Assert.Null(result.ProductType);
    }

    [Fact]
    public async Task ExecuteAsync_ForwardsIdAndCancellationToken()
    {
        var scenario = new Scenario();
        using var source = new CancellationTokenSource();

        await scenario.UseCase.ExecuteAsync(scenario.Query, source.Token);

        Assert.Equal(scenario.Query.ProductTypeId, scenario.Repository.RequestedId);
        Assert.Equal(source.Token, scenario.Repository.Token);
    }

    [Fact]
    public async Task ExecuteAsync_RejectsInvalidArgumentsBeforeRepositoryAccess()
    {
        var scenario = new Scenario();

        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            scenario.UseCase.ExecuteAsync(null!, CancellationToken.None));
        await Assert.ThrowsAsync<ArgumentException>(() =>
            scenario.UseCase.ExecuteAsync(new GetProductTypeQuery(default), CancellationToken.None));

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
            ProductType = MyShop.Domain.Catalog.ProductType.Create("Clothing");
            Repository = new ProductTypeRepositoryFake(this);
            UseCase = new UseCase(Repository);
            Query = new GetProductTypeQuery(ProductType.Id);
        }

        public ProductType? ProductType { get; set; }
        public ProductTypeRepositoryFake Repository { get; }
        public UseCase UseCase { get; }
        public GetProductTypeQuery Query { get; }
    }

    private sealed class ProductTypeRepositoryFake(Scenario scenario) : IProductTypeRepository
    {
        public Exception? Exception { get; set; }
        public int GetCalls { get; private set; }
        public ProductTypeId RequestedId { get; private set; }
        public CancellationToken Token { get; private set; }

        public Task<ProductType?> GetByIdAsync(ProductTypeId id, CancellationToken cancellationToken)
        {
            GetCalls++;
            RequestedId = id;
            Token = cancellationToken;
            if (Exception is not null)
                return Task.FromException<ProductType?>(Exception);
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(scenario.ProductType);
        }
    }
}
