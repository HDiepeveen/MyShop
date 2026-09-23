using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using MyShop.Application.Catalog.Abstractions;
using MyShop.Application.Catalog.AddProductVariantPriceRule;
using MyShop.Domain.Catalog;
using UseCase = MyShop.Application.Catalog.AddProductVariantPriceRule.AddProductVariantPriceRule;

namespace MyShop.Api.Catalog.Products;

public static class AddProductVariantPriceRuleEndpoint
{
    public static IEndpointRouteBuilder MapAddProductVariantPriceRule(this IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        endpoints.MapPost(
                "/api/products/{productId:guid}/variants/{variantId:guid}/price-rules",
                ExecuteAsync)
            .WithName("AddProductVariantPriceRule");

        return endpoints;
    }

    public static async Task<Results<
        Created<AddProductVariantPriceRuleResponse>,
        NotFound<ProblemDetails>,
        BadRequest<ProblemDetails>,
        Conflict<ProblemDetails>>> ExecuteAsync(
        Guid productId,
        Guid variantId,
        AddProductVariantPriceRuleRequest request,
        [FromServices] UseCase useCase,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(useCase);

        try
        {
            var result = await useCase.ExecuteAsync(
                new AddProductVariantPriceRuleCommand(
                    ProductId.From(productId),
                    ProductVariantId.From(variantId),
                    request.Name,
                    (PriceAdjustmentType)request.AdjustmentType,
                    request.Value,
                    request.Priority,
                    request.StartsAt,
                    request.EndsAt),
                cancellationToken);

            return result.Failure switch
            {
                AddProductVariantPriceRuleFailure.ProductNotFound =>
                    TypedResults.NotFound(new ProblemDetails
                    {
                        Title = "Product not found",
                        Detail = $"Product '{productId}' does not exist."
                    }),
                AddProductVariantPriceRuleFailure.VariantNotFound =>
                    TypedResults.NotFound(new ProblemDetails
                    {
                        Title = "Product variant not found",
                        Detail = $"Variant '{variantId}' does not exist on product '{productId}'."
                    }),
                null => TypedResults.Created(
                    $"/api/products/{productId}/variants/{variantId}/price-rules/{result.PriceRuleId}",
                    new AddProductVariantPriceRuleResponse(result.PriceRuleId
                        ?? throw new InvalidOperationException("A successful add result must contain a price rule ID."))),
                _ => throw new InvalidOperationException(
                    $"Add price rule failure '{result.Failure}' is not supported.")
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

public sealed record AddProductVariantPriceRuleRequest(
    string Name,
    int AdjustmentType,
    decimal Value,
    int Priority,
    DateTimeOffset? StartsAt = null,
    DateTimeOffset? EndsAt = null);

public sealed record AddProductVariantPriceRuleResponse(Guid Id);
