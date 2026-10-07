namespace MyShop.Infrastructure.Persistence.Models;

internal sealed class OrderLinePersistence
{
    public Guid OrderId { get; set; }
    public int Ordinal { get; set; }
    public Guid ProductId { get; set; }
    public Guid VariantId { get; set; }
    public string ProductName { get; set; } = null!;
    public string VariantName { get; set; } = null!;
    public int Quantity { get; set; }
    public decimal UnitAmount { get; set; }
    public string Currency { get; set; } = null!;
    public decimal TotalAmount { get; set; }
    public decimal? VatRate { get; set; }
    public bool VatExempt { get; set; }
    public decimal? NetAmount { get; set; }
    public decimal? VatAmount { get; set; }
    public OrderPersistence Order { get; set; } = null!;
}
