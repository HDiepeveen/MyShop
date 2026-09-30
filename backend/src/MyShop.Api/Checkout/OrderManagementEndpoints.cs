using System.Globalization;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using MyShop.Application.Checkout.Abstractions;
using MyShop.Domain.Checkout;
using GetUseCase = MyShop.Application.Checkout.GetOrder.GetOrder;
using ListUseCase = MyShop.Application.Checkout.ListOrders.ListOrders;
using MyShop.Application.Checkout.ListOrders;
using MyShop.Application.Checkout.MarkOrderPaid;
using MyShop.Application.Checkout.MarkOrderShipped;
using MyShop.Application.Checkout.CancelOrder;

namespace MyShop.Api.Checkout;

public static class OrderManagementEndpoints
{
    public static IEndpointRouteBuilder MapOrderManagement(this IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);
        endpoints.MapGet("/api/orders", ListAsync).WithName("ListOrders");
        endpoints.MapGet("/api/orders/{id:guid}", GetAsync).WithName("GetOrder");
        endpoints.MapPut("/api/orders/{id:guid}/status", UpdateStatusAsync).WithName("UpdateOrderStatus");
        return endpoints;
    }

    public static async Task<Results<Ok<OrderListResponse>, BadRequest<ProblemDetails>>> ListAsync(
        [FromQuery] int? offset,
        [FromQuery] int? limit,
        [FromServices] ListUseCase useCase,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(useCase);
        var effectiveOffset = offset ?? 0;
        var effectiveLimit = limit ?? ListUseCase.DefaultLimit;
        try
        {
            var page = await useCase.ExecuteAsync(
                new ListOrdersQuery(effectiveOffset, effectiveLimit), cancellationToken);
            return TypedResults.Ok(new OrderListResponse(
                page.Items.Select(MapSummary).ToArray(),
                effectiveOffset,
                effectiveLimit,
                page.TotalCount));
        }
        catch (ArgumentOutOfRangeException exception)
        {
            return TypedResults.BadRequest(new ProblemDetails
            {
                Title = "Invalid order paging",
                Detail = exception.Message
            });
        }
    }

    public static async Task<Results<Ok<OrderDetailResponse>, NotFound>> GetAsync(
        Guid id,
        [FromServices] GetUseCase useCase,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(useCase);
        var order = await useCase.ExecuteAsync(id, cancellationToken);
        return order is null ? TypedResults.NotFound() : TypedResults.Ok(MapDetail(order));
    }

    public static async Task<IResult> UpdateStatusAsync(
        Guid id,
        UpdateOrderStatusRequest? request,
        [FromServices] MarkOrderPaid markPaid,
        [FromServices] MarkOrderShipped markShipped,
        [FromServices] CancelOrder cancelOrder,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(markPaid);
        ArgumentNullException.ThrowIfNull(markShipped);
        ArgumentNullException.ThrowIfNull(cancelOrder);
        if (request?.Status is not ("paid" or "shipped" or "cancelled"))
            return Results.BadRequest(new { code = "invalidStatus", message = "Kies een geldige bestelstatus." });
        try
        {
            if (request.Status == "paid")
            {
                var result = await markPaid.ExecuteAsync(new(id, request.Revision), cancellationToken);
                return result.Failure switch
                {
                    MarkOrderPaidFailure.NotFound => Results.NotFound(),
                    MarkOrderPaidFailure.InvalidTransition => Results.Conflict(new
                    {
                        code = "invalidTransition",
                        message = "Deze bestelling kan niet meer als betaald worden gemarkeerd."
                    }),
                    MarkOrderPaidFailure.ConcurrencyConflict => ConcurrencyConflict(),
                    null => Results.Ok(MapStatus(result.Order!)),
                    _ => throw new InvalidOperationException()
                };
            }
            if (request.Status == "shipped")
            {
                var shipped = await markShipped.ExecuteAsync(new(id, request.Revision), cancellationToken);
                return shipped.Failure switch
                {
                    MarkOrderShippedFailure.NotFound => Results.NotFound(),
                    MarkOrderShippedFailure.InvalidTransition => Results.Conflict(new
                    {
                        code = "invalidTransition",
                        message = "Alleen een betaalde bestelling kan als verzonden worden gemarkeerd."
                    }),
                    MarkOrderShippedFailure.ConcurrencyConflict => ConcurrencyConflict(),
                    null => Results.Ok(MapStatus(shipped.Order!)),
                    _ => throw new InvalidOperationException()
                };
            }
            var cancelled = await cancelOrder.ExecuteAsync(
                new(id, request.Revision, request.Reason ?? ""), cancellationToken);
            return cancelled.Failure switch
            {
                CancelOrderFailure.NotFound => Results.NotFound(),
                CancelOrderFailure.InvalidTransition => Results.Conflict(new
                {
                    code = "invalidTransition",
                    message = "Alleen een bestelling die op betaling wacht kan worden geannuleerd."
                }),
                CancelOrderFailure.ConcurrencyConflict => ConcurrencyConflict(),
                null => Results.Ok(MapStatus(cancelled.Order!)),
                _ => throw new InvalidOperationException()
            };
        }
        catch (ArgumentException exception)
        {
            return Results.BadRequest(new { code = "invalidOrderStatus", message = exception.Message });
        }
    }

    private static IResult ConcurrencyConflict() => Results.Conflict(new
    {
        code = "concurrency",
        message = "De bestelling is intussen gewijzigd."
    });

    private static OrderStatusResponse MapStatus(OrderStatusSnapshot order) =>
        new(Status(order.Status), order.PaidAt, order.ShippedAt, order.CancelledAt,
            order.CancellationReason, order.Revision);

    private static OrderSummaryResponse MapSummary(OrderListItem order) => new(
        order.Id,
        order.Number,
        order.PlacedAt,
        order.CustomerName,
        PaymentMethod(order.PaymentMethod),
        Status(order.Status),
        order.PaidAt,
        order.ShippedAt,
        order.CancelledAt,
        order.CancellationReason,
        order.Totals.Select(MapTotal).ToArray());

    private static OrderDetailResponse MapDetail(OrderDetail order) => new(
        order.Id,
        order.Number,
        order.PlacedAt,
        new(order.CustomerName, order.Email),
        new(order.AddressLine, order.PostalCode, order.City, order.CountryCode),
        PaymentMethod(order.PaymentMethod),
        Status(order.Status),
        order.PaidAt,
        order.ShippedAt,
        order.CancelledAt,
        order.CancellationReason,
        order.Revision,
        order.Lines.Select(line => new OrderLineResponse(
            line.ProductId,
            line.VariantId,
            line.ProductName,
            line.VariantName,
            line.Quantity,
            Amount(line.UnitAmount),
            line.Currency,
            Amount(line.TotalAmount))).ToArray(),
        order.Totals.Select(MapTotal).ToArray());

    private static OrderTotalResponse MapTotal(OrderTotalSnapshot total) =>
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
        _ => throw new InvalidOperationException($"Unsupported order status: {value}.")
    };
}

