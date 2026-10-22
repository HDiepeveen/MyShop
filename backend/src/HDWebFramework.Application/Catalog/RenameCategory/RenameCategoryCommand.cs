using HDWebFramework.Domain.Catalog;

namespace HDWebFramework.Application.Catalog.RenameCategory;

public sealed record RenameCategoryCommand(CategoryId CategoryId, string Name);
