namespace MyShop.Infrastructure.Persistence.Models;
internal sealed class CompanySettingsPersistence
{
    public Guid Id { get; set; }
    public string Name { get; set; } = "";
    public string AddressLine { get; set; } = "";
    public string PostalCode { get; set; } = "";
    public string City { get; set; } = "";
    public string VatId { get; set; } = "";
    public string KvkNumber { get; set; } = "";
    public string InvoicePrefix { get; set; } = "INV-";
    public Guid Version { get; set; }
}
