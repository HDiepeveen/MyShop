using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using MyShop.Application.Catalog.GetProductTypeUsage;
using MyShop.Domain.Catalog;
using UseCase = MyShop.Application.Catalog.GetProductTypeUsage.GetProductTypeUsage;

namespace MyShop.Api.Catalog.ProductTypes;

public static class GetProductTypeUsageEndpoint
{
    public static IEndpointRouteBuilder MapGetProductTypeUsage(this IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);
        endpoints.MapGet("/api/product-types/{productTypeId:guid}/usage", ExecuteAsync).WithName("GetProductTypeUsage");
        return endpoints;
    }

    public static async Task<Results<Ok<ProductTypeUsageResponse>, NotFound<ProblemDetails>, BadRequest<ProblemDetails>>>
        ExecuteAsync(Guid productTypeId, [FromServices] UseCase useCase, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(useCase);
        try
        {
            var result = await useCase.ExecuteAsync(new(ProductTypeId.From(productTypeId)), cancellationToken);
            if (result.Failure == GetProductTypeUsageFailure.ProductTypeNotFound)
                return TypedResults.NotFound(new ProblemDetails { Title = "Product type not found" });
            if (result.Failure is not null)
                throw new InvalidOperationException("Unsupported product type usage failure.");
            var value = result.ProductCount
                ?? throw new InvalidOperationException("A successful usage result must contain usage data.");
            return TypedResults.Ok(new ProductTypeUsageResponse(productTypeId, value, value > 0));
        }
        catch (ArgumentException exception)
        {
            return TypedResults.BadRequest(new ProblemDetails
            {
                Title = "Invalid product type ID", Detail = exception.Message
            });
        }
    }
}

public sealed record ProductTypeUsageResponse(Guid ProductTypeId, int ProductCount, bool IsInUse);
