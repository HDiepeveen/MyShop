using Microsoft.AspNetCore.Mvc;
using MyShop.Application.Catalog.CategoryNavigation;
namespace MyShop.Api.Catalog.Categories;
public static class GetCategoryNavigationEndpoint
{
    public static IEndpointRouteBuilder MapGetCategoryNavigation(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/api/categories/{categoryId:guid}/navigation", ExecuteAsync).WithName("GetCategoryNavigation");
        return endpoints;
    }
    public static async Task<IResult> ExecuteAsync(Guid categoryId, [FromServices] ICategoryNavigationStore store, CancellationToken ct)
    {
        var settings = await store.GetAsync(categoryId, ct);
        return settings is null ? Results.NotFound() : Results.Ok(settings);
    }
}
