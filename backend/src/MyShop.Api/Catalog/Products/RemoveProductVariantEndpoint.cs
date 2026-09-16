using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using MyShop.Application.Catalog.Abstractions;
using MyShop.Application.Catalog.RemoveProductVariant;
using MyShop.Domain.Catalog;
using UseCase = MyShop.Application.Catalog.RemoveProductVariant.RemoveProductVariant;

namespace MyShop.Api.Catalog.Products;

public static class RemoveProductVariantEndpoint
{
    public static IEndpointRouteBuilder MapRemoveProductVariant(this IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        endpoints.MapDelete(
                "/api/products/{productId:guid}/variants/{variantId:guid}",
                ExecuteAsync)
            .WithName("RemoveProductVariant");

        return endpoints;
    }

    public static async Task<Results<
        NoContent,
        NotFound<ProblemDetails>,
        BadRequest<ProblemDetails>,
        Conflict<ProblemDetails>>> ExecuteAsync(
        Guid productId,
        Guid variantId,
        [FromServices] UseCase useCase,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(useCase);

        try
        {
            var result = await useCase.ExecuteAsync(
                new RemoveProductVariantCommand(
                    ProductId.From(productId),
                    ProductVariantId.From(variantId)),
                cancellationToken);

            return result.Failure switch
            {
                RemoveProductVariantFailure.ProductNotFound =>
                    TypedResults.NotFound(new ProblemDetails
                    {
                        Title = "Product not found",
                        Detail = $"Product '{productId}' does not exist."
                    }),
                RemoveProductVariantFailure.VariantNotFound =>
                    TypedResults.NotFound(new ProblemDetails
                    {
                        Title = "Product variant not found",
                        Detail = $"Variant '{variantId}' does not exist on product '{productId}'."
                    }),
                RemoveProductVariantFailure.LastVariantCannotBeRemoved =>
                    TypedResults.Conflict(new ProblemDetails
                    {
                        Title = "Last variant cannot be removed",
                        Detail = "A product must contain at least one variant."
                    }),
                null => TypedResults.NoContent(),
                _ => throw new InvalidOperationException(
                    $"Remove variant failure '{result.Failure}' is not supported.")
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
                Title = "Invalid product variant",
                Detail = exception.Message
            });
        }
    }
}
