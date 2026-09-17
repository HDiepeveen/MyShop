using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using MyShop.Application.Catalog.DeleteCategory;
using MyShop.Domain.Catalog;
using UseCase = MyShop.Application.Catalog.DeleteCategory.DeleteCategory;

namespace MyShop.Api.Catalog.Categories;

public static class DeleteCategoryEndpoint
{
    public static IEndpointRouteBuilder MapDeleteCategory(this IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);
        endpoints.MapDelete("/api/categories/{categoryId:guid}", ExecuteAsync)
            .WithName("DeleteCategory");
        return endpoints;
    }

    public static async Task<Results<NoContent, NotFound<ProblemDetails>,
        Conflict<ProblemDetails>, BadRequest<ProblemDetails>>> ExecuteAsync(
        Guid categoryId, [FromServices] UseCase useCase, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(useCase);
        try
        {
            var result = await useCase.ExecuteAsync(CategoryId.From(categoryId), cancellationToken);
            return result.Outcome switch
            {
                DeleteCategoryOutcome.Succeeded => TypedResults.NoContent(),
                DeleteCategoryOutcome.NotFound => TypedResults.NotFound(new ProblemDetails
                    { Title = "Category not found" }),
                DeleteCategoryOutcome.InUse => TypedResults.Conflict(new ProblemDetails
                {
                    Title = "Category is in use",
                    Detail = $"Category has {result.Usage!.DirectChildCount} direct children and " +
                             $"{result.Usage.ProductAssignmentCount} product assignments."
                }),
                _ => throw new InvalidOperationException("Unsupported delete category result.")
            };
        }
        catch (ArgumentException exception)
        {
            return TypedResults.BadRequest(new ProblemDetails
            {
                Title = "Invalid category deletion", Detail = exception.Message
            });
        }
    }
}
