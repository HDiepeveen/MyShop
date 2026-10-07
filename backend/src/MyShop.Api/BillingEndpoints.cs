using Microsoft.AspNetCore.Identity;
using MyShop.Application.Billing;
using MyShop.Api.Security;
namespace MyShop.Api;
public static class BillingEndpoints
{
    public static void MapBilling(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/api/billing/company", async (BillingSettings service, CancellationToken token) => Results.Ok(await service.CompanyAsync(token)));
        endpoints.MapPut("/api/billing/company", async (CompanySettings value, BillingSettings service, CancellationToken token) =>
        {
            try { var saved = await service.SaveCompanyAsync(value, token); return saved is null ? Results.Conflict() : Results.Ok(saved); }
            catch (ArgumentException error) { return Invalid(error.Message); }
        });
        endpoints.MapGet("/api/billing/vat-rates", async (bool? enabledOnly, BillingSettings service, CancellationToken token) => Results.Ok(await service.RatesAsync(enabledOnly ?? false, token)));
        endpoints.MapPost("/api/billing/vat-rates", async (SaveVatRate value, BillingSettings service, CancellationToken token) =>
        {
            try { var saved = await service.SaveRateAsync(value, token); return saved is null ? Results.Conflict() : Results.Ok(saved); }
            catch (ArgumentException error) { return Invalid(error.Message); }
        });
        endpoints.MapGet("/api/orders/{orderId:guid}/invoice", async (Guid orderId, IInvoiceRepository repository, CancellationToken token) =>
        { var invoice = await repository.GetAsync(orderId, null, token); return invoice is null ? Results.NotFound() : Results.Ok(invoice); });
        endpoints.MapPost("/api/orders/{orderId:guid}/invoice", async (Guid orderId, InvoiceRequest request, IssueInvoice service, CancellationToken token) =>
        {
            try { var invoice = await service.ExecuteAsync(new(orderId, request.OrderRevision, request.SupplyDate, request.Buyer, request.TaxReviewed, request.TaxStatement), token);
                return invoice is null ? Results.Conflict() : Results.Ok(invoice); }
            catch (ArgumentException error) { return Invalid(error.Message); }
        });
        endpoints.MapGet("/api/customer/orders/{orderId:guid}/invoice", async (Guid orderId, HttpContext context, UserManager<IdentityUser> users, IInvoiceRepository repository, CancellationToken token) =>
        {
            var user = await users.GetUserAsync(context.User); if (user is null) return Results.Unauthorized();
            var invoice = await repository.GetAsync(orderId, user.Id, token); return invoice is null ? Results.NotFound() : Results.Ok(invoice);
        }).RequireAuthorization(AdminSecurity.CustomerPolicy);
    }
    private static IResult Invalid(string message) => Results.BadRequest(new { code = "billing", message });
}
public sealed record InvoiceRequest(Guid OrderRevision, DateOnly SupplyDate, InvoiceBuyer Buyer, bool TaxReviewed, string TaxStatement);
