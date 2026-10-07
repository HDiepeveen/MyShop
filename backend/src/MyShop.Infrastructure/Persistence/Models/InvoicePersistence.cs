namespace MyShop.Infrastructure.Persistence.Models;
internal sealed class InvoicePersistence
{
    public Guid Id { get; set; }
    public Guid OrderId { get; set; }
    public string Number { get; set; } = "";
    public string Document { get; set; } = "";
    public DateTimeOffset IssuedAt { get; set; }
}
