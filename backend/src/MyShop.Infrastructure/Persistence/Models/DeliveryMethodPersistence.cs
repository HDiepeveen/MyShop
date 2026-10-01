namespace MyShop.Infrastructure.Persistence.Models;

internal sealed class DeliveryMethodPersistence
{
    public Guid Id { get; set; }
    public string Name { get; set; } = null!;
    public string? Description { get; set; }
    public decimal Amount { get; set; }
    public string Currency { get; set; } = null!;
    public bool Enabled { get; set; }
    public Guid Version { get; set; }
}
