using HDWebFramework.Domain.Catalog;

namespace HDWebFramework.Application.Catalog.MoveCategory;

public sealed record MoveCategoryCommand(CategoryId CategoryId, CategoryId? ParentCategoryId);
