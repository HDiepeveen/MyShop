using MyShop.Domain.Catalog;

namespace MyShop.Application.Catalog.AssignProductToCategory;

public sealed record AssignProductToCategoryCommand(
    ProductId ProductId,
    CategoryId CategoryId);