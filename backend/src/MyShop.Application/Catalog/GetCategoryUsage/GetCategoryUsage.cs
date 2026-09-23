using MyShop.Application.Catalog.Abstractions;

namespace MyShop.Application.Catalog.GetCategoryUsage;

public sealed class GetCategoryUsage
{
    private readonly ICategoryRepository _repository;
    private readonly ICategoryUsageRepository _usage;

    public GetCategoryUsage(ICategoryRepository repository, ICategoryUsageRepository usage)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _usage = usage ?? throw new ArgumentNullException(nameof(usage));
    }

    public async Task<GetCategoryUsageResult> ExecuteAsync(
        GetCategoryUsageQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);
        cancellationToken.ThrowIfCancellationRequested();
        if (query.CategoryId == default)
            throw new ArgumentException("Category ID must not be empty.", nameof(query.CategoryId));
        if (await _repository.GetByIdAsync(query.CategoryId, cancellationToken) is null)
            return GetCategoryUsageResult.Failed(GetCategoryUsageFailure.CategoryNotFound);
        var value = await _usage.GetUsageAsync(query.CategoryId, cancellationToken);
        return GetCategoryUsageResult.Succeeded(value);
    }
}
