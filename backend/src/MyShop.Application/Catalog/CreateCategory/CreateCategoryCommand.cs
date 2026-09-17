using MyShop.Domain.Catalog;

namespace MyShop.Application.Catalog.CreateCategory;

public sealed record CreateCategoryCommand(string Name, CategoryId? ParentCategoryId = null);
