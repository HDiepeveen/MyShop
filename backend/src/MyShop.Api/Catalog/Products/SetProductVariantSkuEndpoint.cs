using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using MyShop.Application.Catalog.Abstractions;
using MyShop.Application.Catalog.SetProductVariantSku;
using MyShop.Domain.Catalog;
using UseCase = MyShop.Application.Catalog.SetProductVariantSku.SetProductVariantSku;

namespace MyShop.Api.Catalog.Products;

public static class SetProductVariantSkuEndpoint
{
    public static IEndpointRouteBuilder MapSetProductVariantSku(this IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        endpoints.MapPut(
                "/api/products/{productId:guid}/variants/{variantId:guid}/sku",
                ExecuteAsync)
            .WithName("SetProductVariantSku");

        return endpoints;
    }

    public static async Task<Results<
        NoContent,
        NotFound<ProblemDetails>,
        BadRequest<ProblemDetails>,
        Conflict<ProblemDetails>>> ExecuteAsync(
        Guid productId,
        Guid variantId,
        SetProductVariantSkuRequest request,
        [FromServices] UseCase useCase,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(useCase);

        try
        {
            var result = await useCase.ExecuteAsync(
                new SetProductVariantSkuCommand(
                    ProductId.From(productId),
                    ProductVariantId.From(variantId),
                    request.Sku),
                cancellationToken);

            return result.Failure switch
            {
                SetProductVariantSkuFailure.ProductNotFound =>
                    TypedResults.NotFound(new ProblemDetails
                    {
                        Title = "Product not found",
                        Detail = $"Product '{productId}' does not exist."
                    }),
                SetProductVariantSkuFailure.VariantNotFound =>
                    TypedResults.NotFound(new ProblemDetails
                    {
                        Title = "Product variant not found",
                        Detail = $"Variant '{variantId}' does not exist on product '{productId}'."
                    }),
                SetProductVariantSkuFailure.SkuAlreadyInUse =>
                    TypedResults.Conflict(new ProblemDetails
                    {
                        Title = "SKU already in use",
                        Detail = $"SKU '{request.Sku}' is already assigned to another variant."
                    }),
                null => TypedResults.NoContent(),
                _ => throw new InvalidOperationException(
                    $"Set SKU failure '{result.Failure}' is not supported.")
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
                Title = "Invalid product variant SKU",
                Detail = exception.Message
            });
        }
    }
}

public sealed record SetProductVariantSkuRequest(string Sku);
