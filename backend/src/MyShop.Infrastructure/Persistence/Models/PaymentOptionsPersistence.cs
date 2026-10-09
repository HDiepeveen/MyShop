namespace MyShop.Infrastructure.Persistence.Models;

internal sealed class PaymentOptionsPersistence
{
    public Guid Id { get; set; }
    public bool CheckoutEnabled { get; set; } = true;
    public bool PayLaterEnabled { get; set; }
    public bool OnlinePaymentEnabled { get; set; }
    public string? PayLaterInstructions { get; set; }
    public Guid Version { get; set; }
}
