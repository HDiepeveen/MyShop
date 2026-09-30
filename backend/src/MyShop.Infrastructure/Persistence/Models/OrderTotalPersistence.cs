namespace MyShop.Infrastructure.Persistence.Models;

internal sealed class OrderTotalPersistence
{
    public Guid OrderId { get; set; }
    public string Currency { get; set; } = null!;
    public decimal Amount { get; set; }
    public OrderPersistence Order { get; set; } = null!;
}
