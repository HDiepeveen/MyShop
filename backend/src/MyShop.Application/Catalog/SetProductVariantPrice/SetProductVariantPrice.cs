using MyShop.Application.Catalog.Abstractions;
using MyShop.Domain.Catalog;

namespace MyShop.Application.Catalog.SetProductVariantPrice;

public sealed record SetProductVariantPriceCommand(ProductId ProductId, ProductVariantId ProductVariantId, decimal Amount, string Currency);
public enum SetProductVariantPriceFailure { ProductNotFound, VariantNotFound }
public sealed record SetProductVariantPriceResult(SetProductVariantPriceFailure? Failure)
{
    public bool IsSuccess => Failure is null;
}

public sealed class SetProductVariantPrice
{
    private readonly IProductRepository _products;

    public SetProductVariantPrice(IProductRepository products) =>
        _products = products ?? throw new ArgumentNullException(nameof(products));

    public async Task<SetProductVariantPriceResult> ExecuteAsync(SetProductVariantPriceCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        cancellationToken.ThrowIfCancellationRequested();
        if (command.ProductId == default) throw new ArgumentException("Product ID must not be empty.", nameof(command.ProductId));
        if (command.ProductVariantId == default) throw new ArgumentException("Product variant ID must not be empty.", nameof(command.ProductVariantId));
        var snapshot = await _products.GetByIdAsync(command.ProductId, cancellationToken);
        if (snapshot is null) return new(SetProductVariantPriceFailure.ProductNotFound);
        var variant = snapshot.Product.Variants.SingleOrDefault(candidate => candidate.Id == command.ProductVariantId);
        if (variant is null) return new(SetProductVariantPriceFailure.VariantNotFound);
        var price = Money.Create(command.Amount, command.Currency);
        if (variant.Price == price) return new(null);
        snapshot.Product.SetVariantPrice(command.ProductVariantId, price);
        await _products.SaveAsync(snapshot.Product, snapshot.ConcurrencyToken, cancellationToken);
        return new(null);
    }
}
