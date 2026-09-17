using MyShop.Domain.Catalog;

namespace MyShop.Application.Catalog.RenameCategory;

public sealed record RenameCategoryCommand(CategoryId CategoryId, string Name);
