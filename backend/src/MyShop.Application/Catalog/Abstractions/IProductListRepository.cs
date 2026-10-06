using MyShop.Domain.Catalog;

namespace MyShop.Application.Catalog.Abstractions;

public enum ProductStockFilter { Low, OutOfStock, Untracked }

public interface IProductListRepository
{
    Task<ProductListPage> ListAsync(
        int offset,
        int limit,
        ProductTypeId? productTypeId,
        CategoryId? categoryId,
        string? searchTerm,
        CancellationToken cancellationToken, bool? isPublished = null, ProductStockFilter? stock = null);
}

public sealed record ProductListItem(
    Guid Id,
    Guid ProductTypeId,
    string Name,
    int VariantCount,
    bool IsPublished = false);

public sealed record ProductListPage(
    IReadOnlyList<ProductListItem> Items,
    int TotalCount);
