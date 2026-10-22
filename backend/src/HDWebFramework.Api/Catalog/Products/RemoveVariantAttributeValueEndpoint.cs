using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using HDWebFramework.Application.Catalog.Abstractions;
using HDWebFramework.Application.Catalog.RemoveVariantAttributeValue;
using HDWebFramework.Domain.Catalog;
using UseCase = HDWebFramework.Application.Catalog.RemoveVariantAttributeValue.RemoveVariantAttributeValue;

namespace HDWebFramework.Api.Catalog.Products;

public static class RemoveVariantAttributeValueEndpoint
{
    public static IEndpointRouteBuilder MapRemoveVariantAttributeValue(this IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        endpoints.MapDelete(
                "/api/products/{productId:guid}/variants/{variantId:guid}/attributes/{attributeDefinitionId:guid}",
                ExecuteAsync)
            .WithName("RemoveVariantAttributeValue");

        return endpoints;
    }

    public static async Task<Results<
        NoContent,
        NotFound<ProblemDetails>,
        BadRequest<ProblemDetails>,
        Conflict<ProblemDetails>>> ExecuteAsync(
        Guid productId,
        Guid variantId,
        Guid attributeDefinitionId,
        [FromServices] UseCase useCase,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(useCase);

        try
        {
            var result = await useCase.ExecuteAsync(
                new RemoveVariantAttributeValueCommand(
                    ProductId.From(productId),
                    ProductVariantId.From(variantId),
                    AttributeDefinitionId.From(attributeDefinitionId)),
                cancellationToken);

            return result.Failure switch
            {
                RemoveVariantAttributeValueFailure.ProductNotFound =>
                    TypedResults.NotFound(new ProblemDetails
                    {
                        Title = "Product not found",
                        Detail = $"Product '{productId}' does not exist."
                    }),
                RemoveVariantAttributeValueFailure.VariantNotFound =>
                    TypedResults.NotFound(new ProblemDetails
                    {
                        Title = "Product variant not found",
                        Detail = $"Product variant '{variantId}' does not exist."
                    }),
                null => TypedResults.NoContent(),
                _ => throw new InvalidOperationException(
                    $"Remove variant attribute failure '{result.Failure}' is not supported.")
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
                Title = "Invalid product variant attribute removal",
                Detail = exception.Message
            });
        }
    }
}
