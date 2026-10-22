using HDWebFramework.Domain.Catalog;

namespace HDWebFramework.Application.Catalog.AddProductVariant;

public sealed record AddProductVariantCommand(ProductId ProductId, string Name);
