using MyShop.Domain.Catalog;

namespace MyShop.Application.Catalog.ListCategories;

public sealed record ListCategoriesQuery(
    string? SearchTerm = null,
    CategoryId? ParentCategoryId = null,
    bool RootsOnly = false,
    int Offset = 0,
    int Limit = ListCategories.DefaultLimit);
