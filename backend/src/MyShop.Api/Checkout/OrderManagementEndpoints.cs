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
using MyShop.Application.Checkout.RefundOrder;

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
        [FromQuery] string? status,
        [FromQuery] string? search,
        [FromServices] ListUseCase useCase,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(useCase);
        var effectiveOffset = offset ?? 0;
        var effectiveLimit = limit ?? ListUseCase.DefaultLimit;
        try
        {
            OrderStatus? statusFilter = status switch
            {
                null or "" => null,
                "awaitingPayment" => OrderStatus.AwaitingPayment,
                "paid" => OrderStatus.Paid,
                "shipped" => OrderStatus.Shipped,
                "cancelled" => OrderStatus.Cancelled,
                "refunded" => OrderStatus.Refunded,
                _ => throw new ArgumentOutOfRangeException(nameof(status), "Status is not supported.")
            };
            var page = await useCase.ExecuteAsync(
                new ListOrdersQuery(effectiveOffset, effectiveLimit, statusFilter, search), cancellationToken);
            return TypedResults.Ok(new OrderListResponse(
                page.Items.Select(MapSummary).ToArray(),
                effectiveOffset,
                effectiveLimit,
                page.TotalCount));
        }
        catch (ArgumentException exception)
        {
            return TypedResults.BadRequest(new ProblemDetails
            {
                Title = "Invalid order query",
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
        [FromServices] RefundOrder refundOrder,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(markPaid);
        ArgumentNullException.ThrowIfNull(markShipped);
        ArgumentNullException.ThrowIfNull(cancelOrder);
        ArgumentNullException.ThrowIfNull(refundOrder);
        if (request?.Status is not ("paid" or "shipped" or "cancelled" or "refunded"))
            return Results.BadRequest(new { code = "invalidStatus", message = "Kies een geldige bestelstatus." });
        try
        {
            if (request.Status == "paid")
            {
                var result = await markPaid.ExecuteAsync(
                    new(id, request.Revision, request.PaymentReference ?? ""), cancellationToken);
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
                var shipped = await markShipped.ExecuteAsync(new(id, request.Revision,
                    request.Carrier ?? "", request.TrackingCode ?? ""), cancellationToken);
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
            if (request.Status == "refunded")
            {
                var refunded = await refundOrder.ExecuteAsync(new(id, request.Revision,
                    request.RefundReference ?? "", request.Reason ?? ""), cancellationToken);
                return refunded.Failure switch
                {
                    RefundOrderFailure.NotFound => Results.NotFound(),
                    RefundOrderFailure.InvalidTransition => Results.Conflict(new
                    {
                        code = "invalidTransition",
                        message = "Alleen een betaalde, nog niet verzonden bestelling kan als terugbetaald worden gemarkeerd."
                    }),
                    RefundOrderFailure.ConcurrencyConflict => ConcurrencyConflict(),
                    null => Results.Ok(MapStatus(refunded.Order!)),
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
        new(Status(order.Status), order.PaidAt, order.PaymentReference, order.ShippedAt,
            order.ShippingCarrier, order.TrackingCode, order.CancelledAt,
            order.CancellationReason, order.RefundedAt, order.RefundReference,
            order.RefundReason, order.Revision);

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
        order.RefundedAt,
        order.RefundReason,
        order.Totals.Select(MapTotal).ToArray());

    private static OrderDetailResponse MapDetail(OrderDetail order) => new(
        order.Id,
        order.Number,
        order.PlacedAt,
        new(order.CustomerName, order.Email),
        new(order.AddressLine, order.PostalCode, order.City, order.CountryCode),
        PaymentMethod(order.PaymentMethod),
        order.PaymentInstructions,
        Status(order.Status),
        order.PaidAt,
        order.PaymentReference,
        order.ShippedAt,
        order.ShippingCarrier,
        order.TrackingCode,
        order.CancelledAt,
        order.CancellationReason,
        order.RefundedAt,
        order.RefundReference,
        order.RefundReason,
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
        order.Totals.Select(MapTotal).ToArray(),
        order.DeliveryMethod is null ? null : new(order.DeliveryMethod.Id,
            order.DeliveryMethod.Name, order.DeliveryMethod.Description,
            Amount(order.DeliveryMethod.Amount), order.DeliveryMethod.Currency));

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
        OrderStatus.Refunded => "refunded",
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
    DateTimeOffset? RefundedAt,
    string? RefundReason,
    IReadOnlyList<OrderTotalResponse> Totals);
public sealed record OrderDeliveryMethodResponse(Guid Id, string Name, string? Description,
    string Amount, string Currency);
public sealed record OrderDetailResponse(
    Guid Id,
    string Number,
    DateTimeOffset PlacedAt,
    OrderCustomerResponse Customer,
    OrderAddressResponse DeliveryAddress,
    string PaymentMethod,
    string? PaymentInstructions,
    string Status,
    DateTimeOffset? PaidAt,
    string? PaymentReference,
    DateTimeOffset? ShippedAt,
    string? ShippingCarrier,
    string? TrackingCode,
    DateTimeOffset? CancelledAt,
    string? CancellationReason,
    DateTimeOffset? RefundedAt,
    string? RefundReference,
    string? RefundReason,
    Guid Revision,
    IReadOnlyList<OrderLineResponse> Lines,
    IReadOnlyList<OrderTotalResponse> Totals,
    OrderDeliveryMethodResponse? DeliveryMethod = null);
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
public sealed record UpdateOrderStatusRequest(
    string Status,
    Guid Revision,
    string? Reason = null,
    string? Carrier = null,
    string? TrackingCode = null,
    string? PaymentReference = null,
    string? RefundReference = null);
public sealed record OrderStatusResponse(
    string Status,
    DateTimeOffset? PaidAt,
    string? PaymentReference,
    DateTimeOffset? ShippedAt,
    string? ShippingCarrier,
    string? TrackingCode,
    DateTimeOffset? CancelledAt,
    string? CancellationReason,
    DateTimeOffset? RefundedAt,
    string? RefundReference,
    string? RefundReason,
    Guid Revision);
