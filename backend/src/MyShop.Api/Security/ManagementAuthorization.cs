using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using MyShop.Application.Security;

namespace MyShop.Api.Security;

public sealed class ManagementRequirement : IAuthorizationRequirement;
public sealed class ManagementAuthorization(ICompanyAccess access) : AuthorizationHandler<ManagementRequirement>
{
    protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, ManagementRequirement requirement)
    {
        if (context.User.IsInRole(AdminSecurity.Role)) { context.Succeed(requirement); return; }
        if (context.Resource is not HttpContext http || context.User.Identity?.IsAuthenticated != true) return;
        var userId = context.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId is null) return;
        var granted = await access.PermissionsAsync(userId, http.RequestAborted);
        var route = (http.GetEndpoint() as RouteEndpoint)?.RoutePattern.RawText ?? "";
        var permission = Permission(route);
        if (permission is not null && granted.Contains(permission)) { context.Succeed(requirement); return; }
        // Product editing needs these read-only lookup lists, without granting their administration pages.
        if (HttpMethods.IsGet(http.Request.Method) && granted.Contains("products")
            && (Segment(route, "/api/categories") || Segment(route, "/api/product-types") || route == "/api/billing/vat-rates"))
            context.Succeed(requirement);
    }
    public static string? Permission(string route) => route switch
    {
        "/api/dashboard" => "overview",
        "/api/seo-settings" => "seo",
        "/api/shop-appearance" or "/api/shop-icon" => "appearance",
        _ when Segment(route, "/api/products") => "products",
        _ when Segment(route, "/api/categories") => "categories",
        _ when Segment(route, "/api/product-types") => "productTypes",
        _ when Segment(route, "/api/orders") => "orders",
        _ when Segment(route, "/api/customers") => "customers",
        _ when Segment(route, "/api/payment-options") => "payments",
        _ when Segment(route, "/api/delivery-methods") => "delivery",
        _ when Segment(route, "/api/email-settings") => "email",
        _ when Segment(route, "/api/billing") => "billing",
        _ => null,
    };
    private static bool Segment(string route, string root) => route == root || route.StartsWith(root + "/", StringComparison.Ordinal);
}
