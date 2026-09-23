using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using MyShop.Application.Catalog.GetProductVariantPrice;
using MyShop.Domain.Catalog;
using UseCase = MyShop.Application.Catalog.GetProductVariantPrice.GetProductVariantPrice;

namespace MyShop.Api.Catalog.Products;

public static class GetProductVariantPriceEndpoint
{
    public static IEndpointRouteBuilder MapGetProductVariantPrice(this IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);
        endpoints.MapGet("/api/products/{productId:guid}/variants/{variantId:guid}/price", ExecuteAsync)
            .WithName("GetProductVariantPrice");
        return endpoints;
    }

    public static async Task<Results<Ok<ProductVariantPriceResponse>, NotFound<ProblemDetails>,
        BadRequest<ProblemDetails>, Conflict<ProblemDetails>>> ExecuteAsync(
        Guid productId, Guid variantId, [FromQuery] DateTimeOffset at,
        [FromServices] UseCase useCase, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(useCase);
        try
        {
            var result = await useCase.ExecuteAsync(
                new GetProductVariantPriceQuery(ProductId.From(productId), ProductVariantId.From(variantId), at),
                cancellationToken);
            if (result.Failure == GetProductVariantPriceFailure.ProductNotFound)
                return TypedResults.NotFound(new ProblemDetails { Title = "Product not found" });
            if (result.Failure == GetProductVariantPriceFailure.VariantNotFound)
                return TypedResults.NotFound(new ProblemDetails { Title = "Product variant not found" });
            if (result.Failure == GetProductVariantPriceFailure.PriceNotSet)
                return TypedResults.Conflict(new ProblemDetails { Title = "Variant base price is not set" });

            var quote = result.Quote ?? throw new InvalidOperationException("A successful price result must contain a quote.");
            return TypedResults.Ok(new ProductVariantPriceResponse(
                quote.BasePrice.Amount, quote.Price.Amount, quote.Price.Currency, quote.At, quote.Revision, quote.AppliedPriceRuleId));
        }
        catch (ArgumentException exception)
        {
            return TypedResults.BadRequest(new ProblemDetails { Title = "Invalid variant price query", Detail = exception.Message });
        }
    }
}

public sealed record ProductVariantPriceResponse(
    decimal BaseAmount, decimal Amount, string Currency, DateTimeOffset At, Guid Revision, Guid? AppliedPriceRuleId = null);
