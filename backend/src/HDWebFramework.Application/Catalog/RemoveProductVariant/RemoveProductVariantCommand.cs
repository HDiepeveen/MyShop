using HDWebFramework.Domain.Catalog;

namespace HDWebFramework.Application.Catalog.RemoveProductVariant;

public sealed record RemoveProductVariantCommand(
    ProductId ProductId,
    ProductVariantId ProductVariantId);
