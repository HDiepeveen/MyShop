using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using MyShop.Application.Catalog.Abstractions;
using MyShop.Application.Catalog.RenameProductTypeAttribute;
using MyShop.Domain.Catalog;
using UseCase = MyShop.Application.Catalog.RenameProductTypeAttribute.RenameProductTypeAttribute;

namespace MyShop.Api.Catalog.ProductTypes;

public static class RenameProductTypeAttributeEndpoint
{
    public static IEndpointRouteBuilder MapRenameProductTypeAttribute(this IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);
        endpoints.MapPatch(
            "/api/product-types/{productTypeId:guid}/attributes/{attributeId:guid}/name", ExecuteAsync)
            .WithName("RenameProductTypeAttribute");
        return endpoints;
    }

    public static async Task<Results<NoContent, NotFound<ProblemDetails>, BadRequest<ProblemDetails>>>
        ExecuteAsync(Guid productTypeId, Guid attributeId, RenameProductTypeAttributeRequest request,
            [FromServices] UseCase useCase, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(useCase);
        try
        {
            var result = await useCase.ExecuteAsync(new(
                ProductTypeId.From(productTypeId), AttributeDefinitionId.From(attributeId), request.DisplayName),
                cancellationToken);
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
                Title = "Invalid attribute rename", Detail = exception.Message
            });
        }
    }
}

public sealed record RenameProductTypeAttributeRequest(string DisplayName);
