using HDWebFramework.Domain.Catalog;

namespace HDWebFramework.Application.Catalog.RenameProductType;

public sealed record RenameProductTypeCommand(ProductTypeId ProductTypeId, string Name);
