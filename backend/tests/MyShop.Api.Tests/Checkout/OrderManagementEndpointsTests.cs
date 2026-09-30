using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using MyShop.Api.Checkout;
using MyShop.Application.Checkout.Abstractions;
using MyShop.Domain.Checkout;
using GetUseCase = MyShop.Application.Checkout.GetOrder.GetOrder;
using ListUseCase = MyShop.Application.Checkout.ListOrders.ListOrders;
using MyShop.Application.Checkout.MarkOrderPaid;
using MyShop.Application.Checkout.MarkOrderShipped;
using MyShop.Application.Checkout.CancelOrder;
using MyShop.Application.Checkout.RefundOrder;

namespace MyShop.Api.Tests.Checkout;

public sealed class OrderManagementEndpointsTests
{
    [Fact]
    public void MapOrderManagement_MapsReadAndStatusRoutes()
    {
        var app = WebApplication.CreateBuilder().Build();
        Assert.Same(app, app.MapOrderManagement());
        var endpoints = ((IEndpointRouteBuilder)app).DataSources.SelectMany(source => source.Endpoints).Cast<RouteEndpoint>().ToArray();
        Assert.Equal(["/api/orders", "/api/orders/{id:guid}", "/api/orders/{id:guid}/status"],
            endpoints.Select(item => item.RoutePattern.RawText));
        Assert.Equal(["GET", "GET", "PUT"],
            endpoints.Select(item => Assert.Single(item.Metadata.GetMetadata<IHttpMethodMetadata>()!.HttpMethods)));
    }

    [Fact]
    public async Task ListAsync_MapsStableNamesAndExactMoneyStrings()
    {
        var item = new OrderListItem(Guid.NewGuid(), "MS-1", DateTimeOffset.UtcNow, "Ada",
            OrderPaymentMethod.PayLater, OrderStatus.AwaitingPayment, null, null,
            null, null, null, null, [new("EUR", 12.5m)]);
        var repository = new Fake(new([item], 21), null);
        var result = await OrderManagementEndpoints.ListAsync(5, 10, "paid", "  Ada  ",
            new ListUseCase(repository), CancellationToken.None);
        var response = Assert.IsType<Ok<OrderListResponse>>(result.Result).Value!;
        Assert.Equal((5, 10, 21), (response.Offset, response.Limit, response.TotalCount));
        var mapped = Assert.Single(response.Items);
        Assert.Equal("payLater", mapped.PaymentMethod);
        Assert.Equal("awaitingPayment", mapped.Status);
        Assert.Equal("12.50", Assert.Single(mapped.Totals).Amount);
        Assert.Equal(OrderStatus.Paid, repository.ListStatus);
        Assert.Equal("Ada", repository.ListSearch);
    }

    [Fact]
    public async Task ListAsync_RejectsUnsupportedStatusWithoutReading()
    {
        var repository = new Fake(new([], 0), null);
        var result = await OrderManagementEndpoints.ListAsync(0, 20, "returned", null,
            new ListUseCase(repository), CancellationToken.None);
        Assert.IsType<BadRequest<ProblemDetails>>(result.Result);
        Assert.Equal(0, repository.ListCalls);
    }

    [Fact]
    public async Task ListAsync_RejectsOverlongSearchWithoutReading()
    {
        var repository = new Fake(new([], 0), null);
        var result = await OrderManagementEndpoints.ListAsync(0, 20, null,
            new string('x', 201), new ListUseCase(repository), CancellationToken.None);
        Assert.IsType<BadRequest<ProblemDetails>>(result.Result);
        Assert.Equal(0, repository.ListCalls);
    }

    [Fact]
    public async Task GetAsync_MapsDetailAndReturnsNotFound()
    {
        var id = Guid.NewGuid();
        var detail = new OrderDetail(id, "MS-1", DateTimeOffset.UtcNow, "Ada", "ada@example.test",
            "Straat 1", "1234 AB", "Utrecht", "NL", OrderPaymentMethod.PayLater,
            "Betaal binnen 14 dagen.",
            OrderStatus.AwaitingPayment, null, null, null, null, null, null, null,
            null, null, null, Guid.NewGuid(),
            [new(Guid.NewGuid(), Guid.NewGuid(), "Shirt", "Blauw", 2, 3.5m, "EUR", 7m)], [new("EUR", 7m)]);
        var ok = await OrderManagementEndpoints.GetAsync(id, new GetUseCase(new Fake(new([], 0), detail)), CancellationToken.None);
        var response = Assert.IsType<Ok<OrderDetailResponse>>(ok.Result).Value!;
        Assert.Equal("3.50", Assert.Single(response.Lines).UnitAmount);
        Assert.Equal("7.00", Assert.Single(response.Totals).Amount);
        Assert.Equal("Betaal binnen 14 dagen.", response.PaymentInstructions);
        var missing = await OrderManagementEndpoints.GetAsync(Guid.NewGuid(), new GetUseCase(new Fake(new([], 0), null)), CancellationToken.None);
        Assert.IsType<NotFound>(missing.Result);
    }

