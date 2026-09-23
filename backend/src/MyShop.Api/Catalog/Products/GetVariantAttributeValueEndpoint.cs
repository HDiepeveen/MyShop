using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using MyShop.Application.Catalog.GetVariantAttributeValue;
using MyShop.Domain.Catalog;
using UseCase = MyShop.Application.Catalog.GetVariantAttributeValue.GetVariantAttributeValue;

namespace MyShop.Api.Catalog.Products;

public static class GetVariantAttributeValueEndpoint
{
    public static IEndpointRouteBuilder MapGetVariantAttributeValue(this IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        endpoints.MapGet(
                "/api/products/{productId:guid}/variants/{variantId:guid}/attributes/{attributeDefinitionId:guid}",
                ExecuteAsync)
            .WithName("GetVariantAttributeValue");

        return endpoints;
    }

    private static AttributeValueDetailsResponse Map(GetVariantAttributeValueResult result)
    {
        var snapshot = result.Snapshot
            ?? throw new InvalidOperationException("A successful result must contain an attribute value.");
        return new(AttributeValueResponse.FromDomain(snapshot.Value), snapshot.Revision);
    }

    public static async Task<Results<
        Ok<AttributeValueDetailsResponse>,
        NotFound<ProblemDetails>,
        BadRequest<ProblemDetails>>> ExecuteAsync(
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
                new GetVariantAttributeValueQuery(
                    ProductId.From(productId),
                    ProductVariantId.From(variantId),
                    AttributeDefinitionId.From(attributeDefinitionId)),
                cancellationToken);

            return result.Failure switch
            {
                GetVariantAttributeValueFailure.ProductNotFound =>
                    TypedResults.NotFound(new ProblemDetails
                    {
                        Title = "Product not found",
                        Detail = $"Product '{productId}' does not exist."
                    }),
                GetVariantAttributeValueFailure.VariantNotFound =>
                    TypedResults.NotFound(new ProblemDetails
                    {
                        Title = "Product variant not found",
                        Detail = $"Product variant '{variantId}' does not exist on product '{productId}'."
                    }),
                GetVariantAttributeValueFailure.AttributeValueNotFound =>
                    TypedResults.NotFound(new ProblemDetails
                    {
                        Title = "Attribute value not found",
                        Detail = $"Attribute value '{attributeDefinitionId}' does not exist on this variant."
                    }),
                null => TypedResults.Ok(Map(result)),
                _ => throw new InvalidOperationException(
                    $"Get variant attribute query failure '{result.Failure}' is not supported.")
            };
        }
        catch (ArgumentException exception)
        {
            return TypedResults.BadRequest(new ProblemDetails
            {
                Title = "Invalid variant attribute query",
                Detail = exception.Message
            });
        }
    }
}
