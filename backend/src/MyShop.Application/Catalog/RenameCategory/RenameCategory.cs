using MyShop.Application.Catalog.Abstractions;

namespace MyShop.Application.Catalog.RenameCategory;

public sealed class RenameCategory
{
    private readonly ICategoryRepository _categories;
    private readonly ICategoryWriter _writer;

    public RenameCategory(ICategoryRepository categories, ICategoryWriter writer)
    {
        _categories = categories ?? throw new ArgumentNullException(nameof(categories));
        _writer = writer ?? throw new ArgumentNullException(nameof(writer));
    }

    public async Task<bool> ExecuteAsync(RenameCategoryCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        cancellationToken.ThrowIfCancellationRequested();
        if (command.CategoryId == default)
            throw new ArgumentException("Category ID must not be empty.", nameof(command.CategoryId));

        var category = await _categories.GetByIdAsync(command.CategoryId, cancellationToken);
        if (category is null)
            return false;
        if (category.Name == command.Name)
            return true;

        category.Rename(command.Name);
        await _writer.SaveAsync(category, cancellationToken);
        return true;
    }
}
