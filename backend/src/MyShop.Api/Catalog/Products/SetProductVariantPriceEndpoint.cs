using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using MyShop.Application.Catalog.Abstractions;
using MyShop.Application.Catalog.SetProductVariantPrice;
using MyShop.Domain.Catalog;
using UseCase = MyShop.Application.Catalog.SetProductVariantPrice.SetProductVariantPrice;

namespace MyShop.Api.Catalog.Products;

public static class SetProductVariantPriceEndpoint
{
    public static IEndpointRouteBuilder MapSetProductVariantPrice(this IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        endpoints.MapPut(
                "/api/products/{productId:guid}/variants/{variantId:guid}/price",
                ExecuteAsync)
            .WithName("SetProductVariantPrice");

        return endpoints;
    }

    public static async Task<Results<
        NoContent,
        NotFound<ProblemDetails>,
        BadRequest<ProblemDetails>,
        Conflict<ProblemDetails>>> ExecuteAsync(
        Guid productId,
        Guid variantId,
        SetProductVariantPriceRequest request,
        [FromServices] UseCase useCase,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(useCase);

        try
        {
            var result = await useCase.ExecuteAsync(
                new SetProductVariantPriceCommand(
                    ProductId.From(productId),
                    ProductVariantId.From(variantId),
                    request.Amount,
                    request.Currency),
                cancellationToken);

            return result.Failure switch
            {
                SetProductVariantPriceFailure.ProductNotFound =>
                    TypedResults.NotFound(new ProblemDetails
                    {
                        Title = "Product not found",
                        Detail = $"Product '{productId}' does not exist."
                    }),
                SetProductVariantPriceFailure.VariantNotFound =>
                    TypedResults.NotFound(new ProblemDetails
                    {
                        Title = "Product variant not found",
                        Detail = $"Variant '{variantId}' does not exist on product '{productId}'."
                    }),
                null => TypedResults.NoContent(),
                _ => throw new InvalidOperationException(
                    $"Set price failure '{result.Failure}' is not supported.")
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

public sealed record SetProductVariantPriceRequest(decimal Amount, string Currency);