public sealed record OrderListResponse(
    IReadOnlyList<OrderSummaryResponse> Items,
    int Offset,
    int Limit,
    int TotalCount);
public sealed record OrderSummaryResponse(
    Guid Id,
    string Number,
    DateTimeOffset PlacedAt,
    string CustomerName,
    string PaymentMethod,
    string Status,
    DateTimeOffset? PaidAt,
    DateTimeOffset? ShippedAt,
    DateTimeOffset? CancelledAt,
    string? CancellationReason,
    IReadOnlyList<OrderTotalResponse> Totals);
public sealed record OrderDetailResponse(
    Guid Id,
    string Number,
    DateTimeOffset PlacedAt,
    OrderCustomerResponse Customer,
    OrderAddressResponse DeliveryAddress,
    string PaymentMethod,
    string Status,
    DateTimeOffset? PaidAt,
    DateTimeOffset? ShippedAt,
    DateTimeOffset? CancelledAt,
    string? CancellationReason,
    Guid Revision,
    IReadOnlyList<OrderLineResponse> Lines,
    IReadOnlyList<OrderTotalResponse> Totals);
public sealed record OrderCustomerResponse(string Name, string Email);
public sealed record OrderAddressResponse(string AddressLine, string PostalCode, string City, string CountryCode);
public sealed record OrderLineResponse(
    Guid ProductId,
    Guid VariantId,
    string ProductName,
    string VariantName,
    int Quantity,
    string UnitAmount,
    string Currency,
    string TotalAmount);
public sealed record OrderTotalResponse(string Currency, string Amount);
public sealed record UpdateOrderStatusRequest(string Status, Guid Revision, string? Reason = null);
public sealed record OrderStatusResponse(
    string Status,
    DateTimeOffset? PaidAt,
    DateTimeOffset? ShippedAt,
    DateTimeOffset? CancelledAt,
    string? CancellationReason,
    Guid Revision);
