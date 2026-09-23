using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using MyShop.Application.Catalog.GetCategoryUsage;
using MyShop.Domain.Catalog;
using UseCase = MyShop.Application.Catalog.GetCategoryUsage.GetCategoryUsage;

namespace MyShop.Api.Catalog.Categories;

public static class GetCategoryUsageEndpoint
{
    public static IEndpointRouteBuilder MapGetCategoryUsage(this IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);
        endpoints.MapGet("/api/categories/{categoryId:guid}/usage", ExecuteAsync).WithName("GetCategoryUsage");
        return endpoints;
    }

    public static async Task<Results<Ok<CategoryUsageResponse>, NotFound<ProblemDetails>, BadRequest<ProblemDetails>>>
        ExecuteAsync(Guid categoryId, [FromServices] UseCase useCase, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(useCase);
        try
        {
            var result = await useCase.ExecuteAsync(new(CategoryId.From(categoryId)), cancellationToken);
            if (result.Failure == GetCategoryUsageFailure.CategoryNotFound)
                return TypedResults.NotFound(new ProblemDetails { Title = "Category not found" });
            if (result.Failure is not null)
                throw new InvalidOperationException("Unsupported category usage failure.");
            var value = result.Usage
                ?? throw new InvalidOperationException("A successful usage result must contain usage data.");
            return TypedResults.Ok(new CategoryUsageResponse(
                categoryId, value.DirectChildCount, value.ProductAssignmentCount, value.IsInUse));
        }
        catch (ArgumentException exception)
        {
            return TypedResults.BadRequest(new ProblemDetails
            {
                Title = "Invalid category ID", Detail = exception.Message
            });
        }
    }
}

public sealed record CategoryUsageResponse(
    Guid CategoryId,
    int DirectChildCount,
    int ProductAssignmentCount,
    bool IsInUse);
