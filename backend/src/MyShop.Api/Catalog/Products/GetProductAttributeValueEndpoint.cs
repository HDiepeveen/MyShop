using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using MyShop.Application.Catalog.GetProductAttributeValue;
using MyShop.Domain.Catalog;
using UseCase = MyShop.Application.Catalog.GetProductAttributeValue.GetProductAttributeValue;

namespace MyShop.Api.Catalog.Products;

public static class GetProductAttributeValueEndpoint
{
    public static IEndpointRouteBuilder MapGetProductAttributeValue(this IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        endpoints.MapGet(
                "/api/products/{productId:guid}/attributes/{attributeDefinitionId:guid}",
                ExecuteAsync)
            .WithName("GetProductAttributeValue");

        return endpoints;
    }

    private static AttributeValueDetailsResponse Map(GetProductAttributeValueResult result)
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
        Guid attributeDefinitionId,
        [FromServices] UseCase useCase,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(useCase);

        try
        {
            var result = await useCase.ExecuteAsync(
                new GetProductAttributeValueQuery(
                    ProductId.From(productId),
                    AttributeDefinitionId.From(attributeDefinitionId)),
                cancellationToken);

            return result.Failure switch
            {
                GetProductAttributeValueFailure.ProductNotFound =>
                    TypedResults.NotFound(new ProblemDetails
                    {
                        Title = "Product not found",
                        Detail = $"Product '{productId}' does not exist."
                    }),
                GetProductAttributeValueFailure.AttributeValueNotFound =>
                    TypedResults.NotFound(new ProblemDetails
                    {
                        Title = "Attribute value not found",
                        Detail = $"Attribute value '{attributeDefinitionId}' does not exist on this product."
                    }),
                null => TypedResults.Ok(Map(result)),
                _ => throw new InvalidOperationException(
                    $"Get product attribute query failure '{result.Failure}' is not supported.")
            };
        }
        catch (ArgumentException exception)
        {
            return TypedResults.BadRequest(new ProblemDetails
            {
                Title = "Invalid product attribute query",
                Detail = exception.Message
            });
        }
    }
}

public sealed record AttributeValueDetailsResponse(AttributeValueResponse Attribute, Guid Revision);
