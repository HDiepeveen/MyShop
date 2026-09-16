using MyShop.Domain.Catalog;

namespace MyShop.Application.Catalog.RenameProductVariant;

public sealed record RenameProductVariantCommand(
    ProductId ProductId,
    ProductVariantId ProductVariantId,
    string Name);
