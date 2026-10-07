using MyShop.Application.Catalog.QuoteStorefrontCart;
using MyShop.Application.Checkout.Abstractions;
using MyShop.Domain.Catalog;
using MyShop.Domain.Checkout;

namespace MyShop.Application.Checkout.PlaceOrder;

public sealed record PlaceOrderLine(Guid ProductId, Guid VariantId, int Quantity,
    decimal ExpectedAmount, string ExpectedCurrency);
public sealed record PlaceOrderCommand(Guid CheckoutToken, string PaymentMethod, string CustomerName,
    string Email, string AddressLine, string PostalCode, string City, string CountryCode,
    IReadOnlyList<PlaceOrderLine> Lines, string? CustomerUserId = null, Guid DeliveryMethodId = default);
public enum PlaceOrderFailure { CartUnavailable, PaymentUnavailable, OnlinePaymentRequired, DeliveryUnavailable }
public sealed record PlaceOrderResult(OrderReceipt? Receipt, PlaceOrderFailure? Failure)
{
    public static PlaceOrderResult Failed(PlaceOrderFailure failure) => new(null, failure);
}

public sealed class PlaceOrder(QuoteStorefrontCart quoteCart, IPaymentOptionsRepository paymentOptions,
    IOnlinePaymentAvailability onlinePayment, IDeliveryMethodRepository deliveryMethods,
    IOrderRepository orders)
{
    private readonly QuoteStorefrontCart quoteCart = quoteCart ?? throw new ArgumentNullException(nameof(quoteCart));
    private readonly IPaymentOptionsRepository paymentOptions = paymentOptions ?? throw new ArgumentNullException(nameof(paymentOptions));
    private readonly IOnlinePaymentAvailability onlinePayment = onlinePayment ?? throw new ArgumentNullException(nameof(onlinePayment));
    private readonly IOrderRepository orders = orders ?? throw new ArgumentNullException(nameof(orders));
    private readonly IDeliveryMethodRepository deliveryMethods = deliveryMethods
        ?? throw new ArgumentNullException(nameof(deliveryMethods));

    public async Task<PlaceOrderResult> ExecuteAsync(PlaceOrderCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        ArgumentNullException.ThrowIfNull(command.Lines);
        cancellationToken.ThrowIfCancellationRequested();
        if (command.CheckoutToken == Guid.Empty)
            throw new ArgumentException("Checkout token is required.", nameof(command));
        if (command.Lines.Any(line => line is null))
            throw new ArgumentException("Order lines must not contain null values.", nameof(command));
        var existing = await orders.GetByCheckoutTokenAsync(command.CheckoutToken, cancellationToken);
        if (existing is not null) return new(existing, null);
        if (command.Lines.Count is < 1 or > 20)
            throw new ArgumentException("Order must contain 1 to 20 lines.", nameof(command));
        if (command.DeliveryMethodId == Guid.Empty)
            return PlaceOrderResult.Failed(PlaceOrderFailure.DeliveryUnavailable);
        var delivery = await deliveryMethods.GetAsync(command.DeliveryMethodId, cancellationToken);
        if (delivery is null || !delivery.Enabled)
            return PlaceOrderResult.Failed(PlaceOrderFailure.DeliveryUnavailable);

        var settings = await paymentOptions.GetAsync(cancellationToken);
        if (command.PaymentMethod == "online")
            return settings.OnlinePaymentEnabled && onlinePayment.IsConfigured
                ? PlaceOrderResult.Failed(PlaceOrderFailure.OnlinePaymentRequired)
                : PlaceOrderResult.Failed(PlaceOrderFailure.PaymentUnavailable);
        if (command.PaymentMethod != "payLater" || !settings.PayLaterEnabled)
            return PlaceOrderResult.Failed(PlaceOrderFailure.PaymentUnavailable);

        var customer = OrderCustomer.Create(command.CustomerName, command.Email);
        var address = DeliveryAddress.Create(command.AddressLine, command.PostalCode,
            command.City, command.CountryCode);
        var expectedPrices = command.Lines.Select(line =>
            Money.Create(line.ExpectedAmount, line.ExpectedCurrency)).ToArray();
        var at = DateTimeOffset.UtcNow;
        var quote = await quoteCart.ExecuteAsync(new(command.Lines.Select(line =>
            new CartQuoteLine(line.ProductId, line.VariantId, line.Quantity)).ToArray(), at), cancellationToken);
        if (quote.Lines.Count == 0 || quote.Lines.Any(line => line.Failure is not null))
            return PlaceOrderResult.Failed(PlaceOrderFailure.CartUnavailable);
        if (quote.Lines.Where((line, index) => line.Amount != expectedPrices[index].Amount
                || line.Currency != expectedPrices[index].Currency).Any())
            return PlaceOrderResult.Failed(PlaceOrderFailure.CartUnavailable);
        var order = Order.Place(Guid.NewGuid(), at, customer, address,
            quote.Lines.Select(line => (line.ProductId, line.VariantId, line.Name!, line.Variant!,
                line.Quantity, Money.Create(line.Amount!.Value, line.Currency!))),
            settings.PayLaterInstructions, new OrderDeliveryMethod(delivery.Id, delivery.Name,
                delivery.Description, Money.Create(delivery.Amount, delivery.Currency)),
            vatRates: quote.Lines.Where(line => line.VatRate is not null).ToDictionary(line => (line.ProductId, line.VariantId), line => (line.VatRate!.Value, line.VatExempt)));
        var receipt = await orders.AddAsync(order, command.CheckoutToken, command.CustomerUserId,
            quote.Lines.Select(line => new StockReservation(line.ProductId, line.VariantId,
                line.Quantity)).ToArray(), cancellationToken);
        return receipt is null
            ? PlaceOrderResult.Failed(PlaceOrderFailure.CartUnavailable)
            : new(receipt, null);
    }
}
