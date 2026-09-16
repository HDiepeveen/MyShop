using MyShop.Domain.Catalog;

namespace MyShop.Application.Catalog.RenameProduct;

public sealed record RenameProductCommand(ProductId ProductId, string Name);
