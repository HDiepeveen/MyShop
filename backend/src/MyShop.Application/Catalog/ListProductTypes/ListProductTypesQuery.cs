namespace MyShop.Application.Catalog.ListProductTypes;

public sealed record ListProductTypesQuery(
    string? SearchTerm = null,
    int Offset = 0,
    int Limit = ListProductTypes.DefaultLimit);
