using HDWebFramework.Domain.Catalog;

namespace HDWebFramework.Application.Catalog.CreateProduct;

public sealed record CreateProductCommand(
    ProductTypeId ProductTypeId,
    string Name,
    string? InitialVariantName = null);
