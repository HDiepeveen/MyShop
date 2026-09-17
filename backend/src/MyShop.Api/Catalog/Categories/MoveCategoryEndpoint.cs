using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using MyShop.Application.Catalog.MoveCategory;
using MyShop.Domain.Catalog;
using UseCase = MyShop.Application.Catalog.MoveCategory.MoveCategory;

namespace MyShop.Api.Catalog.Categories;

public static class MoveCategoryEndpoint
{
    public static IEndpointRouteBuilder MapMoveCategory(this IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);
        endpoints.MapPut("/api/categories/{categoryId:guid}/parent", ExecuteAsync)
            .WithName("MoveCategory");
        return endpoints;
    }

    public static async Task<Results<NoContent, NotFound<ProblemDetails>,
        Conflict<ProblemDetails>, BadRequest<ProblemDetails>>> ExecuteAsync(
        Guid categoryId, MoveCategoryRequest request,
        [FromServices] UseCase useCase, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(useCase);
        try
        {
            var result = await useCase.ExecuteAsync(new(
                CategoryId.From(categoryId),
                request.ParentCategoryId is null ? null : CategoryId.From(request.ParentCategoryId.Value)),
                cancellationToken);
            return result switch
            {
                MoveCategoryResult.Succeeded => TypedResults.NoContent(),
                MoveCategoryResult.CategoryNotFound => TypedResults.NotFound(new ProblemDetails
                    { Title = "Category not found" }),
                MoveCategoryResult.ParentNotFound => TypedResults.NotFound(new ProblemDetails
                    { Title = "Parent category not found" }),
                MoveCategoryResult.CycleDetected => TypedResults.Conflict(new ProblemDetails
                    { Title = "Category hierarchy cycle" }),
                _ => throw new InvalidOperationException("Unsupported move category result.")
            };
        }
        catch (ArgumentException exception)
        {
            return TypedResults.BadRequest(new ProblemDetails
            {
                Title = "Invalid category move", Detail = exception.Message
            });
        }
    }
}

public sealed record MoveCategoryRequest(Guid? ParentCategoryId);
