using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using MyShop.Application.Catalog.ListProductVariantPriceRules;
using MyShop.Domain.Catalog;
using UseCase = MyShop.Application.Catalog.ListProductVariantPriceRules.ListProductVariantPriceRules;

namespace MyShop.Api.Catalog.Products;

public static class ListProductVariantPriceRulesEndpoint
{
    public static IEndpointRouteBuilder MapListProductVariantPriceRules(this IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        endpoints.MapGet(
                "/api/products/{productId:guid}/variants/{variantId:guid}/price-rules",
                ExecuteAsync)
            .WithName("ListProductVariantPriceRules");

        return endpoints;
    }

    private static ProductVariantPriceRulesResponse Map(ListProductVariantPriceRulesResult result)
    {
        var snapshot = result.Snapshot
            ?? throw new InvalidOperationException("A successful result must contain a price rule list.");
        return new(snapshot.Rules.Select(PriceRuleResponse.FromDomain).ToArray(), snapshot.Revision);
    }

    public static async Task<Results<
        Ok<ProductVariantPriceRulesResponse>,
        NotFound<ProblemDetails>,
        BadRequest<ProblemDetails>>> ExecuteAsync(
        Guid productId,
        Guid variantId,
        [FromServices] UseCase useCase,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(useCase);

        try
        {
            var result = await useCase.ExecuteAsync(
                new ListProductVariantPriceRulesQuery(
                    ProductId.From(productId),
                    ProductVariantId.From(variantId)),
                cancellationToken);

            return result.Failure switch
            {
                ListProductVariantPriceRulesFailure.ProductNotFound =>
                    TypedResults.NotFound(new ProblemDetails
                    {
                        Title = "Product not found",
                        Detail = $"Product '{productId}' does not exist."
                    }),
                ListProductVariantPriceRulesFailure.VariantNotFound =>
                    TypedResults.NotFound(new ProblemDetails
                    {
                        Title = "Product variant not found",
                        Detail = $"Product variant '{variantId}' does not exist."
                    }),
                null => TypedResults.Ok(Map(result)),
                _ => throw new InvalidOperationException(
                    $"List variant price rules failure '{result.Failure}' is not supported.")
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

public sealed record ProductVariantPriceRulesResponse(IReadOnlyList<PriceRuleResponse> Rules, Guid Revision);
