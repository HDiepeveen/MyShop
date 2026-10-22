using HDWebFramework.Application.Catalog.Abstractions;
using HDWebFramework.Domain.Catalog;

namespace HDWebFramework.Application.Catalog.ListProducts;

public sealed record ListProductsQuery(
    int Offset,
    int Limit,
    ProductTypeId? ProductTypeId = null,
    CategoryId? CategoryId = null,
    string? SearchTerm = null, bool? IsPublished = null, ProductStockFilter? Stock = null);
