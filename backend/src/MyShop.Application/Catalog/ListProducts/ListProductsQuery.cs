using MyShop.Domain.Catalog;

namespace MyShop.Application.Catalog.ListProducts;

public sealed record ListProductsQuery(
    int Offset,
    int Limit,
    ProductTypeId? ProductTypeId = null,
    CategoryId? CategoryId = null,
    string? SearchTerm = null);
