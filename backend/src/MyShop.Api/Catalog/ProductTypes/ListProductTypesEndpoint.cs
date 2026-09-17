using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
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

    public static async Task<Ok<IReadOnlyList<ProductTypeSummaryResponse>>> ExecuteAsync(
        [FromServices] UseCase useCase,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(useCase);

        var productTypes = await useCase.ExecuteAsync(cancellationToken);
        IReadOnlyList<ProductTypeSummaryResponse> response = productTypes
            .Select(productType => new ProductTypeSummaryResponse(
                productType.Id,
                productType.Name))
            .ToArray();

        return TypedResults.Ok(response);
    }
}

public sealed record ProductTypeSummaryResponse(Guid Id, string Name);
