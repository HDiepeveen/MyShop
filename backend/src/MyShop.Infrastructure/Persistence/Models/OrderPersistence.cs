namespace MyShop.Infrastructure.Persistence.Models;

internal sealed class OrderPersistence
{
    public Guid Id { get; set; }
    public Guid CheckoutToken { get; set; }
    public string? CustomerUserId { get; set; }
    public string Number { get; set; } = null!;
    public DateTimeOffset PlacedAt { get; set; }
    public string CustomerName { get; set; } = null!;
    public string Email { get; set; } = null!;
    public string AddressLine { get; set; } = null!;
    public string PostalCode { get; set; } = null!;
    public string City { get; set; } = null!;
    public string CountryCode { get; set; } = null!;
    public int PaymentMethod { get; set; }
    public string? PaymentInstructions { get; set; }
    public int Status { get; set; }
    public DateTimeOffset? PaidAt { get; set; }
    public string? PaymentReference { get; set; }
    public DateTimeOffset? ShippedAt { get; set; }
    public string? ShippingCarrier { get; set; }
    public string? TrackingCode { get; set; }
    public DateTimeOffset? CancelledAt { get; set; }
    public string? CancellationReason { get; set; }
    public DateTimeOffset? RefundedAt { get; set; }
    public string? RefundReference { get; set; }
    public string? RefundReason { get; set; }
    public Guid Version { get; set; }
    public ICollection<OrderLinePersistence> Lines { get; set; } = [];
    public ICollection<OrderTotalPersistence> Totals { get; set; } = [];
}
