using HDWebFramework.Application.Checkout.Abstractions;
using HDWebFramework.Domain.Catalog;
using HDWebFramework.Domain.Checkout;

namespace HDWebFramework.Application.Checkout.CompleteOnlinePayment;

public sealed record CompleteOnlinePaymentCommand(Guid CheckoutToken, string? ProviderPaymentId);
public enum CompleteOnlinePaymentFailure { PaymentNotFound, PaymentMismatch, CartUnavailable, PaymentPending, PaymentFailed, PaymentCanceled, PaymentExpired }
public sealed record CompleteOnlinePaymentResult(OrderReceipt? Receipt, CompleteOnlinePaymentFailure? Failure)
{
    public HDWebFramework.Application.Checkout.StartOnlinePayment.OnlinePaymentStart? PreparedPayment { get; init; }
    public Uri? CheckoutUrl { get; init; }
    public static CompleteOnlinePaymentResult Failed(CompleteOnlinePaymentFailure failure) => new(null, failure);
}

public sealed class CompleteOnlinePayment(IOnlinePaymentStartRepository paymentStarts, IOrderRepository orders, IOnlinePaymentStatusReader paymentStatus)
{
    private readonly IOnlinePaymentStatusReader paymentStatus = paymentStatus ?? throw new ArgumentNullException(nameof(paymentStatus));
    private readonly IOnlinePaymentStartRepository paymentStarts = paymentStarts
        ?? throw new ArgumentNullException(nameof(paymentStarts));
    private readonly IOrderRepository orders = orders ?? throw new ArgumentNullException(nameof(orders));

    public async Task<CompleteOnlinePaymentResult> ExecuteAsync(CompleteOnlinePaymentCommand command,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        cancellationToken.ThrowIfCancellationRequested();
        if (command.CheckoutToken == Guid.Empty)
            throw new ArgumentException("Checkout token is required.", nameof(command));

        var payment = await paymentStarts.GetByCheckoutTokenAsync(command.CheckoutToken, cancellationToken);
        if (payment is null) return CompleteOnlinePaymentResult.Failed(CompleteOnlinePaymentFailure.PaymentNotFound);
        if (command.ProviderPaymentId is not null && !string.Equals(payment.ProviderPaymentId, command.ProviderPaymentId.Trim(), StringComparison.Ordinal))
            return CompleteOnlinePaymentResult.Failed(CompleteOnlinePaymentFailure.PaymentMismatch);

        var existing = await orders.GetByCheckoutTokenAsync(command.CheckoutToken, cancellationToken);
        if (existing is not null) return new(existing, null);
        var verified = await paymentStatus.VerifyAsync(payment, cancellationToken);
        if (!verified.MatchesPayment) return CompleteOnlinePaymentResult.Failed(CompleteOnlinePaymentFailure.PaymentMismatch);
        if (verified.Status != OnlinePaymentStatus.Paid)
            return CompleteOnlinePaymentResult.Failed(verified.Status switch
            {
                OnlinePaymentStatus.Failed => CompleteOnlinePaymentFailure.PaymentFailed,
                OnlinePaymentStatus.Canceled => CompleteOnlinePaymentFailure.PaymentCanceled,
                OnlinePaymentStatus.Expired => CompleteOnlinePaymentFailure.PaymentExpired,
                _ => CompleteOnlinePaymentFailure.PaymentPending
            }) with {
                CheckoutUrl = verified.Status == OnlinePaymentStatus.Open ? payment.CheckoutUrl : null,
                PreparedPayment = verified.Status is OnlinePaymentStatus.Open or OnlinePaymentStatus.Pending or OnlinePaymentStatus.Authorized
                    ? new(payment.CheckoutToken, payment.ProviderName, payment.PaymentReference, payment.ProviderPaymentId,
                        payment.CheckoutUrl, payment.Totals, payment.DeliveryMethod) : null
            };

        var customer = OrderCustomer.Create(payment.Customer.Name, payment.Customer.Email);
        var address = DeliveryAddress.Create(payment.Address.AddressLine, payment.Address.PostalCode,
            payment.Address.City, payment.Address.CountryCode, payment.Address.PhoneNumber, payment.Address.Notes);
        var delivery = new OrderDeliveryMethod(payment.DeliveryMethod.Id, payment.DeliveryMethod.Name,
            payment.DeliveryMethod.Description,
            Money.Create(payment.DeliveryMethod.Amount, payment.DeliveryMethod.Currency));
        var order = Order.Place(Guid.NewGuid(), DateTimeOffset.UtcNow, customer, address,
            payment.Lines.Select(line => (line.ProductId, line.VariantId, line.ProductName,
                line.VariantName, line.Quantity, Money.Create(line.UnitAmount, line.Currency))),
            paymentInstructions: null, deliveryMethod: delivery, paymentMethod: OrderPaymentMethod.Online,
            vatRates: payment.Lines.Where(line => line.VatRate is not null).ToDictionary(line => (line.ProductId, line.VariantId), line => (line.VatRate!.Value, line.VatExempt)));
        if (order.Totals.Count != payment.Totals.Count || order.Totals.Any(total =>
            !payment.Totals.Any(saved => saved.Currency == total.Currency && saved.Amount == total.Amount)))
            return CompleteOnlinePaymentResult.Failed(CompleteOnlinePaymentFailure.PaymentMismatch);
        var receipt = await orders.AddPaidAsync(order, command.CheckoutToken, payment.PaymentReference,
            payment.Lines.Select(line => new StockReservation(line.ProductId, line.VariantId,
                line.Quantity)).ToArray(), cancellationToken, payment.CustomerUserId);
        return receipt is null
            ? CompleteOnlinePaymentResult.Failed(CompleteOnlinePaymentFailure.CartUnavailable)
            : new(receipt, null);
    }
}
