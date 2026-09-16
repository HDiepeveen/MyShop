using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using MyShop.Application.Catalog.Abstractions;
using MyShop.Application.Catalog.RenameProductVariant;
using MyShop.Domain.Catalog;
using UseCase = MyShop.Application.Catalog.RenameProductVariant.RenameProductVariant;

namespace MyShop.Api.Catalog.Products;

public static class RenameProductVariantEndpoint
{
    public static IEndpointRouteBuilder MapRenameProductVariant(this IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        endpoints.MapPatch(
                "/api/products/{productId:guid}/variants/{variantId:guid}/name",
                ExecuteAsync)
            .WithName("RenameProductVariant");

        return endpoints;
    }

    public static async Task<Results<
        NoContent,
        NotFound<ProblemDetails>,
        BadRequest<ProblemDetails>,
        Conflict<ProblemDetails>>> ExecuteAsync(
        Guid productId,
        Guid variantId,
        RenameProductVariantRequest request,
        [FromServices] UseCase useCase,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(useCase);

        try
        {
            var result = await useCase.ExecuteAsync(
                new RenameProductVariantCommand(
                    ProductId.From(productId),
                    ProductVariantId.From(variantId),
                    request.Name),
                cancellationToken);

            if (result.Failure is not null)
                return TypedResults.NotFound(new ProblemDetails
                {
                    Title = result.Failure == RenameProductVariantFailure.ProductNotFound
                        ? "Product not found"
                        : "Product variant not found",
                    Detail = result.Failure == RenameProductVariantFailure.ProductNotFound
                        ? $"Product '{productId}' does not exist."
                        : $"Variant '{variantId}' does not exist on product '{productId}'."
                });

            return TypedResults.NoContent();
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
                Title = "Invalid product variant rename",
                Detail = exception.Message
            });
        }
    }
}

public sealed record RenameProductVariantRequest(string Name);
