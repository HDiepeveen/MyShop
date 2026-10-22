using HDWebFramework.Domain.Catalog;

namespace HDWebFramework.Application.Catalog.RemoveProductFromCategory;

public sealed record RemoveProductFromCategoryCommand(
    ProductId ProductId,
    CategoryId CategoryId);