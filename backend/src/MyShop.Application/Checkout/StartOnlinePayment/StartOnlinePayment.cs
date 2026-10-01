using MyShop.Application.Catalog.QuoteStorefrontCart;
using MyShop.Application.Checkout.Abstractions;
using MyShop.Domain.Catalog;

namespace MyShop.Application.Checkout.StartOnlinePayment;

public sealed record StartOnlinePaymentLine(Guid ProductId, Guid VariantId, int Quantity,
    decimal ExpectedAmount, string ExpectedCurrency);
public sealed record StartOnlinePaymentCommand(Guid CheckoutToken, Guid DeliveryMethodId,
    IReadOnlyList<StartOnlinePaymentLine> Lines);
public enum StartOnlinePaymentFailure { CartUnavailable, PaymentUnavailable, DeliveryUnavailable }
public sealed record OnlinePaymentStart(Guid CheckoutToken, string ProviderName, string PaymentReference,
    IReadOnlyList<OrderTotalSnapshot> Totals, OrderDeliveryMethodSnapshot DeliveryMethod);
public sealed record StartOnlinePaymentResult(OnlinePaymentStart? Payment, StartOnlinePaymentFailure? Failure)
{
    public static StartOnlinePaymentResult Failed(StartOnlinePaymentFailure failure) => new(null, failure);
}

public sealed class StartOnlinePayment(QuoteStorefrontCart quoteCart, IPaymentOptionsRepository paymentOptions,
    IOnlinePaymentAvailability onlinePayment, IDeliveryMethodRepository deliveryMethods)
{
    private readonly QuoteStorefrontCart quoteCart = quoteCart ?? throw new ArgumentNullException(nameof(quoteCart));
    private readonly IPaymentOptionsRepository paymentOptions = paymentOptions ?? throw new ArgumentNullException(nameof(paymentOptions));
    private readonly IOnlinePaymentAvailability onlinePayment = onlinePayment ?? throw new ArgumentNullException(nameof(onlinePayment));
    private readonly IDeliveryMethodRepository deliveryMethods = deliveryMethods
        ?? throw new ArgumentNullException(nameof(deliveryMethods));

    public async Task<StartOnlinePaymentResult> ExecuteAsync(StartOnlinePaymentCommand command,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        ArgumentNullException.ThrowIfNull(command.Lines);
        cancellationToken.ThrowIfCancellationRequested();
        if (command.CheckoutToken == Guid.Empty)
            throw new ArgumentException("Checkout token is required.", nameof(command));
        if (command.Lines.Any(line => line is null))
            throw new ArgumentException("Order lines must not contain null values.", nameof(command));
        if (command.Lines.Count is < 1 or > 20)
            throw new ArgumentException("Order must contain 1 to 20 lines.", nameof(command));
        if (command.DeliveryMethodId == Guid.Empty)
            return StartOnlinePaymentResult.Failed(StartOnlinePaymentFailure.DeliveryUnavailable);

        var settings = await paymentOptions.GetAsync(cancellationToken);
        if (!settings.OnlinePaymentEnabled || !onlinePayment.IsConfigured || onlinePayment.ProviderName is null)
            return StartOnlinePaymentResult.Failed(StartOnlinePaymentFailure.PaymentUnavailable);

        var delivery = await deliveryMethods.GetAsync(command.DeliveryMethodId, cancellationToken);
        if (delivery is null || !delivery.Enabled)
            return StartOnlinePaymentResult.Failed(StartOnlinePaymentFailure.DeliveryUnavailable);

        var expectedPrices = command.Lines.Select(line =>
            Money.Create(line.ExpectedAmount, line.ExpectedCurrency)).ToArray();
        var quote = await quoteCart.ExecuteAsync(new(command.Lines.Select(line =>
            new CartQuoteLine(line.ProductId, line.VariantId, line.Quantity)).ToArray(),
            DateTimeOffset.UtcNow), cancellationToken);
        if (quote.Lines.Count == 0 || quote.Lines.Any(line => line.Failure is not null))
            return StartOnlinePaymentResult.Failed(StartOnlinePaymentFailure.CartUnavailable);
        if (quote.Lines.Where((line, index) => line.Amount != expectedPrices[index].Amount
                || line.Currency != expectedPrices[index].Currency).Any())
            return StartOnlinePaymentResult.Failed(StartOnlinePaymentFailure.CartUnavailable);

        var totals = quote.Totals.Select(total => Money.Create(total.Amount, total.Currency))
            .Concat([Money.Create(delivery.Amount, delivery.Currency)])
            .GroupBy(total => total.Currency)
            .Select(group => new OrderTotalSnapshot(group.Key, group.Sum(total => total.Amount)))
            .OrderBy(total => total.Currency, StringComparer.Ordinal).ToArray();
        var deliverySnapshot = new OrderDeliveryMethodSnapshot(delivery.Id, delivery.Name,
            delivery.Description, delivery.Amount, delivery.Currency);
        return new(new(command.CheckoutToken, onlinePayment.ProviderName, PaymentReference(command.CheckoutToken),
            totals, deliverySnapshot), null);
    }

    private static string PaymentReference(Guid checkoutToken) =>
        $"OP-{checkoutToken:N}".ToUpperInvariant();
}
