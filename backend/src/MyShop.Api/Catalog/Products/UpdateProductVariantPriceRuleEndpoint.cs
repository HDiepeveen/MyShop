using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using MyShop.Application.Catalog.Abstractions;
using MyShop.Application.Catalog.UpdateProductVariantPriceRule;
using MyShop.Domain.Catalog;
using UseCase = MyShop.Application.Catalog.UpdateProductVariantPriceRule.UpdateProductVariantPriceRule;

namespace MyShop.Api.Catalog.Products;

public static class UpdateProductVariantPriceRuleEndpoint
{
    public static IEndpointRouteBuilder MapUpdateProductVariantPriceRule(this IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        endpoints.MapPut(
                "/api/products/{productId:guid}/variants/{variantId:guid}/price-rules/{priceRuleId:guid}",
                ExecuteAsync)
            .WithName("UpdateProductVariantPriceRule");

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
        UpdateProductVariantPriceRuleRequest request,
        [FromServices] UseCase useCase,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(useCase);

        try
        {
            var result = await useCase.ExecuteAsync(
                new UpdateProductVariantPriceRuleCommand(
                    ProductId.From(productId),
                    ProductVariantId.From(variantId),
                    priceRuleId,
                    request.Name,
                    (PriceAdjustmentType)request.AdjustmentType,
                    request.Value,
                    request.Priority,
                    request.StartsAt,
                    request.EndsAt),
                cancellationToken);

            return result.Failure switch
            {
                UpdateProductVariantPriceRuleFailure.ProductNotFound =>
                    TypedResults.NotFound(new ProblemDetails
                    {
                        Title = "Product not found",
                        Detail = $"Product '{productId}' does not exist."
                    }),
                UpdateProductVariantPriceRuleFailure.VariantNotFound =>
                    TypedResults.NotFound(new ProblemDetails
                    {
                        Title = "Product variant not found",
                        Detail = $"Variant '{variantId}' does not exist on product '{productId}'."
                    }),
                UpdateProductVariantPriceRuleFailure.PriceRuleNotFound =>
                    TypedResults.NotFound(new ProblemDetails
                    {
                        Title = "Price rule not found",
                        Detail = $"Price rule '{priceRuleId}' does not exist on variant '{variantId}'."
                    }),
                null => TypedResults.NoContent(),
                _ => throw new InvalidOperationException(
                    $"Update price rule failure '{result.Failure}' is not supported.")
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
                Title = "Invalid product variant price rule",
                Detail = exception.Message
            });
        }
    }
}

public sealed record UpdateProductVariantPriceRuleRequest(
    string Name,
    int AdjustmentType,
    decimal Value,
    int Priority,
    DateTimeOffset? StartsAt = null,
    DateTimeOffset? EndsAt = null);
