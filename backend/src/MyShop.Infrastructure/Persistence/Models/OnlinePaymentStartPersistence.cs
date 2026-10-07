namespace MyShop.Infrastructure.Persistence.Models;

internal sealed class OnlinePaymentStartPersistence
{
    public Guid CheckoutToken { get; set; }
    public string ProviderName { get; set; } = null!;
    public string PaymentReference { get; set; } = null!;
    public string ProviderPaymentId { get; set; } = null!;
    public string CheckoutUrl { get; set; } = null!;
    public string? CustomerUserId { get; set; }
    public string CustomerName { get; set; } = null!;
    public string Email { get; set; } = null!;
    public string AddressLine { get; set; } = null!;
    public string PostalCode { get; set; } = null!;
    public string City { get; set; } = null!;
    public string CountryCode { get; set; } = null!;
    public DateTimeOffset CreatedAt { get; set; }
    public Guid DeliveryMethodId { get; set; }
    public string DeliveryMethodName { get; set; } = null!;
    public string? DeliveryDescription { get; set; }
    public decimal DeliveryAmount { get; set; }
    public string DeliveryCurrency { get; set; } = null!;
    public ICollection<OnlinePaymentStartLinePersistence> Lines { get; set; } = [];
    public ICollection<OnlinePaymentStartTotalPersistence> Totals { get; set; } = [];
}