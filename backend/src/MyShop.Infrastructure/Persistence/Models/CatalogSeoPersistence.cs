namespace MyShop.Infrastructure.Persistence.Models;
internal sealed class CatalogSeoSettingsPersistence
{
    public Guid Id { get; set; }
    public byte[]? IconBytes { get; set; }
    public string? IconContentType { get; set; }
    public string PrimaryColor { get; set; } = "#1d5543";
    public string BackgroundColor { get; set; } = "#f6f7f1";
    public string PanelColor { get; set; } = "#fffefa";
    public string TextColor { get; set; } = "#223c33";
    public string ShopName { get; set; } = "MyShop";
    public string WelcomeText { get; set; } = "Welkom bij MyShop";
    public string Introduction { get; set; } = "Bekijk onze producten en kies de variant die bij je past.";
    public string FooterText { get; set; } = "Ontdek wat bij je past.";
    public string CompanyHeading { get; set; } = "Over ons en contact";
    public string? CompanyName { get; set; }
    public string? CompanyDescription { get; set; }
    public string? CompanyAddress { get; set; }
    public string? CompanyEmail { get; set; }
    public string? CompanyPhone { get; set; }
    public string? CompanyOpeningHours { get; set; }
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
    public Guid Version { get; set; }
}
internal sealed class ProductWebAddressPersistence
{
    public string Address { get; set; } = null!;
    public Guid ProductId { get; set; }
    public ProductPersistence Product { get; set; } = null!;
}
