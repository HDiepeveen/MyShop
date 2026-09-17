using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
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

    public static async Task<Ok<IReadOnlyList<CategorySummaryResponse>>> ExecuteAsync(
        [FromServices] UseCase useCase,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(useCase);

        var categories = await useCase.ExecuteAsync(cancellationToken);
        IReadOnlyList<CategorySummaryResponse> response = categories
            .Select(category => new CategorySummaryResponse(
                category.Id,
                category.Name,
                category.ParentCategoryId,
                category.ParentCategoryId is null))
            .ToArray();

        return TypedResults.Ok(response);
    }
}

public sealed record CategorySummaryResponse(
    Guid Id,
    string Name,
    Guid? ParentCategoryId,
    bool IsRoot);
