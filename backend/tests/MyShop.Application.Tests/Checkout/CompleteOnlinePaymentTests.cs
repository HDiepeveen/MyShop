using MyShop.Application.Checkout.Abstractions;
using MyShop.Application.Checkout.CompleteOnlinePayment;
using MyShop.Domain.Checkout;

namespace MyShop.Application.Tests.Checkout;

public sealed class CompleteOnlinePaymentTests
{
    [Fact]
    public async Task CreatesPaidOnlineOrderFromStoredPaymentStart()
    {
        var scenario = new Scenario();
        var result = await scenario.Execute();

        Assert.Null(result.Failure);
        Assert.Equal(scenario.Orders.Saved!.Id, result.Receipt!.Id);
        Assert.Equal(OrderPaymentMethod.Online, scenario.Orders.Saved.PaymentMethod);
        Assert.Equal("Ada Lovelace", scenario.Orders.Saved.Customer.Name);
        Assert.Equal("Standaardbezorging", scenario.Orders.Saved.DeliveryMethod!.Name);
        Assert.Equal(2, Assert.Single(scenario.Orders.Saved.Lines).Quantity);
        Assert.Equal(57.50m, Assert.Single(result.Receipt.Totals).Amount);
        Assert.Equal(scenario.Payment.PaymentReference, scenario.Orders.PaymentReference);
        Assert.Equal(scenario.Token, scenario.Orders.Token);
    }

    [Fact]
    public async Task RepeatedCheckoutTokenReturnsExistingReceiptWithoutLoadingPaymentStart()
    {
        var scenario = new Scenario();
        scenario.Orders.Existing = new(Guid.NewGuid(), "MS-EXISTING", DateTimeOffset.UtcNow,
            null, [new("EUR", 42m)], null);

        var result = await scenario.Execute();

        Assert.Equal("MS-EXISTING", result.Receipt!.Number);
        Assert.Equal(0, scenario.PaymentStarts.Calls);
        Assert.Null(scenario.Orders.Saved);
    }

    [Fact]
    public async Task RejectsMissingPaymentStart()
    {
        var scenario = new Scenario();
        scenario.PaymentStarts.Payment = null;

        var result = await scenario.Execute();

        Assert.Equal(CompleteOnlinePaymentFailure.PaymentNotFound, result.Failure);
        Assert.Null(scenario.Orders.Saved);
    }

    [Fact]
    public async Task RejectsProviderPaymentMismatch()
    {
        var scenario = new Scenario { ProviderPaymentId = "different" };

        var result = await scenario.Execute();

        Assert.Equal(CompleteOnlinePaymentFailure.PaymentMismatch, result.Failure);
        Assert.Null(scenario.Orders.Saved);
    }

    [Fact]
    public async Task RejectsWhenStockCannotBeReserved()
    {
        var scenario = new Scenario();
        scenario.Orders.RejectStock = true;

        var result = await scenario.Execute();

        Assert.Equal(CompleteOnlinePaymentFailure.CartUnavailable, result.Failure);
    }

    private sealed class Scenario
    {
        public Guid Token { get; } = Guid.NewGuid();
        public string ProviderPaymentId { get; init; } = "test_payment";
        public OnlinePaymentStartRecord Payment => new(Token, "test", "MSP-123", "test_payment",
            new Uri("https://example.com/pay"), new("Ada Lovelace", "ada@example.com"),
            new("Main street 1", "1234 AB", "Amsterdam", "NL"),
            [new(Guid.NewGuid(), Guid.NewGuid(), "Shirt", "Small", 2, 25m, "EUR", 50m)],
            [new("EUR", 57.50m)],
            new(Guid.NewGuid(), "Standaardbezorging", null, 7.50m, "EUR"),
            DateTimeOffset.UtcNow);
        public PaymentStarts PaymentStarts { get; }
        public Orders Orders { get; } = new();

        public Scenario() => PaymentStarts = new(Payment);

        public Task<CompleteOnlinePaymentResult> Execute()
        {
            var useCase = new CompleteOnlinePayment(PaymentStarts, Orders);
            return useCase.ExecuteAsync(new(Token, ProviderPaymentId), CancellationToken.None);
        }
    }

    private sealed class PaymentStarts(OnlinePaymentStartRecord? payment) : IOnlinePaymentStartRepository
    {
        public OnlinePaymentStartRecord? Payment { get; set; } = payment;
        public int Calls { get; private set; }
        public Task<OnlinePaymentStartRecord?> GetByCheckoutTokenAsync(Guid checkoutToken,
            CancellationToken cancellationToken)
        {
            Calls++;
            return Task.FromResult(Payment?.CheckoutToken == checkoutToken ? Payment : null);
        }
        public Task SaveAsync(OnlinePaymentStartRecord payment, CancellationToken cancellationToken) =>
            throw new NotSupportedException();
    }

    private sealed class Orders : IOrderRepository
    {
        public OrderReceipt? Existing { get; set; }
        public Order? Saved { get; private set; }
        public Guid Token { get; private set; }
        public string? PaymentReference { get; private set; }
        public bool RejectStock { get; set; }
        public Task<OrderReceipt?> GetByCheckoutTokenAsync(Guid checkoutToken, CancellationToken cancellationToken) =>
            Task.FromResult(Existing);
        public Task<OrderReceipt?> AddAsync(Order order, Guid checkoutToken, string? customerUserId,
            IReadOnlyList<StockReservation> stock, CancellationToken cancellationToken) =>
            throw new NotSupportedException();
        public Task<OrderReceipt?> AddPaidAsync(Order order, Guid checkoutToken, string paymentReference,
            IReadOnlyList<StockReservation> stock, CancellationToken cancellationToken)
        {
            if (RejectStock) return Task.FromResult<OrderReceipt?>(null);
            Saved = order; Token = checkoutToken; PaymentReference = paymentReference;
            return Task.FromResult<OrderReceipt?>(new OrderReceipt(order.Id, order.Number,
                order.PlacedAt, order.PaymentInstructions,
                order.Totals.Select(total => new OrderTotalSnapshot(total.Currency, total.Amount)).ToArray(),
                order.DeliveryMethod is null ? null : new OrderDeliveryMethodSnapshot(order.DeliveryMethod.Id,
                    order.DeliveryMethod.Name, order.DeliveryMethod.Description, order.DeliveryMethod.Fee.Amount,
                    order.DeliveryMethod.Fee.Currency)));
        }
    }
}
