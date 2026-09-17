using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using MyShop.Application.Catalog.GetProductType;
using MyShop.Domain.Catalog;
using UseCase = MyShop.Application.Catalog.GetProductType.GetProductType;

namespace MyShop.Api.Catalog.ProductTypes;

public static class GetProductTypeEndpoint
{
    public static IEndpointRouteBuilder MapGetProductType(this IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        endpoints.MapGet("/api/product-types/{productTypeId:guid}", ExecuteAsync)
            .WithName("GetProductType");

        return endpoints;
    }

    public static async Task<Results<
        Ok<GetProductTypeResponse>,
        NotFound<ProblemDetails>,
        BadRequest<ProblemDetails>>> ExecuteAsync(
        Guid productTypeId,
        [FromServices] UseCase useCase,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(useCase);

        try
        {
            var result = await useCase.ExecuteAsync(
                new GetProductTypeQuery(ProductTypeId.From(productTypeId)),
                cancellationToken);

            if (result.Failure == GetProductTypeFailure.ProductTypeNotFound)
                return TypedResults.NotFound(new ProblemDetails
                {
                    Title = "Product type not found",
                    Detail = $"Product type '{productTypeId}' does not exist."
                });
            if (result.Failure is not null)
                throw new InvalidOperationException(
                    $"Get product type failure '{result.Failure}' is not supported.");

            var productType = result.ProductType
                ?? throw new InvalidOperationException(
                    "A successful get result must contain a product type.");

            return TypedResults.Ok(new GetProductTypeResponse(
                productType.Id.Value,
                productType.Name,
                productType.AttributeDefinitions.Select(definition =>
                    new AttributeDefinitionResponse(
                        definition.Id.Value,
                        definition.Code.Value,
                        definition.DisplayName,
                        definition.DataType.ToString(),
                        definition.Scope.ToString(),
                        definition.IsRequired,
                        definition.IsFilterable)).ToArray()));
        }
        catch (ArgumentException exception)
        {
            return TypedResults.BadRequest(new ProblemDetails
            {
                Title = "Invalid product type ID",
                Detail = exception.Message
            });
        }
    }
}

public sealed record GetProductTypeResponse(
    Guid Id,
    string Name,
    IReadOnlyList<AttributeDefinitionResponse> AttributeDefinitions);

public sealed record AttributeDefinitionResponse(
    Guid Id,
    string Code,
    string DisplayName,
    string DataType,
    string Scope,
    bool IsRequired,
    bool IsFilterable);
