using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using MyShop.Application.Catalog.Abstractions;
using MyShop.Application.Catalog.ConfigureProductTypeAttribute;
using MyShop.Domain.Catalog;
using UseCase = MyShop.Application.Catalog.ConfigureProductTypeAttribute.ConfigureProductTypeAttribute;

namespace MyShop.Api.Catalog.ProductTypes;

public static class ConfigureProductTypeAttributeEndpoint
{
    public static IEndpointRouteBuilder MapConfigureProductTypeAttribute(this IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);
        endpoints.MapPut(
            "/api/product-types/{productTypeId:guid}/attributes/{attributeId:guid}/configuration",
            ExecuteAsync).WithName("ConfigureProductTypeAttribute");
        return endpoints;
    }

    public static async Task<Results<NoContent, NotFound<ProblemDetails>, BadRequest<ProblemDetails>>>
        ExecuteAsync(Guid productTypeId, Guid attributeId, ConfigureProductTypeAttributeRequest request,
            [FromServices] UseCase useCase, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(useCase);
        try
        {
            var result = await useCase.ExecuteAsync(new(
                ProductTypeId.From(productTypeId), AttributeDefinitionId.From(attributeId),
                request.IsRequired, request.IsFilterable), cancellationToken);
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
                Title = "Invalid attribute configuration", Detail = exception.Message
            });
        }
    }
}

public sealed record ConfigureProductTypeAttributeRequest(bool IsRequired, bool IsFilterable);
