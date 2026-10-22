using HDWebFramework.Domain.Catalog;

namespace HDWebFramework.Application.Catalog.AssignProductToCategory;

public sealed record AssignProductToCategoryCommand(
    ProductId ProductId,
    CategoryId CategoryId);