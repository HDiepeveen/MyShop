using MyShop.Application.Catalog.Abstractions;
using MyShop.Application.Catalog.QuoteStorefrontCart;
using MyShop.Application.Checkout.Abstractions;
using MyShop.Application.Checkout.StartOnlinePayment;
using MyShop.Domain.Catalog;

namespace MyShop.Application.Tests.Checkout;

public sealed class StartOnlinePaymentTests
{
    [Fact]
    public async Task StartsPaymentFromFreshServerPricesWithoutCreatingAnOrder()
    {
        var scenario = new Scenario();

        var result = await scenario.Execute();

        Assert.Null(result.Failure);
        Assert.Equal(scenario.Token, result.Payment!.CheckoutToken);
        Assert.Equal("Mollie", result.Payment.ProviderName);
        Assert.Equal(57.50m, Assert.Single(result.Payment.Totals).Amount);
        Assert.Equal("Standaardbezorging", result.Payment.DeliveryMethod.Name);
        Assert.Equal(1, scenario.Products.Calls);
    }

    [Theory]
    [InlineData(false, true)]
    [InlineData(true, false)]
    public async Task RejectsUnavailableOnlinePayment(bool enabled, bool configured)
    {
        var scenario = new Scenario();
        scenario.Payments.Online = enabled;
        scenario.Availability.Configured = configured;

        var result = await scenario.Execute();

        Assert.Equal(StartOnlinePaymentFailure.PaymentUnavailable, result.Failure);
        Assert.Equal(0, scenario.Products.Calls);
    }

    [Fact]
    public async Task RejectsUnavailableDeliveryMethodBeforeQuoting()
    {
        var scenario = new Scenario();
        scenario.DeliveryMethods.Enabled = false;

        var result = await scenario.Execute();

        Assert.Equal(StartOnlinePaymentFailure.DeliveryUnavailable, result.Failure);
        Assert.Equal(0, scenario.Products.Calls);
    }

    [Fact]
    public async Task RejectsPriceChangedSinceDisplayedQuote()
    {
        var scenario = new Scenario();
        scenario.Products.Product.SetVariantPrice(scenario.Products.Product.Variants.Single().Id,
            Money.Create(30, "EUR"));

        var result = await scenario.Execute();

        Assert.Equal(StartOnlinePaymentFailure.CartUnavailable, result.Failure);
    }

    private sealed class Scenario
    {
        public Products Products { get; } = new();
        public Payments Payments { get; } = new();
        public Availability Availability { get; } = new();
        public DeliveryMethods DeliveryMethods { get; } = new();
        public Guid Token { get; } = Guid.NewGuid();

        public Task<StartOnlinePaymentResult> Execute()
        {
            var variant = Products.Product.Variants.Single();
            var useCase = new StartOnlinePayment(new QuoteStorefrontCart(Products), Payments,
                Availability, DeliveryMethods);
            return useCase.ExecuteAsync(new(Token, DeliveryMethods.Id,
                [new(Products.Product.Id.Value, variant.Id.Value, 2, 25m, "EUR")]),
                CancellationToken.None);
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
        public bool Online { get; set; } = true;
        public Task<PaymentOptionsSnapshot> GetAsync(CancellationToken cancellationToken) =>
            Task.FromResult(new PaymentOptionsSnapshot(true, Online,
                "Betaal binnen 14 dagen.", Guid.NewGuid()));
        public Task<PaymentOptionsSnapshot?> SaveAsync(bool payLaterEnabled, bool onlinePaymentEnabled,
            string? payLaterInstructions, Guid expectedRevision,
            CancellationToken cancellationToken) => throw new NotSupportedException();
    }

    private sealed class Availability : IOnlinePaymentAvailability
    {
        public bool Configured { get; set; } = true;
        public bool IsConfigured => Configured;
        public string? ProviderName => Configured ? "Mollie" : null;
    }

    private sealed class DeliveryMethods : IDeliveryMethodRepository
    {
        public Guid Id { get; } = Guid.NewGuid();
        public bool Enabled { get; set; } = true;
        public Task<DeliveryMethodSnapshot?> GetAsync(Guid id, CancellationToken cancellationToken) =>
            Task.FromResult<DeliveryMethodSnapshot?>(id == Id
                ? new(Id, "Standaardbezorging", null, 7.50m, "EUR", Enabled, Guid.NewGuid()) : null);
        public Task<bool> NameExistsAsync(string name, Guid? excludingId, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<IReadOnlyList<DeliveryMethodSnapshot>> ListAsync(bool enabledOnly, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<DeliveryMethodSnapshot> AddAsync(string name, string? description, decimal amount, string currency, bool enabled, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<DeliveryMethodSnapshot?> UpdateAsync(Guid id, string name, string? description, decimal amount, string currency, bool enabled, Guid expectedRevision, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<bool> DeleteAsync(Guid id, Guid expectedRevision, CancellationToken cancellationToken) => throw new NotSupportedException();
    }
}
