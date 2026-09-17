using MyShop.Application.Catalog.Abstractions;
using MyShop.Domain.Catalog;

namespace MyShop.Application.Catalog.DeleteCategory;

public sealed class DeleteCategory
{
    private readonly ICategoryRepository _categories;
    private readonly ICategoryUsageRepository _usage;
    private readonly ICategoryWriter _writer;

    public DeleteCategory(
        ICategoryRepository categories,
        ICategoryUsageRepository usage,
        ICategoryWriter writer)
    {
        _categories = categories ?? throw new ArgumentNullException(nameof(categories));
        _usage = usage ?? throw new ArgumentNullException(nameof(usage));
        _writer = writer ?? throw new ArgumentNullException(nameof(writer));
    }

    public async Task<DeleteCategoryResult> ExecuteAsync(
        CategoryId categoryId, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (categoryId == default)
            throw new ArgumentException("Category ID must not be empty.", nameof(categoryId));
        if (await _categories.GetByIdAsync(categoryId, cancellationToken) is null)
            return new(DeleteCategoryOutcome.NotFound, null);

        var usage = await _usage.GetUsageAsync(categoryId, cancellationToken);
        if (usage.IsInUse)
            return new(DeleteCategoryOutcome.InUse, usage);

        await _writer.DeleteAsync(categoryId, cancellationToken);
        return new(DeleteCategoryOutcome.Succeeded, usage);
    }
}

public sealed record DeleteCategoryResult(DeleteCategoryOutcome Outcome, CategoryUsage? Usage);

public enum DeleteCategoryOutcome
{
    Succeeded,
    NotFound,
    InUse
}
