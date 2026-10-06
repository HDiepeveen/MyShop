using MyShop.Application.Catalog.Abstractions;
using MyShop.Domain.Catalog;

namespace MyShop.Application.Catalog.ListProducts;

public sealed record ListProductsQuery(
    int Offset,
    int Limit,
    ProductTypeId? ProductTypeId = null,
    CategoryId? CategoryId = null,
    string? SearchTerm = null, bool? IsPublished = null, ProductStockFilter? Stock = null);
