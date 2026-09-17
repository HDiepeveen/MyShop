using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using MyShop.Application.Catalog.Abstractions;
using MyShop.Application.Catalog.RemoveProductTypeAttribute;
using MyShop.Domain.Catalog;
using UseCase = MyShop.Application.Catalog.RemoveProductTypeAttribute.RemoveProductTypeAttribute;

namespace MyShop.Api.Catalog.ProductTypes;

public static class RemoveProductTypeAttributeEndpoint
{
    public static IEndpointRouteBuilder MapRemoveProductTypeAttribute(this IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);
        endpoints.MapDelete(
            "/api/product-types/{productTypeId:guid}/attributes/{attributeId:guid}", ExecuteAsync)
            .WithName("RemoveProductTypeAttribute");
        return endpoints;
    }

    public static async Task<Results<NoContent, NotFound<ProblemDetails>, BadRequest<ProblemDetails>>>
        ExecuteAsync(Guid productTypeId, Guid attributeId,
            [FromServices] UseCase useCase, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(useCase);
        try
        {
            var result = await useCase.ExecuteAsync(new(
                ProductTypeId.From(productTypeId), AttributeDefinitionId.From(attributeId)), cancellationToken);
            return result == ProductTypeAttributeUpdateResult.Succeeded
                ? TypedResults.NoContent()
                : TypedResults.NotFound(new ProblemDetails
                {
                    Title = result == ProductTypeAttributeUpdateResult.ProductTypeNotFound
                        ? "Product type not found" : "Attribute not found"
                });
        }
        catch (ArgumentException exception)
        {
            return TypedResults.BadRequest(new ProblemDetails
            {
                Title = "Invalid attribute removal", Detail = exception.Message
            });
        }
    }
}
