using System.Globalization;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using MyShop.Application.Checkout.Abstractions;
using MyShop.Domain.Checkout;
using GetUseCase = MyShop.Application.Checkout.GetOrder.GetOrder;
using ListUseCase = MyShop.Application.Checkout.ListOrders.ListOrders;
using MyShop.Application.Checkout.ListOrders;

namespace MyShop.Api.Checkout;

public static class OrderManagementEndpoints
{
    public static IEndpointRouteBuilder MapOrderManagement(this IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);
        endpoints.MapGet("/api/orders", ListAsync).WithName("ListOrders");
        endpoints.MapGet("/api/orders/{id:guid}", GetAsync).WithName("GetOrder");
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

    private static OrderSummaryResponse MapSummary(OrderListItem order) => new(
        order.Id,
        order.Number,
        order.PlacedAt,
        order.CustomerName,
        PaymentMethod(order.PaymentMethod),
        Status(order.Status),
        order.Totals.Select(MapTotal).ToArray());

    private static OrderDetailResponse MapDetail(OrderDetail order) => new(
        order.Id,
        order.Number,
        order.PlacedAt,
        new(order.CustomerName, order.Email),
        new(order.AddressLine, order.PostalCode, order.City, order.CountryCode),
        PaymentMethod(order.PaymentMethod),
        Status(order.Status),
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
    IReadOnlyList<OrderTotalResponse> Totals);
public sealed record OrderDetailResponse(
    Guid Id,
    string Number,
    DateTimeOffset PlacedAt,
    OrderCustomerResponse Customer,
    OrderAddressResponse DeliveryAddress,
    string PaymentMethod,
    string Status,
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
