using MyShop.Application.Catalog.Abstractions;
using MyShop.Application.Catalog.UpdateProductPresentation;
using MyShop.Domain.Catalog;

namespace MyShop.Application.Tests.Catalog.UpdateProductPresentation;

public sealed class UpdateProductPresentationTests
{
    [Fact]
    public async Task SavesWithReadRevisionAndAvoidsNoOpWrites()
    {
        var repository = new Repository();
        var handler = new MyShop.Application.Catalog.UpdateProductPresentation.UpdateProductPresentation(repository);
        var command = new UpdateProductPresentationCommand(repository.Product.Id, repository.Token.Revision,
            ProductPresentation.Create("Description", null, "", false));
        Assert.True(await handler.ExecuteAsync(command, CancellationToken.None));
        Assert.Equal(command.Presentation, repository.Product.Presentation);
        Assert.Equal(repository.Token, repository.SavedToken);
        Assert.Equal(1, repository.Saves);
        Assert.True(await handler.ExecuteAsync(command, CancellationToken.None));
        Assert.Equal(1, repository.Saves);
    }
    [Fact]
    public async Task RejectsStaleRevisionBeforeMutatingProduct()
    {
        var repository = new Repository();
        var handler = new MyShop.Application.Catalog.UpdateProductPresentation.UpdateProductPresentation(repository);
        await Assert.ThrowsAsync<ProductConcurrencyException>(() => handler.ExecuteAsync(new(repository.Product.Id, Guid.NewGuid(),
            ProductPresentation.Create("Description", null, "", false)), CancellationToken.None));
        Assert.Equal(ProductPresentation.Draft, repository.Product.Presentation);
        Assert.Equal(0, repository.Saves);
    }
    [Fact]
    public async Task HandlesMissingProductAndCancellationWithoutSaving()
    {
        var repository = new Repository { Missing = true };
        var handler = new MyShop.Application.Catalog.UpdateProductPresentation.UpdateProductPresentation(repository);
        var command = new UpdateProductPresentationCommand(repository.Product.Id, repository.Token.Revision, ProductPresentation.Draft);
        Assert.False(await handler.ExecuteAsync(command, CancellationToken.None));
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => handler.ExecuteAsync(command, new CancellationToken(true)));
        await Assert.ThrowsAsync<ArgumentException>(() => handler.ExecuteAsync(command with { Revision = Guid.Empty }, CancellationToken.None));
        Assert.Equal(0, repository.Saves);
    }
    private sealed class Repository : IProductRepository
    {
        internal Product Product { get; } = Product.Create("Shirt", ProductTypeId.New(), "Default");
        internal ProductConcurrencyToken Token { get; }
        internal bool Missing { get; init; }
        internal int Saves { get; private set; }
        internal ProductConcurrencyToken? SavedToken { get; private set; }
        internal Repository() => Token = ProductConcurrencyToken.Create(Product.Id, Guid.NewGuid());
        public Task<ProductSnapshot?> GetByIdAsync(ProductId id, CancellationToken cancellationToken) => Task.FromResult(Missing ? null : new ProductSnapshot(Product, Token));
        public Task<ProductConcurrencyToken> AddAsync(Product product, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<ProductConcurrencyToken> SaveAsync(Product product, ProductConcurrencyToken expectedToken, CancellationToken cancellationToken)
        { Saves++; SavedToken = expectedToken; return Task.FromResult(Token); }
    }
}
