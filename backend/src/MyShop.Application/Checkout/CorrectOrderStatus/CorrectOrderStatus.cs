using MyShop.Application.Checkout.Abstractions;
using MyShop.Domain.Checkout;

namespace MyShop.Application.Checkout.CorrectOrderStatus;

public enum OrderCorrection { Payment, Shipping }
public enum CorrectionResult { Success, NotFound, InvalidTransition, ConcurrencyConflict }
public interface IOrderCorrectionRepository
{
    Task<bool> CorrectAsync(Guid id, Guid revision, OrderCorrection correction, CancellationToken cancellationToken);
}

public sealed class CorrectOrderStatus(IOrderReadRepository orders, IOrderCorrectionRepository corrections)
{
    public async Task<CorrectionResult> ExecuteAsync(Guid id, Guid revision, OrderCorrection correction,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (id == Guid.Empty || revision == Guid.Empty || !Enum.IsDefined(correction))
            throw new ArgumentException("A valid order, revision and correction are required.");
        var order = await orders.GetAsync(id, cancellationToken);
        if (order is null) return CorrectionResult.NotFound;
        if (order.Revision != revision) return CorrectionResult.ConcurrencyConflict;
        if (correction == OrderCorrection.Payment
            ? order.Status != OrderStatus.Paid || order.PaymentMethod != OrderPaymentMethod.PayLater
            : order.Status != OrderStatus.Shipped)
            return CorrectionResult.InvalidTransition;
        return await corrections.CorrectAsync(id, revision, correction, cancellationToken)
            ? CorrectionResult.Success : CorrectionResult.ConcurrencyConflict;
    }
}
