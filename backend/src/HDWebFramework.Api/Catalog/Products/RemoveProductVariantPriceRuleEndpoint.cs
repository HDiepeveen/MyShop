using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using HDWebFramework.Application.Catalog.Abstractions;
using HDWebFramework.Application.Catalog.RemoveProductVariantPriceRule;
using HDWebFramework.Domain.Catalog;
using UseCase = HDWebFramework.Application.Catalog.RemoveProductVariantPriceRule.RemoveProductVariantPriceRule;

namespace HDWebFramework.Api.Catalog.Products;

public static class RemoveProductVariantPriceRuleEndpoint
{
    public static IEndpointRouteBuilder MapRemoveProductVariantPriceRule(this IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        endpoints.MapDelete(
                "/api/products/{productId:guid}/variants/{variantId:guid}/price-rules/{priceRuleId:guid}",
                ExecuteAsync)
            .WithName("RemoveProductVariantPriceRule");

        return endpoints;
    }

    public static async Task<Results<
        NoContent,
        NotFound<ProblemDetails>,
        BadRequest<ProblemDetails>,
        Conflict<ProblemDetails>>> ExecuteAsync(
        Guid productId,
        Guid variantId,
        Guid priceRuleId,
        [FromServices] UseCase useCase,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(useCase);

        try
        {
            var result = await useCase.ExecuteAsync(
                new RemoveProductVariantPriceRuleCommand(
                    ProductId.From(productId),
                    ProductVariantId.From(variantId),
                    priceRuleId),
                cancellationToken);

            return result.Failure switch
            {
                RemoveProductVariantPriceRuleFailure.ProductNotFound =>
                    TypedResults.NotFound(new ProblemDetails
                    {
                        Title = "Product not found",
                        Detail = $"Product '{productId}' does not exist."
                    }),
                RemoveProductVariantPriceRuleFailure.VariantNotFound =>
                    TypedResults.NotFound(new ProblemDetails
                    {
                        Title = "Product variant not found",
                        Detail = $"Product variant '{variantId}' does not exist."
                    }),
                null => TypedResults.NoContent(),
                _ => throw new InvalidOperationException(
                    $"Remove variant price rule failure '{result.Failure}' is not supported.")
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
                Title = "Invalid product variant price rule removal",
                Detail = exception.Message
            });
        }
    }
}
