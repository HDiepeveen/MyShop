using MyShop.Application.Catalog.Abstractions;
using MyShop.Domain.Catalog;
using UseCase = MyShop.Application.Catalog.DeleteProduct.DeleteProduct;

namespace MyShop.Application.Tests.Catalog.DeleteProduct;

public sealed class DeleteProductTests
{
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task ExecuteAsync_ReturnsDeleterOutcomeAndForwardsArguments(bool deleted)
    {
        var deleter = new DeleterFake(deleted);
        var id = ProductId.New();
        using var source = new CancellationTokenSource();
        Assert.Equal(deleted, await new UseCase(deleter).ExecuteAsync(id, source.Token));
        Assert.Equal(id, deleter.Id);
        Assert.Equal(source.Token, deleter.Token);
    }

    [Fact]
    public void Constructor_RejectsNullDeleter() =>
        Assert.Throws<ArgumentNullException>(() => new UseCase(null!));

    [Fact]
    public async Task ExecuteAsync_RejectsEmptyIdBeforeDeleterAccess()
    {
        var deleter = new DeleterFake(true);
        await Assert.ThrowsAsync<ArgumentException>(() =>
            new UseCase(deleter).ExecuteAsync(default, CancellationToken.None));
        Assert.Null(deleter.Id);
    }

    [Fact]
    public async Task ExecuteAsync_PreCancelledRequestSkipsDeleter()
    {
        var deleter = new DeleterFake(true);
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            new UseCase(deleter).ExecuteAsync(ProductId.New(), new CancellationToken(canceled: true)));
        Assert.Null(deleter.Id);
    }

    private sealed class DeleterFake(bool outcome) : IProductDeleter
    {
        public ProductId? Id { get; private set; }
        public CancellationToken Token { get; private set; }
        public Task<bool> DeleteAsync(ProductId productId, CancellationToken cancellationToken)
        {
            Id = productId;
            Token = cancellationToken;
            return Task.FromResult(outcome);
        }
    }
}
