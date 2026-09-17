using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using MyShop.Application.Catalog.Abstractions;
using MyShop.Application.Catalog.RemoveProductAttributeValue;
using MyShop.Domain.Catalog;
using UseCase = MyShop.Application.Catalog.RemoveProductAttributeValue.RemoveProductAttributeValue;

namespace MyShop.Api.Catalog.Products;

public static class RemoveProductAttributeValueEndpoint
{
    public static IEndpointRouteBuilder MapRemoveProductAttributeValue(this IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        endpoints.MapDelete(
                "/api/products/{productId:guid}/attributes/{attributeDefinitionId:guid}",
                ExecuteAsync)
            .WithName("RemoveProductAttributeValue");

        return endpoints;
    }

    public static async Task<Results<
        NoContent,
        NotFound<ProblemDetails>,
        BadRequest<ProblemDetails>,
        Conflict<ProblemDetails>>> ExecuteAsync(
        Guid productId,
        Guid attributeDefinitionId,
        [FromServices] UseCase useCase,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(useCase);

        try
        {
            var result = await useCase.ExecuteAsync(
                new RemoveProductAttributeValueCommand(
                    ProductId.From(productId),
                    AttributeDefinitionId.From(attributeDefinitionId)),
                cancellationToken);

            return result.Failure switch
            {
                RemoveProductAttributeValueFailure.ProductNotFound =>
                    TypedResults.NotFound(new ProblemDetails
                    {
                        Title = "Product not found",
                        Detail = $"Product '{productId}' does not exist."
                    }),
                null => TypedResults.NoContent(),
                _ => throw new InvalidOperationException(
                    $"Remove product attribute failure '{result.Failure}' is not supported.")
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
                Title = "Invalid product attribute removal",
                Detail = exception.Message
            });
        }
    }
}
