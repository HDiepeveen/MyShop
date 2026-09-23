using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using MyShop.Application.Catalog.GetProductTypeAttribute;
using MyShop.Domain.Catalog;
using UseCase = MyShop.Application.Catalog.GetProductTypeAttribute.GetProductTypeAttribute;

namespace MyShop.Api.Catalog.ProductTypes;

public static class GetProductTypeAttributeEndpoint
{
    public static IEndpointRouteBuilder MapGetProductTypeAttribute(this IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);
        endpoints.MapGet("/api/product-types/{productTypeId:guid}/attributes/{attributeId:guid}", ExecuteAsync)
            .WithName("GetProductTypeAttribute");
        return endpoints;
    }

    public static async Task<Results<Ok<AttributeDefinitionResponse>, NotFound<ProblemDetails>, BadRequest<ProblemDetails>>>
        ExecuteAsync(Guid productTypeId, Guid attributeId, [FromServices] UseCase useCase, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(useCase);
        try
        {
            var result = await useCase.ExecuteAsync(
                new(ProductTypeId.From(productTypeId), AttributeDefinitionId.From(attributeId)), cancellationToken);
            if (result.Failure is not null)
                return result.Failure switch
                {
                    GetProductTypeAttributeFailure.ProductTypeNotFound => TypedResults.NotFound(
                        new ProblemDetails { Title = "Product type not found" }),
                    GetProductTypeAttributeFailure.AttributeNotFound => TypedResults.NotFound(
                        new ProblemDetails { Title = "Attribute not found" }),
                    _ => throw new InvalidOperationException("Unsupported get product type attribute failure.")
                };
            var attribute = result.Attribute
                ?? throw new InvalidOperationException("A successful get result must contain an attribute.");
            return TypedResults.Ok(new AttributeDefinitionResponse(
                attribute.Id.Value, attribute.Code.Value, attribute.DisplayName,
                attribute.DataType.ToString(), attribute.Scope.ToString(), attribute.IsRequired, attribute.IsFilterable));
        }
        catch (ArgumentException exception)
        {
            return TypedResults.BadRequest(new ProblemDetails
            {
                Title = "Invalid product type attribute ID", Detail = exception.Message
            });
        }
    }
}
