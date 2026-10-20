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
    public static async Task<IResult> ExecuteAsync(Guid categoryId, CategoryNavigationRequest request, [FromServices] ICategoryNavigationStore store, CancellationToken ct)
    {
        try
        {
            var settings = new CategoryNavigationSettings(categoryId, request.ShowInMenu, request.Revision) { WebAddress = request.WebAddress, SeoTitle = request.SeoTitle, SeoDescription = request.SeoDescription }.Normalize();
            return (await store.SaveAsync(settings, ct)).Failure switch
            {
                CategoryNavigationFailure.NotFound => Results.NotFound(),
                CategoryNavigationFailure.Conflict => Results.Conflict(new ProblemDetails { Title = "Category navigation changed", Detail = "Reload the category navigation settings before saving." }),
                CategoryNavigationFailure.AddressInUse => Results.BadRequest(new ProblemDetails { Title = "Webadres is al in gebruik", Detail = "Kies een ander webadres voor deze categorie." }),
                _ => Results.NoContent()
            };
        }
        catch (ArgumentException exception) { return Results.BadRequest(new ProblemDetails { Title = "Invalid category SEO", Detail = exception.Message }); }
    }
}
public sealed record CategoryNavigationRequest(bool ShowInMenu, Guid Revision, string? WebAddress = null, string? SeoTitle = null, string? SeoDescription = null);
