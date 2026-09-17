namespace MyShop.Application.Catalog.Abstractions;

public interface ICategoryListRepository
{
    Task<IReadOnlyList<CategoryListItem>> ListAsync(CancellationToken cancellationToken);
}

public sealed record CategoryListItem(Guid Id, string Name, Guid? ParentCategoryId);
