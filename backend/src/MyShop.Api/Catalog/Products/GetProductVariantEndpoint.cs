using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using MyShop.Application.Catalog.GetProductVariant;
using MyShop.Domain.Catalog;
using UseCase = MyShop.Application.Catalog.GetProductVariant.GetProductVariant;

namespace MyShop.Api.Catalog.Products;

public static class GetProductVariantEndpoint
{
    public static IEndpointRouteBuilder MapGetProductVariant(this IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        endpoints.MapGet(
                "/api/products/{productId:guid}/variants/{variantId:guid}",
                ExecuteAsync)
            .WithName("GetProductVariant");

        return endpoints;
    }

    private static GetProductVariantResponse Map(GetProductVariantResult result)
    {
        var snapshot = result.Snapshot
            ?? throw new InvalidOperationException("A successful result must contain a product variant.");
        return new(ProductVariantResponse.FromDomain(snapshot.Variant), snapshot.Revision);
    }

    public static async Task<Results<
        Ok<GetProductVariantResponse>,
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
                new GetProductVariantQuery(
                    ProductId.From(productId),
                    ProductVariantId.From(variantId)),
                cancellationToken);

            return result.Failure switch
            {
                GetProductVariantFailure.ProductNotFound =>
                    TypedResults.NotFound(new ProblemDetails
                    {
                        Title = "Product not found",
                        Detail = $"Product '{productId}' does not exist."
                    }),
                GetProductVariantFailure.VariantNotFound =>
                    TypedResults.NotFound(new ProblemDetails
                    {
                        Title = "Product variant not found",
                        Detail = $"Product variant '{variantId}' does not exist."
                    }),
                null => TypedResults.Ok(Map(result)),
                _ => throw new InvalidOperationException(
                    $"Get variant query failure '{result.Failure}' is not supported.")
            };
        }
        catch (ArgumentException exception)
        {
            return TypedResults.BadRequest(new ProblemDetails
            {
                Title = "Invalid product variant query",
                Detail = exception.Message
            });
        }
    }
}

public sealed record GetProductVariantResponse(ProductVariantResponse Variant, Guid Revision);
