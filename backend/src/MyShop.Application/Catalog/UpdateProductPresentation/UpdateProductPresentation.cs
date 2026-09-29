using MyShop.Application.Catalog.Abstractions;
using MyShop.Domain.Catalog;

namespace MyShop.Application.Catalog.UpdateProductPresentation;

public sealed record UpdateProductPresentationCommand(ProductId ProductId, Guid Revision, ProductPresentation Presentation);

public sealed class UpdateProductPresentation(IProductRepository products)
{
    private readonly IProductRepository products = products ?? throw new ArgumentNullException(nameof(products));

    public async Task<bool> ExecuteAsync(UpdateProductPresentationCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        ArgumentNullException.ThrowIfNull(command.Presentation);
        cancellationToken.ThrowIfCancellationRequested();
        if (command.ProductId == default || command.Revision == Guid.Empty)
            throw new ArgumentException("Product ID and revision are required.", nameof(command));
        var snapshot = await products.GetByIdAsync(command.ProductId, cancellationToken);
        if (snapshot is null) return false;
        if (snapshot.ConcurrencyToken.Revision != command.Revision) throw new ProductConcurrencyException(command.ProductId);
        if (snapshot.Product.Presentation == command.Presentation) return true;
        snapshot.Product.SetPresentation(command.Presentation);
        await products.SaveAsync(snapshot.Product, snapshot.ConcurrencyToken, cancellationToken);
        return true;
    }
}