    [Fact]
    public async Task UpdateStatusAsync_MarksOrderPaidAndMapsStableResponse()
    {
        var id = Guid.NewGuid();
        var revision = Guid.NewGuid();
        var detail = new OrderDetail(id, "MS-1", DateTimeOffset.UtcNow, "Ada", "ada@example.test",
            "Straat 1", "1234 AB", "Utrecht", "NL", OrderPaymentMethod.PayLater, null,
            OrderStatus.AwaitingPayment, null, null, null, null, null, null, null,
            null, null, null, revision, [], []);
        var repository = new Fake(new([], 0), detail);
        var result = await OrderManagementEndpoints.UpdateStatusAsync(id,
            new("paid", revision, PaymentReference: "  bankafschrift 12345  "), new MarkOrderPaid(repository),
            new MarkOrderShipped(repository), new CancelOrder(repository),
            new RefundOrder(repository), CancellationToken.None);
        var response = Assert.IsType<Ok<OrderStatusResponse>>(result).Value!;
        Assert.Equal("paid", response.Status);
        Assert.NotNull(response.PaidAt);
        Assert.Equal("bankafschrift 12345", response.PaymentReference);
        Assert.Equal(response.PaymentReference, repository.PaymentReference);
        Assert.Null(response.ShippedAt);
        Assert.NotEqual(revision, response.Revision);
    }

    [Fact]
    public async Task UpdateStatusAsync_MarksPaidOrderShipped()
    {
        var id = Guid.NewGuid();
        var revision = Guid.NewGuid();
        var paidAt = DateTimeOffset.UtcNow.AddMinutes(-5);
        var detail = new OrderDetail(id, "MS-1", DateTimeOffset.UtcNow, "Ada", "ada@example.test",
            "Straat 1", "1234 AB", "Utrecht", "NL", OrderPaymentMethod.PayLater, null,
            OrderStatus.Paid, paidAt, "bankafschrift 12345", null, null, null, null, null,
            null, null, null, revision, [], []);
        var repository = new Fake(new([], 0), detail);
        var result = await OrderManagementEndpoints.UpdateStatusAsync(id,
            new("shipped", revision, Carrier: "  PostNL  ", TrackingCode: "  3SMYSHOP123  "),
            new MarkOrderPaid(repository),
            new MarkOrderShipped(repository), new CancelOrder(repository),
            new RefundOrder(repository), CancellationToken.None);
        var response = Assert.IsType<Ok<OrderStatusResponse>>(result).Value!;
        Assert.Equal("shipped", response.Status);
        Assert.Equal(paidAt, response.PaidAt);
        Assert.Equal("bankafschrift 12345", response.PaymentReference);
        Assert.NotNull(response.ShippedAt);
        Assert.Equal("PostNL", response.ShippingCarrier);
        Assert.Equal("3SMYSHOP123", response.TrackingCode);
    }

    [Fact]
    public async Task UpdateStatusAsync_CancelsAwaitingOrderWithReason()
    {
        var id = Guid.NewGuid();
        var revision = Guid.NewGuid();
        var detail = new OrderDetail(id, "MS-1", DateTimeOffset.UtcNow, "Ada", "ada@example.test",
            "Straat 1", "1234 AB", "Utrecht", "NL", OrderPaymentMethod.PayLater, null,
            OrderStatus.AwaitingPayment, null, null, null, null, null, null, null,
            null, null, null, revision, [], []);
        var repository = new Fake(new([], 0), detail);
        var result = await OrderManagementEndpoints.UpdateStatusAsync(id,
            new("cancelled", revision, "Klant ziet af van bestelling."),
            new MarkOrderPaid(repository), new MarkOrderShipped(repository),
            new CancelOrder(repository), new RefundOrder(repository), CancellationToken.None);
        var response = Assert.IsType<Ok<OrderStatusResponse>>(result).Value!;
        Assert.Equal("cancelled", response.Status);
        Assert.Equal("Klant ziet af van bestelling.", response.CancellationReason);
        Assert.NotNull(response.CancelledAt);
        Assert.Equal(response.CancellationReason, repository.Reason);
    }

