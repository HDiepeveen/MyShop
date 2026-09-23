using MyShop.Application.Catalog.Abstractions;

namespace MyShop.Application.Catalog.GetProductTypeUsage;

public sealed class GetProductTypeUsage
{
    private readonly IProductTypeRepository _repository;
    private readonly IProductTypeUsageRepository _usage;

    public GetProductTypeUsage(IProductTypeRepository repository, IProductTypeUsageRepository usage)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _usage = usage ?? throw new ArgumentNullException(nameof(usage));
    }

    public async Task<GetProductTypeUsageResult> ExecuteAsync(
        GetProductTypeUsageQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);
        cancellationToken.ThrowIfCancellationRequested();
        if (query.ProductTypeId == default)
            throw new ArgumentException("Product type ID must not be empty.", nameof(query.ProductTypeId));
        if (await _repository.GetByIdAsync(query.ProductTypeId, cancellationToken) is null)
            return GetProductTypeUsageResult.Failed(GetProductTypeUsageFailure.ProductTypeNotFound);
        var value = await _usage.CountProductsAsync(query.ProductTypeId, cancellationToken);
        return GetProductTypeUsageResult.Succeeded(value);
    }
}
