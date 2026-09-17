using MyShop.Domain.Catalog;

namespace MyShop.Application.Catalog.RenameProductType;

public sealed record RenameProductTypeCommand(ProductTypeId ProductTypeId, string Name);
