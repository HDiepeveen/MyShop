namespace MyShop.Infrastructure.Persistence.Models;

internal sealed class OnlinePaymentStartTotalPersistence
{
    public Guid OnlinePaymentStartId { get; set; }
    public string Currency { get; set; } = null!;
    public decimal Amount { get; set; }
    public OnlinePaymentStartPersistence PaymentStart { get; set; } = null!;
}