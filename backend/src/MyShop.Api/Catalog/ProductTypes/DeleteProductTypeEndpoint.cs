using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using MyShop.Application.Catalog.DeleteProductType;
using MyShop.Domain.Catalog;
using UseCase = MyShop.Application.Catalog.DeleteProductType.DeleteProductType;

namespace MyShop.Api.Catalog.ProductTypes;

public static class DeleteProductTypeEndpoint
{
    public static IEndpointRouteBuilder MapDeleteProductType(this IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);
        endpoints.MapDelete("/api/product-types/{productTypeId:guid}", ExecuteAsync)
            .WithName("DeleteProductType");
        return endpoints;
    }

    public static async Task<Results<NoContent, NotFound<ProblemDetails>,
        Conflict<ProblemDetails>, BadRequest<ProblemDetails>>> ExecuteAsync(
        Guid productTypeId, [FromServices] UseCase useCase, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(useCase);
        try
        {
            var result = await useCase.ExecuteAsync(ProductTypeId.From(productTypeId), cancellationToken);
            return result.Outcome switch
            {
                DeleteProductTypeOutcome.Succeeded => TypedResults.NoContent(),
                DeleteProductTypeOutcome.NotFound => TypedResults.NotFound(new ProblemDetails
                    { Title = "Product type not found" }),
                DeleteProductTypeOutcome.InUse => TypedResults.Conflict(new ProblemDetails
                {
                    Title = "Product type is in use",
                    Detail = $"Product type is used by {result.ProductCount} products."
                }),
                _ => throw new InvalidOperationException("Unsupported delete product type result.")
            };
        }
        catch (ArgumentException exception)
        {
            return TypedResults.BadRequest(new ProblemDetails
            {
                Title = "Invalid product type deletion", Detail = exception.Message
            });
        }
    }
}
