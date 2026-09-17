using MyShop.Application.Catalog.Abstractions;
using MyShop.Domain.Catalog;

namespace MyShop.Application.Catalog.CreateCategory;

public sealed class CreateCategory
{
    private readonly ICategoryRepository _categories;
    private readonly ICategoryWriter _writer;

    public CreateCategory(ICategoryRepository categories, ICategoryWriter writer)
    {
        _categories = categories ?? throw new ArgumentNullException(nameof(categories));
        _writer = writer ?? throw new ArgumentNullException(nameof(writer));
    }

    public async Task<CreateCategoryResult> ExecuteAsync(
        CreateCategoryCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        cancellationToken.ThrowIfCancellationRequested();
        if (command.ParentCategoryId == default(CategoryId))
            throw new ArgumentException("Parent category ID must not be empty.", nameof(command.ParentCategoryId));

        if (command.ParentCategoryId is not null &&
            await _categories.GetByIdAsync(command.ParentCategoryId.Value, cancellationToken) is null)
            return new CreateCategoryResult(null, true);

        var category = command.ParentCategoryId is null
            ? Category.CreateRoot(command.Name)
            : Category.CreateChild(command.Name, command.ParentCategoryId.Value);
        await _writer.AddAsync(category, cancellationToken);
        return new CreateCategoryResult(category, false);
    }
}

public sealed record CreateCategoryResult(Category? Category, bool ParentNotFound);
