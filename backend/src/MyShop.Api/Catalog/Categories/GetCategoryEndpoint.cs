using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using MyShop.Application.Catalog.GetCategory;
using MyShop.Domain.Catalog;
using UseCase = MyShop.Application.Catalog.GetCategory.GetCategory;

namespace MyShop.Api.Catalog.Categories;

public static class GetCategoryEndpoint
{
    public static IEndpointRouteBuilder MapGetCategory(this IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        endpoints.MapGet("/api/categories/{categoryId:guid}", ExecuteAsync)
            .WithName("GetCategory");

        return endpoints;
    }

    public static async Task<Results<
        Ok<GetCategoryResponse>,
        NotFound<ProblemDetails>,
        BadRequest<ProblemDetails>>> ExecuteAsync(
        Guid categoryId,
        [FromServices] UseCase useCase,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(useCase);

        try
        {
            var result = await useCase.ExecuteAsync(
                new GetCategoryQuery(CategoryId.From(categoryId)),
                cancellationToken);

            if (result.Failure == GetCategoryFailure.CategoryNotFound)
                return TypedResults.NotFound(new ProblemDetails
                {
                    Title = "Category not found",
                    Detail = $"Category '{categoryId}' does not exist."
                });
            if (result.Failure is not null)
                throw new InvalidOperationException(
                    $"Get category failure '{result.Failure}' is not supported.");

            var category = result.Category
                ?? throw new InvalidOperationException(
                    "A successful get result must contain a category.");

            return TypedResults.Ok(new GetCategoryResponse(
                category.Id.Value,
                category.Name,
                category.ParentCategoryId?.Value,
                category.IsRoot));
        }
        catch (ArgumentException exception)
        {
            return TypedResults.BadRequest(new ProblemDetails
            {
                Title = "Invalid category ID",
                Detail = exception.Message
            });
        }
    }
}

public sealed record GetCategoryResponse(
    Guid Id,
    string Name,
    Guid? ParentCategoryId,
    bool IsRoot);
