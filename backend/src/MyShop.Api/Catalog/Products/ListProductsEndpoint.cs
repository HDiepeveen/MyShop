using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using MyShop.Application.Catalog.ListProducts;
using UseCase = MyShop.Application.Catalog.ListProducts.ListProducts;

namespace MyShop.Api.Catalog.Products;

public static class ListProductsEndpoint
{
    public static IEndpointRouteBuilder MapListProducts(this IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        endpoints.MapGet("/api/products", ExecuteAsync)
            .WithName("ListProducts");

        return endpoints;
    }

    public static async Task<Results<
        Ok<ProductListResponse>,
        BadRequest<ProblemDetails>>> ExecuteAsync(
        [FromQuery] int? offset,
        [FromQuery] int? limit,
        [FromServices] UseCase useCase,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(useCase);

        var effectiveOffset = offset ?? 0;
        var effectiveLimit = limit ?? UseCase.DefaultLimit;

        try
        {
            var page = await useCase.ExecuteAsync(
                new ListProductsQuery(effectiveOffset, effectiveLimit),
                cancellationToken);
            var items = page.Items.Select(item => new ProductSummaryResponse(
                item.Id,
                item.ProductTypeId,
                item.Name,
                item.VariantCount)).ToArray();

            return TypedResults.Ok(new ProductListResponse(
                items,
                effectiveOffset,
                effectiveLimit,
                page.TotalCount));
        }
        catch (ArgumentOutOfRangeException exception)
        {
            return TypedResults.BadRequest(new ProblemDetails
            {
                Title = "Invalid product paging",
                Detail = exception.Message
            });
        }
    }
}

public sealed record ProductListResponse(
    IReadOnlyList<ProductSummaryResponse> Items,
    int Offset,
    int Limit,
    int TotalCount);

public sealed record ProductSummaryResponse(
    Guid Id,
    Guid ProductTypeId,
    string Name,
    int VariantCount);
