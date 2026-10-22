using Microsoft.AspNetCore.Mvc;
using HDWebFramework.Application.Catalog.CategoryNavigation;
namespace HDWebFramework.Api.Catalog.Categories;
public static class SaveShopNavigationEndpoint
{
    public static IEndpointRouteBuilder MapSaveShopNavigation(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPut("/api/categories/navigation-settings", ExecuteAsync).WithName("SaveShopNavigation");
        return endpoints;
    }
    public static async Task<IResult> ExecuteAsync(ShopNavigationRequest request, [FromServices] ICategoryNavigationStore store, CancellationToken ct) =>
        (await store.SaveShopAsync(new(request.ShowAssortmentInMenu, request.Revision), ct)).Failure is null ? Results.NoContent() : Results.Conflict(new ProblemDetails { Title = "Navigation settings changed", Detail = "Reload the navigation settings before saving." });
}
public sealed record ShopNavigationRequest(bool ShowAssortmentInMenu, Guid Revision);
