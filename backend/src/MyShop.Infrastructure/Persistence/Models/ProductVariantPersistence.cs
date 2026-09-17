namespace MyShop.Infrastructure.Persistence.Models;

internal sealed class ProductVariantPersistence
{
    public Guid Id { get; set; }
    public Guid ProductId { get; set; }
    public string Name { get; set; } = null!;
    public string? Sku { get; set; }
    public int Ordinal { get; set; }
    public decimal? PriceAmount { get; set; }
    public string? PriceCurrency { get; set; }
    public ProductPersistence Product { get; set; } = null!;
    public ICollection<ProductVariantAttributeValuePersistence> AttributeValues { get; set; } = [];
    public ICollection<PriceRulePersistence> PriceRules { get; set; } = [];
}
