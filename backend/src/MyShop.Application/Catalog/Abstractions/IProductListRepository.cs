using MyShop.Domain.Catalog;

namespace MyShop.Application.Catalog.Abstractions;

public interface IProductListRepository
{
    Task<ProductListPage> ListAsync(
        int offset,
        int limit,
        ProductTypeId? productTypeId,
        CategoryId? categoryId,
        string? searchTerm,
        CancellationToken cancellationToken);
}

public sealed record ProductListItem(
    Guid Id,
    Guid ProductTypeId,
    string Name,
    int VariantCount);

public sealed record ProductListPage(
    IReadOnlyList<ProductListItem> Items,
    int TotalCount);
