using HDWebFramework.Domain.Catalog;

namespace HDWebFramework.Application.Catalog.CreateCategory;

public sealed record CreateCategoryCommand(string Name, CategoryId? ParentCategoryId = null);
