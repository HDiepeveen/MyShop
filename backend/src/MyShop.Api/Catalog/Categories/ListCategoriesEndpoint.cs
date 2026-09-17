using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using MyShop.Application.Catalog.ListCategories;
using UseCase = MyShop.Application.Catalog.ListCategories.ListCategories;

namespace MyShop.Api.Catalog.Categories;

public static class ListCategoriesEndpoint
{
    public static IEndpointRouteBuilder MapListCategories(this IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        endpoints.MapGet("/api/categories", ExecuteAsync)
            .WithName("ListCategories");

        return endpoints;
    }

    public static async Task<Results<
        Ok<IReadOnlyList<CategorySummaryResponse>>,
        BadRequest<ProblemDetails>>> ExecuteAsync(
        [FromQuery] string? search,
        [FromServices] UseCase useCase,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(useCase);

        try
        {
            var categories = await useCase.ExecuteAsync(
                new ListCategoriesQuery(search), cancellationToken);
            IReadOnlyList<CategorySummaryResponse> response = categories
                .Select(category => new CategorySummaryResponse(
                    category.Id,
                    category.Name,
                    category.ParentCategoryId,
                    category.ParentCategoryId is null))
                .ToArray();
            return TypedResults.Ok(response);
        }
        catch (ArgumentException exception)
        {
            return TypedResults.BadRequest(new ProblemDetails
            {
                Title = "Invalid category search",
                Detail = exception.Message
            });
        }
    }
}

public sealed record CategorySummaryResponse(
    Guid Id,
    string Name,
    Guid? ParentCategoryId,
    bool IsRoot);
