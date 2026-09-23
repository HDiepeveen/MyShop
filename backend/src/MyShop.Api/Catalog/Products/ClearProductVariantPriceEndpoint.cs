using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using MyShop.Application.Catalog.Abstractions;
using MyShop.Application.Catalog.ClearProductVariantPrice;
using MyShop.Domain.Catalog;
using UseCase = MyShop.Application.Catalog.ClearProductVariantPrice.ClearProductVariantPrice;

namespace MyShop.Api.Catalog.Products;

public static class ClearProductVariantPriceEndpoint
{
    public static IEndpointRouteBuilder MapClearProductVariantPrice(this IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        endpoints.MapDelete(
                "/api/products/{productId:guid}/variants/{variantId:guid}/price",
                ExecuteAsync)
            .WithName("ClearProductVariantPrice");

        return endpoints;
    }

    public static async Task<Results<NoContent, NotFound<ProblemDetails>, BadRequest<ProblemDetails>, Conflict<ProblemDetails>>>
        ExecuteAsync(Guid productId, Guid variantId, [FromServices] UseCase useCase, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(useCase);

        try
        {
            var result = await useCase.ExecuteAsync(
                new ClearProductVariantPriceCommand(ProductId.From(productId), ProductVariantId.From(variantId)),
                cancellationToken);

            return result.Failure switch
            {
                ClearProductVariantPriceFailure.ProductNotFound => TypedResults.NotFound(new ProblemDetails
                {
                    Title = "Product not found",
                    Detail = $"Product '{productId}' does not exist."
                }),
                ClearProductVariantPriceFailure.VariantNotFound => TypedResults.NotFound(new ProblemDetails
                {
                    Title = "Product variant not found",
                    Detail = $"Variant '{variantId}' does not exist on product '{productId}'."
                }),
                null => TypedResults.NoContent(),
                _ => throw new InvalidOperationException($"Clear price failure '{result.Failure}' is not supported.")
            };
        }
        catch (ProductConcurrencyException exception)
        {
            return TypedResults.Conflict(new ProblemDetails
            {
                Title = "Product was modified",
                Detail = exception.Message
            });
        }
        catch (ArgumentException exception)
        {
            return TypedResults.BadRequest(new ProblemDetails
            {
                Title = "Invalid product variant price",
                Detail = exception.Message
            });
        }
    }
}
