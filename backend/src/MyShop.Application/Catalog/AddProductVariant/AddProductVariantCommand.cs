using MyShop.Domain.Catalog;

namespace MyShop.Application.Catalog.AddProductVariant;

public sealed record AddProductVariantCommand(ProductId ProductId, string Name);