    [Fact]
    public async Task UpdateStatusAsync_RefundsPaidOrderWithDetails()
    {
        var id = Guid.NewGuid();
        var revision = Guid.NewGuid();
        var paidAt = DateTimeOffset.UtcNow.AddMinutes(-5);
        var detail = new OrderDetail(id, "MS-1", DateTimeOffset.UtcNow, "Ada", "ada@example.test",
            "Straat 1", "1234 AB", "Utrecht", "NL", OrderPaymentMethod.PayLater, null,
            OrderStatus.Paid, paidAt, "bankafschrift 12345", null, null, null, null, null,
            null, null, null, revision, [], []);
        var repository = new Fake(new([], 0), detail);
        var result = await OrderManagementEndpoints.UpdateStatusAsync(id,
            new("refunded", revision, Reason: "  Dubbele betaling.  ",
                RefundReference: "  bankafschrift 67890  "),
            new MarkOrderPaid(repository), new MarkOrderShipped(repository),
            new CancelOrder(repository), new RefundOrder(repository), CancellationToken.None);
        var response = Assert.IsType<Ok<OrderStatusResponse>>(result).Value!;
        Assert.Equal("refunded", response.Status);
        Assert.Equal(paidAt, response.PaidAt);
        Assert.NotNull(response.RefundedAt);
        Assert.Equal("bankafschrift 67890", response.RefundReference);
        Assert.Equal("Dubbele betaling.", response.RefundReason);
        Assert.Equal(response.RefundReference, repository.RefundReference);
        Assert.Equal(response.RefundReason, repository.Reason);
    }

    [Fact]
    public async Task UpdateStatusAsync_RejectsUnsupportedStatusWithoutWriting()
    {
        var repository = new Fake(new([], 0), null);
        var result = await OrderManagementEndpoints.UpdateStatusAsync(Guid.NewGuid(),
            new("returned", Guid.NewGuid()), new MarkOrderPaid(repository),
            new MarkOrderShipped(repository), new CancelOrder(repository),
            new RefundOrder(repository), CancellationToken.None);
        Assert.IsAssignableFrom<IStatusCodeHttpResult>(result);
        Assert.Equal(StatusCodes.Status400BadRequest, ((IStatusCodeHttpResult)result).StatusCode);
        Assert.Equal(0, repository.WriteCalls);
    }

    private sealed class Fake(OrderListPage page, OrderDetail? detail) : IOrderReadRepository, IOrderStatusRepository
    {
        public int WriteCalls { get; private set; }
        public int ListCalls { get; private set; }
        public OrderStatus? ListStatus { get; private set; }
        public string? ListSearch { get; private set; }
        public string? Reason { get; private set; }
        public string? Carrier { get; private set; }
        public string? TrackingCode { get; private set; }
        public string? PaymentReference { get; private set; }
        public string? RefundReference { get; private set; }
        public Task<OrderListPage> ListAsync(int offset, int limit, OrderStatus? status, string? search,
            CancellationToken cancellationToken)
        {
            ListCalls++; ListStatus = status; ListSearch = search; return Task.FromResult(page);
        }
        public Task<OrderDetail?> GetAsync(Guid id, CancellationToken cancellationToken) => Task.FromResult(detail);
        public Task<OrderStatusSnapshot?> GetStatusAsync(Guid id, CancellationToken cancellationToken) =>
            Task.FromResult(detail is null ? null : new OrderStatusSnapshot(
                detail.Status, detail.PaidAt, detail.PaymentReference, detail.ShippedAt,
                detail.ShippingCarrier, detail.TrackingCode, detail.CancelledAt,
                detail.CancellationReason, detail.RefundedAt, detail.RefundReference,
                detail.RefundReason, detail.Revision));
        public Task<Guid?> MarkPaidAsync(Guid id, Guid expectedRevision,
            DateTimeOffset paidAt, string paymentReference, CancellationToken cancellationToken)
        {
            WriteCalls++; PaymentReference = paymentReference;
            return Task.FromResult<Guid?>(Guid.NewGuid());
        }
        public Task<Guid?> MarkShippedAsync(Guid id, Guid expectedRevision,
            DateTimeOffset shippedAt, string carrier, string trackingCode,
            CancellationToken cancellationToken)
        {
            WriteCalls++; Carrier = carrier; TrackingCode = trackingCode;
            return Task.FromResult<Guid?>(Guid.NewGuid());
        }
        public Task<Guid?> CancelAsync(Guid id, Guid expectedRevision, DateTimeOffset cancelledAt,
            string reason, CancellationToken cancellationToken)
        {
            WriteCalls++; Reason = reason;
            return Task.FromResult<Guid?>(Guid.NewGuid());
        }
        public Task<Guid?> RefundAsync(Guid id, Guid expectedRevision, DateTimeOffset refundedAt,
            string refundReference, string reason, CancellationToken cancellationToken)
        {
            WriteCalls++; RefundReference = refundReference; Reason = reason;
            return Task.FromResult<Guid?>(Guid.NewGuid());
        }
    }
}
