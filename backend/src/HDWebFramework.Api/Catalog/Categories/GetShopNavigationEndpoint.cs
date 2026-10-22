using Microsoft.AspNetCore.Mvc;
using HDWebFramework.Application.Catalog.CategoryNavigation;
namespace HDWebFramework.Api.Catalog.Categories;
public static class GetShopNavigationEndpoint
{
    public static IEndpointRouteBuilder MapGetShopNavigation(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/api/categories/navigation-settings", ExecuteAsync).WithName("GetShopNavigation");
        return endpoints;
    }
    public static async Task<IResult> ExecuteAsync([FromServices] ICategoryNavigationStore store, CancellationToken ct) =>
        Results.Ok(await store.GetShopAsync(ct));
}
