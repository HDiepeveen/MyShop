namespace MyShop.Infrastructure.Persistence.Models;

internal sealed class PriceRulePersistence
{
    public Guid Id { get; set; }
    public Guid ProductVariantId { get; set; }
    public string Name { get; set; } = null!;
    public int AdjustmentType { get; set; }
    public decimal Value { get; set; }
    public int Priority { get; set; }
    public DateTimeOffset? StartsAt { get; set; }
    public DateTimeOffset? EndsAt { get; set; }
    public ProductVariantPersistence ProductVariant { get; set; } = null!;
}
