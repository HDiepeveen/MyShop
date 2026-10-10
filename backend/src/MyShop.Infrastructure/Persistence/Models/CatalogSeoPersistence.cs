namespace MyShop.Infrastructure.Persistence.Models;
internal sealed class CatalogSeoSettingsPersistence
{
    public Guid Id { get; set; }
    public string ShopName { get; set; } = "MyShop";
    public string WelcomeText { get; set; } = "Welkom bij MyShop";
    public string Introduction { get; set; } = "Bekijk onze producten en kies de variant die bij je past.";
    public string Heading { get; set; } = "Ontdek ons assortiment";
    public string SeoTitle { get; set; } = "Assortiment · MyShop";
    public Guid Version { get; set; }
}
internal sealed class ProductSeoPersistence
{
    public Guid ProductId { get; set; }
    public ProductPersistence Product { get; set; } = null!;
    public string? SeoTitle { get; set; }
    public string? SeoDescription { get; set; }
    public string? WebAddress { get; set; }
    public string? AboutHeading { get; set; }
    public string? AttributesHeading { get; set; }
    public Guid Version { get; set; }
}
internal sealed class ProductWebAddressPersistence
{
    public string Address { get; set; } = null!;
    public Guid ProductId { get; set; }
    public ProductPersistence Product { get; set; } = null!;
}
