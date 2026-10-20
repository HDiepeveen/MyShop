using Microsoft.AspNetCore.Mvc;
using MyShop.Application.Catalog.CategoryNavigation;
namespace MyShop.Api.Catalog.Categories;
public static class SaveCategoryNavigationEndpoint
{
    public static IEndpointRouteBuilder MapSaveCategoryNavigation(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPut("/api/categories/{categoryId:guid}/navigation", ExecuteAsync).WithName("SaveCategoryNavigation");
        return endpoints;
    }
    public static async Task<IResult> ExecuteAsync(Guid categoryId, CategoryNavigationRequest request, [FromServices] ICategoryNavigationStore store, CancellationToken ct) =>
        (await store.SaveAsync(new(categoryId, request.ShowInMenu, request.Revision), ct)).Failure switch
        {
            CategoryNavigationFailure.NotFound => Results.NotFound(),
            CategoryNavigationFailure.Conflict => Results.Conflict(new ProblemDetails { Title = "Category navigation changed", Detail = "Reload the category navigation settings before saving." }),
            _ => Results.NoContent()
        };
}
public sealed record CategoryNavigationRequest(bool ShowInMenu, Guid Revision);
