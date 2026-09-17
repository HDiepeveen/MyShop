using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using MyShop.Application.Catalog.ListProductTypes;
using UseCase = MyShop.Application.Catalog.ListProductTypes.ListProductTypes;

namespace MyShop.Api.Catalog.ProductTypes;

public static class ListProductTypesEndpoint
{
    public static IEndpointRouteBuilder MapListProductTypes(this IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        endpoints.MapGet("/api/product-types", ExecuteAsync)
            .WithName("ListProductTypes");

        return endpoints;
    }

    public static async Task<Results<
        Ok<IReadOnlyList<ProductTypeSummaryResponse>>,
        BadRequest<ProblemDetails>>> ExecuteAsync(
        [FromQuery] string? search,
        [FromServices] UseCase useCase,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(useCase);

        try
        {
            var productTypes = await useCase.ExecuteAsync(
                new ListProductTypesQuery(search), cancellationToken);
            IReadOnlyList<ProductTypeSummaryResponse> response = productTypes
                .Select(productType => new ProductTypeSummaryResponse(
                    productType.Id,
                    productType.Name,
                    productType.AttributeDefinitionCount))
                .ToArray();
            return TypedResults.Ok(response);
        }
        catch (ArgumentException exception)
        {
            return TypedResults.BadRequest(new ProblemDetails
            {
                Title = "Invalid product type search",
                Detail = exception.Message
            });
        }
    }
}

public sealed record ProductTypeSummaryResponse(
    Guid Id,
    string Name,
    int AttributeDefinitionCount);
