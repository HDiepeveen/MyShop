using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using MyShop.Application.Customers.ManageWishlist;
using MyShop.Application.Customers.Abstractions;

namespace MyShop.Api.Security;

public static class CustomerWishlistEndpoints
{
    public static IEndpointRouteBuilder MapCustomerWishlistEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/customer/wishlist")
            .RequireAuthorization(AdminSecurity.CustomerPolicy);
        group.MapGet("", ListAsync);
        group.MapGet("/{productId:guid}", StateAsync);
        group.MapPost("/{productId:guid}", AddAsync);
        group.MapDelete("/{productId:guid}", RemoveAsync);
        return endpoints;
    }

    private static async Task<IResult> ListAsync(int? offset, int? limit, string? search, string? sort, HttpContext context,
        UserManager<IdentityUser> users, [FromServices] ListWishlist useCase,
        CancellationToken cancellationToken)
    {
        var userId = users.GetUserId(context.User);
        if (userId is null) return Results.Unauthorized();
        var effectiveOffset = offset ?? 0;
        var effectiveLimit = limit ?? ListWishlist.DefaultLimit;
        try
        {
            var ordering = sort switch { null or "" or "newest" => WishlistSort.Newest,
                "name" => WishlistSort.Name, _ => throw new ArgumentException("Sort is not supported.", nameof(sort)) };
            var page = await useCase.ExecuteAsync(new(userId, effectiveOffset, effectiveLimit, search, ordering), cancellationToken);
            return Results.Ok(new WishlistPageResponse(page.Items.Select(item => new WishlistItemResponse(
                item.ProductId, item.Name, item.ImageUrl, item.ImageAlt, item.IsAvailable, item.AddedAt)).ToArray(),
                effectiveOffset, effectiveLimit, page.TotalCount));
        }
        catch (ArgumentException exception)
        {
            return Results.BadRequest(new ProblemDetails { Title = "Invalid wishlist query", Detail = exception.Message });
        }
    }

    private static async Task<IResult> StateAsync(Guid productId, HttpContext context,
        UserManager<IdentityUser> users, [FromServices] GetWishlistState useCase,
        CancellationToken cancellationToken)
    {
        if (productId == Guid.Empty) return Results.BadRequest();
        var userId = users.GetUserId(context.User);
        if (userId is null) return Results.Unauthorized();
        return Results.Ok(new WishlistStateResponse(await useCase.ExecuteAsync(userId, productId, cancellationToken)));
    }

    private static async Task<IResult> AddAsync(Guid productId, HttpContext context,
        UserManager<IdentityUser> users, [FromServices] AddWishlistItem useCase,
        CancellationToken cancellationToken)
    {
        if (productId == Guid.Empty) return Results.BadRequest();
        var userId = users.GetUserId(context.User);
        if (userId is null) return Results.Unauthorized();
        return await useCase.ExecuteAsync(userId, productId, cancellationToken)
            ? Results.NoContent() : Results.NotFound();
    }

    private static async Task<IResult> RemoveAsync(Guid productId, HttpContext context,
        UserManager<IdentityUser> users, [FromServices] RemoveWishlistItem useCase,
        CancellationToken cancellationToken)
    {
        if (productId == Guid.Empty) return Results.BadRequest();
        var userId = users.GetUserId(context.User);
        if (userId is null) return Results.Unauthorized();
        await useCase.ExecuteAsync(userId, productId, cancellationToken);
        return Results.NoContent();
    }
}

public sealed record WishlistItemResponse(Guid ProductId, string Name, string? ImageUrl,
    string ImageAlt, bool IsAvailable, DateTimeOffset AddedAt);
public sealed record WishlistPageResponse(IReadOnlyList<WishlistItemResponse> Items, int Offset,
    int Limit, int TotalCount);
public sealed record WishlistStateResponse(bool Saved);
