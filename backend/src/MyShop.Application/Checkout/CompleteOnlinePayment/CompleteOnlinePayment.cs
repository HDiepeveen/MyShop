using MyShop.Application.Checkout.Abstractions;
using MyShop.Domain.Catalog;
using MyShop.Domain.Checkout;

namespace MyShop.Application.Checkout.CompleteOnlinePayment;

public sealed record CompleteOnlinePaymentCommand(Guid CheckoutToken, string ProviderPaymentId);
public enum CompleteOnlinePaymentFailure { PaymentNotFound, PaymentMismatch, CartUnavailable }
public sealed record CompleteOnlinePaymentResult(OrderReceipt? Receipt, CompleteOnlinePaymentFailure? Failure)
{
    public static CompleteOnlinePaymentResult Failed(CompleteOnlinePaymentFailure failure) => new(null, failure);
}

public sealed class CompleteOnlinePayment(IOnlinePaymentStartRepository paymentStarts, IOrderRepository orders)
{
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
        ArgumentException.ThrowIfNullOrWhiteSpace(command.ProviderPaymentId);

        var existing = await orders.GetByCheckoutTokenAsync(command.CheckoutToken, cancellationToken);
        if (existing is not null) return new(existing, null);

        var payment = await paymentStarts.GetByCheckoutTokenAsync(command.CheckoutToken, cancellationToken);
        if (payment is null) return CompleteOnlinePaymentResult.Failed(CompleteOnlinePaymentFailure.PaymentNotFound);
        if (!string.Equals(payment.ProviderPaymentId, command.ProviderPaymentId.Trim(), StringComparison.Ordinal))
            return CompleteOnlinePaymentResult.Failed(CompleteOnlinePaymentFailure.PaymentMismatch);

        var customer = OrderCustomer.Create(payment.Customer.Name, payment.Customer.Email);
        var address = DeliveryAddress.Create(payment.Address.AddressLine, payment.Address.PostalCode,
            payment.Address.City, payment.Address.CountryCode);
        var delivery = new OrderDeliveryMethod(payment.DeliveryMethod.Id, payment.DeliveryMethod.Name,
            payment.DeliveryMethod.Description,
            Money.Create(payment.DeliveryMethod.Amount, payment.DeliveryMethod.Currency));
        var order = Order.Place(Guid.NewGuid(), DateTimeOffset.UtcNow, customer, address,
            payment.Lines.Select(line => (line.ProductId, line.VariantId, line.ProductName,
                line.VariantName, line.Quantity, Money.Create(line.UnitAmount, line.Currency))),
            paymentInstructions: null, deliveryMethod: delivery, paymentMethod: OrderPaymentMethod.Online,
            vatRates: payment.Lines.Where(line => line.VatRate is not null).ToDictionary(line => (line.ProductId, line.VariantId), line => (line.VatRate!.Value, line.VatExempt)));
        var receipt = await orders.AddPaidAsync(order, command.CheckoutToken, payment.PaymentReference,
            payment.Lines.Select(line => new StockReservation(line.ProductId, line.VariantId,
                line.Quantity)).ToArray(), cancellationToken);
        return receipt is null
            ? CompleteOnlinePaymentResult.Failed(CompleteOnlinePaymentFailure.CartUnavailable)
            : new(receipt, null);
    }
}
