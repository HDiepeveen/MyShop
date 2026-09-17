using MyShop.Domain.Catalog;

namespace MyShop.Application.Catalog.MoveCategory;

public sealed record MoveCategoryCommand(CategoryId CategoryId, CategoryId? ParentCategoryId);
