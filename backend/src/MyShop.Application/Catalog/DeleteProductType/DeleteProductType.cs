using MyShop.Application.Catalog.Abstractions;
using MyShop.Domain.Catalog;

namespace MyShop.Application.Catalog.DeleteProductType;

public sealed class DeleteProductType
{
    private readonly IProductTypeRepository _productTypes;
    private readonly IProductTypeUsageRepository _usage;
    private readonly IProductTypeDeleter _deleter;

    public DeleteProductType(
        IProductTypeRepository productTypes,
        IProductTypeUsageRepository usage,
        IProductTypeDeleter deleter)
    {
        _productTypes = productTypes ?? throw new ArgumentNullException(nameof(productTypes));
        _usage = usage ?? throw new ArgumentNullException(nameof(usage));
        _deleter = deleter ?? throw new ArgumentNullException(nameof(deleter));
    }

    public async Task<DeleteProductTypeResult> ExecuteAsync(
        ProductTypeId productTypeId, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (productTypeId == default)
            throw new ArgumentException("Product type ID must not be empty.", nameof(productTypeId));
        if (await _productTypes.GetByIdAsync(productTypeId, cancellationToken) is null)
            return new(DeleteProductTypeOutcome.NotFound, 0);

        var productCount = await _usage.CountProductsAsync(productTypeId, cancellationToken);
        if (productCount > 0)
            return new(DeleteProductTypeOutcome.InUse, productCount);

        await _deleter.DeleteAsync(productTypeId, cancellationToken);
        return new(DeleteProductTypeOutcome.Succeeded, 0);
    }
}

public sealed record DeleteProductTypeResult(DeleteProductTypeOutcome Outcome, int ProductCount);

public enum DeleteProductTypeOutcome
{
    Succeeded,
    NotFound,
    InUse
}
