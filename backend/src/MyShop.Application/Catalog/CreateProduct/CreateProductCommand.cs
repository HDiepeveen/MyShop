using MyShop.Domain.Catalog;

namespace MyShop.Application.Catalog.CreateProduct;

public sealed record CreateProductCommand(
    ProductTypeId ProductTypeId,
    string Name,
    string InitialVariantName);
