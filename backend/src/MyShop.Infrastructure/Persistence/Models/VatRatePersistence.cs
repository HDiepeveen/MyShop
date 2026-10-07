namespace MyShop.Infrastructure.Persistence.Models;
internal sealed class VatRatePersistence
{
    public Guid Id { get; set; }
    public string Name { get; set; } = "";
    public decimal Percentage { get; set; }
    public bool Exempt { get; set; }
    public bool Enabled { get; set; }
    public Guid Version { get; set; }
}
