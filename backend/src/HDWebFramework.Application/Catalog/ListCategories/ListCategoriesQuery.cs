using HDWebFramework.Domain.Catalog;

namespace HDWebFramework.Application.Catalog.ListCategories;

public sealed record ListCategoriesQuery(
    string? SearchTerm = null,
    CategoryId? ParentCategoryId = null,
    bool RootsOnly = false,
    int Offset = 0,
    int Limit = ListCategories.DefaultLimit);
