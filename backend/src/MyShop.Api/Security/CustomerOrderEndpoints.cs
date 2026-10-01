using System.Globalization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using MyShop.Application.Checkout.Abstractions;
using MyShop.Application.Customers.Abstractions;
using MyShop.Application.Customers.CancelCustomerOrder;
using MyShop.Application.Customers.GetCustomerOrder;
using MyShop.Application.Customers.ListCustomerOrders;
using MyShop.Domain.Checkout;

namespace MyShop.Api.Security;

public static class CustomerOrderEndpoints
{
    public static IEndpointRouteBuilder MapCustomerOrderEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/api/customer/orders", ListAsync)
            .RequireAuthorization(AdminSecurity.CustomerPolicy);
        endpoints.MapGet("/api/customer/orders/{id:guid}", GetAsync)
            .RequireAuthorization(AdminSecurity.CustomerPolicy);
        endpoints.MapPost("/api/customer/orders/{id:guid}/cancel", CancelAsync)
            .RequireAuthorization(AdminSecurity.CustomerPolicy);
        return endpoints;
    }

    private static async Task<IResult> CancelAsync(Guid id, CustomerOrderCancellationRequest? request,
        HttpContext context, UserManager<IdentityUser> users,
        [FromServices] CancelCustomerOrder useCase, CancellationToken cancellationToken)
    {
        if (id == Guid.Empty || request is null) return Results.BadRequest();
        var userId = users.GetUserId(context.User);
        if (userId is null) return Results.Unauthorized();
        try
        {
            var result = await useCase.ExecuteAsync(new(userId, id, request.Revision), cancellationToken);
            return result.Failure switch
            {
                CancelCustomerOrderFailure.NotFound => Results.NotFound(),
                CancelCustomerOrderFailure.InvalidTransition => Results.Conflict(new
                {
                    code = "invalidTransition",
                    message = "Alleen een bestelling die op betaling wacht kan worden geannuleerd."
                }),
                CancelCustomerOrderFailure.ConcurrencyConflict => Results.Conflict(new
                {
                    code = "concurrency",
                    message = "De bestelling is intussen gewijzigd. Vernieuw de pagina."
                }),
                null => Results.Ok(new CustomerOrderCancellationResponse("cancelled",
                    result.CancelledAt!.Value, result.Revision!.Value)),
                _ => throw new InvalidOperationException()
            };
        }
        catch (ArgumentException exception)
        {
            return Results.BadRequest(new { code = "invalidCancellation", message = exception.Message });
        }
    }

    private static async Task<IResult> ListAsync(int? offset, int? limit, HttpContext context,
        UserManager<IdentityUser> users, [FromServices] ListCustomerOrders useCase,
        CancellationToken cancellationToken)
    {
        var userId = users.GetUserId(context.User);
        if (userId is null) return Results.Unauthorized();
        var effectiveOffset = offset ?? 0;
        var effectiveLimit = limit ?? ListCustomerOrders.DefaultLimit;
        try
        {
            var page = await useCase.ExecuteAsync(new(userId, effectiveOffset, effectiveLimit),
                cancellationToken);
            return Results.Ok(new CustomerOrderListResponse(page.Items.Select(MapSummary).ToArray(),
                effectiveOffset, effectiveLimit, page.TotalCount));
        }
        catch (ArgumentOutOfRangeException exception)
        {
            return Results.BadRequest(new ProblemDetails { Title = "Invalid paging", Detail = exception.Message });
        }
    }

    private static async Task<IResult> GetAsync(Guid id, HttpContext context,
        UserManager<IdentityUser> users, [FromServices] GetCustomerOrder useCase,
        CancellationToken cancellationToken)
    {
        if (id == Guid.Empty) return Results.BadRequest();
        var userId = users.GetUserId(context.User);
        if (userId is null) return Results.Unauthorized();
        var order = await useCase.ExecuteAsync(userId, id, cancellationToken);
        return order is null ? Results.NotFound() : Results.Ok(MapDetail(order));
    }

    private static CustomerOrderSummaryResponse MapSummary(CustomerOrderListItem order) => new(
        order.Id, order.Number, order.PlacedAt, PaymentMethod(order.PaymentMethod),
        Status(order.Status), order.Totals.Select(MapTotal).ToArray());

    private static CustomerOrderDetailResponse MapDetail(CustomerOrderDetail order) => new(
        order.Id, order.Number, order.PlacedAt,
        new(order.CustomerName, order.Email),
        new(order.AddressLine, order.PostalCode, order.City, order.CountryCode),
        PaymentMethod(order.PaymentMethod), order.PaymentInstructions, Status(order.Status),
        order.PaidAt, order.ShippedAt, order.ShippingCarrier, order.TrackingCode,
        order.CancelledAt, order.RefundedAt, order.Revision,
        order.Lines.Select(line => new CustomerOrderLineResponse(line.ProductId, line.VariantId,
            line.ProductName, line.VariantName, line.Quantity, Amount(line.UnitAmount),
            line.Currency, Amount(line.TotalAmount))).ToArray(),
        order.Totals.Select(MapTotal).ToArray());

    private static CustomerOrderTotalResponse MapTotal(OrderTotalSnapshot total) =>
        new(total.Currency, Amount(total.Amount));
    private static string Amount(decimal value) => value.ToString("0.00", CultureInfo.InvariantCulture);
    private static string PaymentMethod(OrderPaymentMethod value) => value switch
    {
        OrderPaymentMethod.PayLater => "payLater",
        _ => throw new InvalidOperationException($"Unsupported order payment method: {value}.")
    };
    private static string Status(OrderStatus value) => value switch
    {
        OrderStatus.AwaitingPayment => "awaitingPayment",
        OrderStatus.Paid => "paid",
        OrderStatus.Shipped => "shipped",
        OrderStatus.Cancelled => "cancelled",
        OrderStatus.Refunded => "refunded",
        _ => throw new InvalidOperationException($"Unsupported order status: {value}.")
    };
}

public sealed record CustomerOrderListResponse(IReadOnlyList<CustomerOrderSummaryResponse> Items,
    int Offset, int Limit, int TotalCount);
public sealed record CustomerOrderSummaryResponse(Guid Id, string Number, DateTimeOffset PlacedAt,
    string PaymentMethod, string Status, IReadOnlyList<CustomerOrderTotalResponse> Totals);
public sealed record CustomerOrderDetailResponse(Guid Id, string Number, DateTimeOffset PlacedAt,
    CustomerOrderCustomerResponse Customer, CustomerOrderAddressResponse DeliveryAddress,
    string PaymentMethod, string? PaymentInstructions, string Status, DateTimeOffset? PaidAt,
    DateTimeOffset? ShippedAt, string? ShippingCarrier, string? TrackingCode,
    DateTimeOffset? CancelledAt, DateTimeOffset? RefundedAt, Guid Revision,
    IReadOnlyList<CustomerOrderLineResponse> Lines, IReadOnlyList<CustomerOrderTotalResponse> Totals);
public sealed record CustomerOrderCustomerResponse(string Name, string Email);
public sealed record CustomerOrderAddressResponse(string AddressLine, string PostalCode, string City,
    string CountryCode);
public sealed record CustomerOrderLineResponse(Guid ProductId, Guid VariantId, string ProductName,
    string VariantName, int Quantity, string UnitAmount, string Currency, string TotalAmount);
public sealed record CustomerOrderTotalResponse(string Currency, string Amount);
public sealed record CustomerOrderCancellationRequest(Guid Revision);
public sealed record CustomerOrderCancellationResponse(string Status, DateTimeOffset CancelledAt,
    Guid Revision);
