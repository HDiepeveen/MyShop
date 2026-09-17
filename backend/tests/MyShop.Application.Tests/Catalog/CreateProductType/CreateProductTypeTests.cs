using MyShop.Application.Catalog.Abstractions;
using MyShop.Application.Catalog.CreateProductType;
using MyShop.Domain.Catalog;
using UseCase = MyShop.Application.Catalog.CreateProductType.CreateProductType;

namespace MyShop.Application.Tests.Catalog.CreateProductType;

public sealed class CreateProductTypeTests
{
    [Fact]
    public async Task ExecuteAsync_CreatesAndPersistsProductType()
    {
        var writer = new WriterFake();
        var useCase = new UseCase(writer);

        var result = await useCase.ExecuteAsync(
            new CreateProductTypeCommand("Clothing"), CancellationToken.None);

        Assert.Same(result, writer.Added);
        Assert.Equal("Clothing", result.Name);
    }

    [Fact]
    public async Task ExecuteAsync_RejectsInvalidInputAndCancellation()
    {
        var writer = new WriterFake();
        var useCase = new UseCase(writer);

        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            useCase.ExecuteAsync(null!, CancellationToken.None));
        await Assert.ThrowsAsync<ArgumentException>(() =>
            useCase.ExecuteAsync(new CreateProductTypeCommand(" "), CancellationToken.None));
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            useCase.ExecuteAsync(new CreateProductTypeCommand("Clothing"), new CancellationToken(true)));
        Assert.Null(writer.Added);
    }

    private sealed class WriterFake : IProductTypeWriter
    {
        public ProductType? Added { get; private set; }

        public Task AddAsync(ProductType productType, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Added = productType;
            return Task.CompletedTask;
        }

        public Task SaveAsync(ProductType productType, CancellationToken cancellationToken) =>
            throw new NotSupportedException();
    }
}
