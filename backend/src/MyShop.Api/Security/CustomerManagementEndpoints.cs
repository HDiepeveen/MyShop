using Microsoft.AspNetCore.Mvc;
using MyShop.Application.Customers.Abstractions;
using MyShop.Application.Customers.ManageCustomers;

namespace MyShop.Api.Security;

public static class CustomerManagementEndpoints
{
    public static void MapCustomerManagementEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/customers")
            .RequireAuthorization(policy => policy.RequireRole(AdminSecurity.Role));
        group.MapGet("", ListAsync);
        group.MapGet("/{id}", GetAsync);
        group.MapPut("/{id}/access", SetAccessAsync);
    }

    private static async Task<IResult> ListAsync(int? offset, int? limit, string? search,
        [FromServices] ListManagedCustomers useCase, CancellationToken cancellationToken)
    {
        var effectiveOffset = offset ?? 0; var effectiveLimit = limit ?? 20;
        try
        {
            var page = await useCase.ExecuteAsync(new(effectiveOffset, effectiveLimit, search), cancellationToken);
            return Results.Ok(new ManagedCustomerListResponse(page.Items, effectiveOffset,
                effectiveLimit, page.TotalCount));
        }
        catch (ArgumentException exception)
        {
            return Results.BadRequest(new { code = "invalidQuery", message = exception.Message });
        }
    }

    private static async Task<IResult> GetAsync(string id, [FromServices] GetManagedCustomer useCase,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(id)) return Results.BadRequest();
        var customer = await useCase.ExecuteAsync(id, cancellationToken);
        return customer is null ? Results.NotFound() : Results.Ok(customer);
    }

    private static async Task<IResult> SetAccessAsync(string id, CustomerAccessRequest? request,
        [FromServices] SetCustomerLock useCase, CancellationToken cancellationToken)
    {
        if (request is null) return Results.BadRequest();
        try
        {
            var result = await useCase.ExecuteAsync(new(id, request.Revision, request.Locked), cancellationToken);
            return result.Failure switch
            {
                SetCustomerLockFailure.NotFound => Results.NotFound(),
                SetCustomerLockFailure.ConcurrencyConflict => Results.Conflict(new
                { code = "concurrency", message = "Het klantaccount is intussen gewijzigd." }),
                null => Results.Ok(new CustomerAccessResponse(request.Locked, result.Revision!)),
                _ => throw new InvalidOperationException()
            };
        }
        catch (ArgumentException exception)
        {
            return Results.BadRequest(new { code = "invalidAccess", message = exception.Message });
        }
    }
}

public sealed record ManagedCustomerListResponse(IReadOnlyList<ManagedCustomerListItem> Items,
    int Offset, int Limit, int TotalCount);
public sealed record CustomerAccessRequest(bool Locked, string Revision);
public sealed record CustomerAccessResponse(bool Locked, string Revision);
