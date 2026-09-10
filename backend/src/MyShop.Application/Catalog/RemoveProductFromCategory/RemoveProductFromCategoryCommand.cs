using MyShop.Domain.Catalog;

namespace MyShop.Application.Catalog.RemoveProductFromCategory;

public sealed record RemoveProductFromCategoryCommand(
    ProductId ProductId,
    CategoryId CategoryId);