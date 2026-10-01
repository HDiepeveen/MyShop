using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using MyShop.Application.Catalog.Abstractions;
using MyShop.Application.Catalog.SetProductVariantStock;
using MyShop.Domain.Catalog;
using UseCase = MyShop.Application.Catalog.SetProductVariantStock.SetProductVariantStock;

namespace MyShop.Api.Catalog.Products;

public static class SetProductVariantStockEndpoint
{
    public static IEndpointRouteBuilder MapSetProductVariantStock(this IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);
        endpoints.MapPut("/api/products/{productId:guid}/variants/{variantId:guid}/stock", ExecuteAsync)
            .WithName("SetProductVariantStock");
        return endpoints;
    }

    public static async Task<Results<NoContent, NotFound<ProblemDetails>,
        BadRequest<ProblemDetails>, Conflict<ProblemDetails>>> ExecuteAsync(
        Guid productId, Guid variantId, SetProductVariantStockRequest request,
        [FromServices] UseCase useCase, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(useCase);

        try
        {
            var result = await useCase.ExecuteAsync(
                new SetProductVariantStockCommand(ProductId.From(productId),
                    ProductVariantId.From(variantId), request.Quantity), cancellationToken);
            return result.Failure switch
            {
                SetProductVariantStockFailure.ProductNotFound => ProductNotFound(productId),
                SetProductVariantStockFailure.VariantNotFound => VariantNotFound(productId, variantId),
                null => TypedResults.NoContent(),
                _ => throw new InvalidOperationException(
                    $"Set stock failure '{result.Failure}' is not supported.")
            };
        }
        catch (ProductConcurrencyException exception)
        {
            return TypedResults.Conflict(new ProblemDetails
                { Title = "Product was modified", Detail = exception.Message });
        }
        catch (ArgumentException exception)
        {
            return TypedResults.BadRequest(new ProblemDetails
                { Title = "Invalid product variant stock", Detail = exception.Message });
        }
    }

    private static NotFound<ProblemDetails> ProductNotFound(Guid productId) =>
        TypedResults.NotFound(new ProblemDetails
            { Title = "Product not found", Detail = $"Product '{productId}' does not exist." });

    private static NotFound<ProblemDetails> VariantNotFound(Guid productId, Guid variantId) =>
        TypedResults.NotFound(new ProblemDetails
        {
            Title = "Product variant not found",
            Detail = $"Variant '{variantId}' does not exist on product '{productId}'."
        });
}

public sealed record SetProductVariantStockRequest(int Quantity);
