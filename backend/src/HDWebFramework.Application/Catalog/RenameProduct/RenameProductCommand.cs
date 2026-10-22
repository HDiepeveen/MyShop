using HDWebFramework.Domain.Catalog;

namespace HDWebFramework.Application.Catalog.RenameProduct;

public sealed record RenameProductCommand(ProductId ProductId, string Name);
