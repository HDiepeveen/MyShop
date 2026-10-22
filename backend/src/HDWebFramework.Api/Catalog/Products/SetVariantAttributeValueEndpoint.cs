using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using HDWebFramework.Application.Catalog.Abstractions;
using HDWebFramework.Application.Catalog.SetVariantAttributeValue;
using HDWebFramework.Domain.Catalog;
using UseCase = HDWebFramework.Application.Catalog.SetVariantAttributeValue.SetVariantAttributeValue;

namespace HDWebFramework.Api.Catalog.Products;

public static class SetVariantAttributeValueEndpoint
{
    public static IEndpointRouteBuilder MapSetVariantAttributeValue(this IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        endpoints.MapPut(
                "/api/products/{productId:guid}/variants/{variantId:guid}/attributes/{attributeDefinitionId:guid}",
                ExecuteAsync)
            .WithName("SetVariantAttributeValue");

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
        AttributeValueRequest request,
        [FromServices] UseCase useCase,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(useCase);

        try
        {
            var result = await useCase.ExecuteAsync(
                new SetVariantAttributeValueCommand(
                    ProductId.From(productId),
                    ProductVariantId.From(variantId),
                    AttributeDefinitionId.From(attributeDefinitionId),
                    AttributeValueRequestMapper.Map(request)),
                cancellationToken);

            return result.Failure switch
            {
                SetVariantAttributeValueFailure.ProductNotFound => NotFound(
                    "Product not found", $"Product '{productId}' does not exist."),
                SetVariantAttributeValueFailure.VariantNotFound => NotFound(
                    "Product variant not found", $"Product variant '{variantId}' does not exist."),
                SetVariantAttributeValueFailure.ProductTypeNotFound => NotFound(
                    "Product type not found", "The product type does not exist."),
                SetVariantAttributeValueFailure.AttributeDefinitionNotFound => NotFound(
                    "Attribute definition not found",
                    $"Attribute definition '{attributeDefinitionId}' does not exist on the product type."),
                SetVariantAttributeValueFailure.WrongAttributeScope => Conflict(
                    "Wrong attribute scope", "The attribute definition is not scoped to variants."),
                SetVariantAttributeValueFailure.WrongAttributeDataType => Conflict(
                    "Wrong attribute data type",
                    $"The attribute definition does not accept '{request.DataType}' values."),
                null => TypedResults.NoContent(),
                _ => throw new InvalidOperationException(
                    $"Set variant attribute failure '{result.Failure}' is not supported.")
            };
        }
        catch (ProductConcurrencyException exception)
        {
            return Conflict("Product was modified", exception.Message);
        }
        catch (ArgumentException exception)
        {
            return TypedResults.BadRequest(new ProblemDetails
            {
                Title = "Invalid product variant attribute value",
                Detail = exception.Message
            });
        }
    }

    private static NotFound<ProblemDetails> NotFound(string title, string detail) =>
        TypedResults.NotFound(new ProblemDetails { Title = title, Detail = detail });

    private static Conflict<ProblemDetails> Conflict(string title, string detail) =>
        TypedResults.Conflict(new ProblemDetails { Title = title, Detail = detail });
}
