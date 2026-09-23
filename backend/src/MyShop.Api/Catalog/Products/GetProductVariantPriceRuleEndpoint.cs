using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using MyShop.Application.Catalog.GetProductVariantPriceRule;
using MyShop.Domain.Catalog;
using UseCase = MyShop.Application.Catalog.GetProductVariantPriceRule.GetProductVariantPriceRule;

namespace MyShop.Api.Catalog.Products;

public static class GetProductVariantPriceRuleEndpoint
{
    public static IEndpointRouteBuilder MapGetProductVariantPriceRule(this IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        endpoints.MapGet(
                "/api/products/{productId:guid}/variants/{variantId:guid}/price-rules/{priceRuleId:guid}",
                ExecuteAsync)
            .WithName("GetProductVariantPriceRule");

        return endpoints;
    }

    public static async Task<Results<
        Ok<PriceRuleResponse>,
        NotFound<ProblemDetails>,
        BadRequest<ProblemDetails>>> ExecuteAsync(
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
                new GetProductVariantPriceRuleQuery(
                    ProductId.From(productId),
                    ProductVariantId.From(variantId),
                    priceRuleId),
                cancellationToken);

            return result.Failure switch
            {
                GetProductVariantPriceRuleFailure.ProductNotFound =>
                    TypedResults.NotFound(new ProblemDetails
                    {
                        Title = "Product not found",
                        Detail = $"Product '{productId}' does not exist."
                    }),
                GetProductVariantPriceRuleFailure.VariantNotFound =>
                    TypedResults.NotFound(new ProblemDetails
                    {
                        Title = "Product variant not found",
                        Detail = $"Product variant '{variantId}' does not exist."
                    }),
                GetProductVariantPriceRuleFailure.PriceRuleNotFound =>
                    TypedResults.NotFound(new ProblemDetails
                    {
                        Title = "Price rule not found",
                        Detail = $"Price rule '{priceRuleId}' does not exist on variant '{variantId}'."
                    }),
                null => TypedResults.Ok(PriceRuleResponse.FromDomain(
                    result.Snapshot?.Rule ?? throw new InvalidOperationException("A successful result must contain a price rule."))),
                _ => throw new InvalidOperationException(
                    $"Get variant price rule failure '{result.Failure}' is not supported.")
            };
        }
        catch (ArgumentException exception)
        {
            return TypedResults.BadRequest(new ProblemDetails
            {
                Title = "Invalid product variant price rule query",
                Detail = exception.Message
            });
        }
    }
}
