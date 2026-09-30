using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;
using MyShop.Api.Checkout;
using MyShop.Application.Checkout.Abstractions;
using MyShop.Domain.Checkout;
using GetUseCase = MyShop.Application.Checkout.GetOrder.GetOrder;
using ListUseCase = MyShop.Application.Checkout.ListOrders.ListOrders;

namespace MyShop.Api.Tests.Checkout;

public sealed class OrderManagementEndpointsTests
{
    [Fact]
    public void MapOrderManagement_MapsTwoNamedGetRoutes()
    {
        var app = WebApplication.CreateBuilder().Build();
        Assert.Same(app, app.MapOrderManagement());
        var endpoints = ((IEndpointRouteBuilder)app).DataSources.SelectMany(source => source.Endpoints).Cast<RouteEndpoint>().ToArray();
        Assert.Equal(["/api/orders", "/api/orders/{id:guid}"], endpoints.Select(item => item.RoutePattern.RawText));
        Assert.All(endpoints, item => Assert.Equal(["GET"], item.Metadata.GetMetadata<IHttpMethodMetadata>()!.HttpMethods));
    }

    [Fact]
    public async Task ListAsync_MapsStableNamesAndExactMoneyStrings()
    {
        var item = new OrderListItem(Guid.NewGuid(), "MS-1", DateTimeOffset.UtcNow, "Ada",
            OrderPaymentMethod.PayLater, OrderStatus.AwaitingPayment, [new("EUR", 12.5m)]);
        var result = await OrderManagementEndpoints.ListAsync(5, 10,
            new ListUseCase(new Fake(new([item], 21), null)), CancellationToken.None);
        var response = Assert.IsType<Ok<OrderListResponse>>(result.Result).Value!;
        Assert.Equal((5, 10, 21), (response.Offset, response.Limit, response.TotalCount));
        var mapped = Assert.Single(response.Items);
        Assert.Equal("payLater", mapped.PaymentMethod);
        Assert.Equal("awaitingPayment", mapped.Status);
        Assert.Equal("12.50", Assert.Single(mapped.Totals).Amount);
    }

    [Fact]
    public async Task GetAsync_MapsDetailAndReturnsNotFound()
    {
        var id = Guid.NewGuid();
        var detail = new OrderDetail(id, "MS-1", DateTimeOffset.UtcNow, "Ada", "ada@example.test",
            "Straat 1", "1234 AB", "Utrecht", "NL", OrderPaymentMethod.PayLater,
            OrderStatus.AwaitingPayment, [new(Guid.NewGuid(), Guid.NewGuid(), "Shirt", "Blauw", 2, 3.5m, "EUR", 7m)], [new("EUR", 7m)]);
        var ok = await OrderManagementEndpoints.GetAsync(id, new GetUseCase(new Fake(new([], 0), detail)), CancellationToken.None);
        var response = Assert.IsType<Ok<OrderDetailResponse>>(ok.Result).Value!;
        Assert.Equal("3.50", Assert.Single(response.Lines).UnitAmount);
        Assert.Equal("7.00", Assert.Single(response.Totals).Amount);
        var missing = await OrderManagementEndpoints.GetAsync(Guid.NewGuid(), new GetUseCase(new Fake(new([], 0), null)), CancellationToken.None);
        Assert.IsType<NotFound>(missing.Result);
    }

    private sealed class Fake(OrderListPage page, OrderDetail? detail) : IOrderReadRepository
    {
        public Task<OrderListPage> ListAsync(int offset, int limit, CancellationToken cancellationToken) => Task.FromResult(page);
        public Task<OrderDetail?> GetAsync(Guid id, CancellationToken cancellationToken) => Task.FromResult(detail);
    }
}
