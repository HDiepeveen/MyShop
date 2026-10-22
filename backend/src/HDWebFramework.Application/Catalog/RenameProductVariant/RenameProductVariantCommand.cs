using HDWebFramework.Domain.Catalog;

namespace HDWebFramework.Application.Catalog.RenameProductVariant;

public sealed record RenameProductVariantCommand(
    ProductId ProductId,
    ProductVariantId ProductVariantId,
    string Name);
