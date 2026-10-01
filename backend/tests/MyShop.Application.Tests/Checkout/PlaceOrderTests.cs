using MyShop.Application.Catalog.Abstractions;
using MyShop.Application.Catalog.QuoteStorefrontCart;
using MyShop.Application.Checkout.Abstractions;
using MyShop.Application.Checkout.PlaceOrder;
using MyShop.Domain.Catalog;
using MyShop.Domain.Checkout;

namespace MyShop.Application.Tests.Checkout;

public sealed class PlaceOrderTests
{
    [Fact]
    public async Task PlacesPayLaterOrderFromFreshServerPrices()
    {
        var scenario = new Scenario();
        var result = await scenario.Execute();

        Assert.Null(result.Failure);
        Assert.Equal(scenario.Orders.Saved!.Id, result.Receipt!.Id);
        Assert.Equal(2, Assert.Single(scenario.Orders.Saved.Lines).Quantity);
        Assert.Equal(25m, Assert.Single(scenario.Orders.Saved.Lines).UnitPrice.Amount);
        Assert.Equal(50m, Assert.Single(scenario.Orders.Saved.Totals).Amount);
        Assert.Equal("Ada", scenario.Orders.Saved.Customer.Name);
        Assert.Equal("Betaal binnen 14 dagen.", scenario.Orders.Saved.PaymentInstructions);
        Assert.Equal(scenario.Orders.Saved.PaymentInstructions, result.Receipt.PaymentInstructions);
        Assert.Equal(scenario.Token, scenario.Orders.Token);
        Assert.Equal("customer-user", scenario.Orders.CustomerUserId);
    }

    [Fact]
    public async Task RepeatedCheckoutTokenReturnsExistingReceiptWithoutRepricing()
    {
        var scenario = new Scenario();
        scenario.Orders.Existing = new(Guid.NewGuid(), "MS-EXISTING", DateTimeOffset.UtcNow,
            "Bestaande instructies");

        var result = await scenario.Execute();

        Assert.Equal("MS-EXISTING", result.Receipt!.Number);
        Assert.Equal("Bestaande instructies", result.Receipt.PaymentInstructions);
        Assert.Equal(0, scenario.Products.Calls);
        Assert.Null(scenario.Orders.Saved);
    }

    [Theory]
    [InlineData("missing", false, PlaceOrderFailure.PaymentUnavailable)]
    [InlineData("payLater", false, PlaceOrderFailure.PaymentUnavailable)]
    [InlineData("online", true, PlaceOrderFailure.PaymentUnavailable)]
    public async Task RejectsUnavailablePaymentMethod(string method, bool payLater,
        PlaceOrderFailure failure)
    {
        var scenario = new Scenario { PaymentMethod = method };
        scenario.Payments.PayLater = payLater;

        var result = await scenario.Execute();

        Assert.Equal(failure, result.Failure);
        Assert.Equal(0, scenario.Products.Calls);
    }

    [Fact]
    public async Task RejectsCartThatCannotBeQuotedWithoutSavingCustomerData()
    {
        var scenario = new Scenario();
        scenario.Products.Product.SetPresentation(ProductPresentation.Draft);

        var result = await scenario.Execute();

        Assert.Equal(PlaceOrderFailure.CartUnavailable, result.Failure);
        Assert.Null(scenario.Orders.Saved);
    }

    [Fact]
    public async Task RejectsPriceChangedSinceTheDisplayedQuote()
    {
        var scenario = new Scenario();
        scenario.Products.Product.SetVariantPrice(scenario.Products.Product.Variants.Single().Id,
            Money.Create(30, "EUR"));

        var result = await scenario.Execute();

        Assert.Equal(PlaceOrderFailure.CartUnavailable, result.Failure);
        Assert.Null(scenario.Orders.Saved);
    }

    [Fact]
    public async Task RejectsWhenStockChangesDuringAtomicPlacement()
    {
        var scenario = new Scenario();
        scenario.Orders.RejectStock = true;
        var result = await scenario.Execute();
        Assert.Equal(PlaceOrderFailure.CartUnavailable, result.Failure);
    }

    private sealed class Scenario
    {
        public Products Products { get; } = new();
        public Payments Payments { get; } = new();
        public Orders Orders { get; } = new();
        public Guid Token { get; } = Guid.NewGuid();
        public string PaymentMethod { get; init; } = "payLater";

        public Task<PlaceOrderResult> Execute()
        {
            var variant = Products.Product.Variants.Single();
            var useCase = new PlaceOrder(new QuoteStorefrontCart(Products), Payments,
                new Availability(), Orders);
            return useCase.ExecuteAsync(new(Token, PaymentMethod, " Ada ", "ada@example.com",
                "Street 1", "1234 AB", "Amsterdam", "NL",
                [new(Products.Product.Id.Value, variant.Id.Value, 2, 25m, "EUR")], "customer-user"), CancellationToken.None);
        }
    }

    private sealed class Products : IProductRepository
    {
        public Product Product { get; } = Product.Create("Shirt", ProductTypeId.New(), "Small");
        public int Calls { get; private set; }
        public Products()
        {
            Product.SetPresentation(ProductPresentation.Create("Shirt", "https://example.com/shirt.jpg", "Shirt", true));
            Product.SetVariantPrice(Product.Variants.Single().Id, Money.Create(25, "EUR"));
        }
        public Task<ProductSnapshot?> GetByIdAsync(ProductId id, CancellationToken cancellationToken)
        {
            Calls++;
            return Task.FromResult<ProductSnapshot?>(id == Product.Id
                ? new(Product, ProductConcurrencyToken.Create(Product.Id, Guid.NewGuid())) : null);
        }
        public Task<ProductConcurrencyToken> AddAsync(Product product, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<ProductConcurrencyToken> SaveAsync(Product product, ProductConcurrencyToken expectedToken, CancellationToken cancellationToken) => throw new NotSupportedException();
    }

    private sealed class Payments : IPaymentOptionsRepository
    {
        public bool PayLater { get; set; } = true;
        public Task<PaymentOptionsSnapshot> GetAsync(CancellationToken cancellationToken) =>
            Task.FromResult(new PaymentOptionsSnapshot(PayLater, false,
                "Betaal binnen 14 dagen.", Guid.NewGuid()));
        public Task<PaymentOptionsSnapshot?> SaveAsync(bool payLaterEnabled, bool onlinePaymentEnabled,
            string? payLaterInstructions, Guid expectedRevision,
            CancellationToken cancellationToken) => throw new NotSupportedException();
    }

    private sealed class Availability : IOnlinePaymentAvailability { public bool IsConfigured => false; }

    private sealed class Orders : IOrderRepository
    {
        public OrderReceipt? Existing { get; set; }
        public Order? Saved { get; private set; }
        public Guid Token { get; private set; }
        public string? CustomerUserId { get; private set; }
        public bool RejectStock { get; set; }
        public Task<OrderReceipt?> GetByCheckoutTokenAsync(Guid checkoutToken, CancellationToken cancellationToken) =>
            Task.FromResult(Existing);
        public Task<OrderReceipt?> AddAsync(Order order, Guid checkoutToken, string? customerUserId,
            IReadOnlyList<StockReservation> stock, CancellationToken cancellationToken)
        {
            if (RejectStock) return Task.FromResult<OrderReceipt?>(null);
            Saved = order; Token = checkoutToken; CustomerUserId = customerUserId;
            return Task.FromResult<OrderReceipt?>(new OrderReceipt(order.Id, order.Number,
                order.PlacedAt, order.PaymentInstructions));
        }
    }
}
