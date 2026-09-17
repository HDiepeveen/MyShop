using MyShop.Application.Catalog.Abstractions;
using MyShop.Domain.Catalog;

namespace MyShop.Application.Catalog.MoveCategory;

public sealed class MoveCategory
{
    private readonly ICategoryRepository _categories;
    private readonly ICategoryHierarchyRepository _hierarchy;
    private readonly ICategoryWriter _writer;

    public MoveCategory(
        ICategoryRepository categories,
        ICategoryHierarchyRepository hierarchy,
        ICategoryWriter writer)
    {
        _categories = categories ?? throw new ArgumentNullException(nameof(categories));
        _hierarchy = hierarchy ?? throw new ArgumentNullException(nameof(hierarchy));
        _writer = writer ?? throw new ArgumentNullException(nameof(writer));
    }

    public async Task<MoveCategoryResult> ExecuteAsync(
        MoveCategoryCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        cancellationToken.ThrowIfCancellationRequested();
        if (command.CategoryId == default || command.ParentCategoryId == default(CategoryId))
            throw new ArgumentException("Category IDs must not be empty.", nameof(command));

        var category = await _categories.GetByIdAsync(command.CategoryId, cancellationToken);
        if (category is null)
            return MoveCategoryResult.CategoryNotFound;
        if (category.ParentCategoryId == command.ParentCategoryId)
            return MoveCategoryResult.Succeeded;

        if (command.ParentCategoryId is not null)
        {
            var parent = await _categories.GetByIdAsync(command.ParentCategoryId.Value, cancellationToken);
            if (parent is null)
                return MoveCategoryResult.ParentNotFound;
            if (await _hierarchy.IsDescendantOfAsync(
                command.ParentCategoryId.Value, command.CategoryId, cancellationToken))
                return MoveCategoryResult.CycleDetected;
            category.MoveUnder(parent.Id);
        }
        else
        {
            category.MoveToRoot();
        }

        await _writer.SaveAsync(category, cancellationToken);
        return MoveCategoryResult.Succeeded;
    }
}

public enum MoveCategoryResult
{
    Succeeded,
    CategoryNotFound,
    ParentNotFound,
    CycleDetected
}
