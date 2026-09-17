using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using MyShop.Application.Catalog.RenameCategory;
using MyShop.Domain.Catalog;
using UseCase = MyShop.Application.Catalog.RenameCategory.RenameCategory;

namespace MyShop.Api.Catalog.Categories;

public static class RenameCategoryEndpoint
{
    public static IEndpointRouteBuilder MapRenameCategory(this IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);
        endpoints.MapPatch("/api/categories/{categoryId:guid}/name", ExecuteAsync)
            .WithName("RenameCategory");
        return endpoints;
    }

    public static async Task<Results<NoContent, NotFound<ProblemDetails>, BadRequest<ProblemDetails>>>
        ExecuteAsync(Guid categoryId, RenameCategoryRequest request,
            [FromServices] UseCase useCase, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(useCase);
        try
        {
            var found = await useCase.ExecuteAsync(
                new(CategoryId.From(categoryId), request.Name), cancellationToken);
            return found ? TypedResults.NoContent() : TypedResults.NotFound(new ProblemDetails
            {
                Title = "Category not found", Detail = $"Category '{categoryId}' does not exist."
            });
        }
        catch (ArgumentException exception)
        {
            return TypedResults.BadRequest(new ProblemDetails
            {
                Title = "Invalid category rename", Detail = exception.Message
            });
        }
    }
}

public sealed record RenameCategoryRequest(string Name);
