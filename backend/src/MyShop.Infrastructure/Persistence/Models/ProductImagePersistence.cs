namespace MyShop.Infrastructure.Persistence.Models;

internal sealed class ProductImagePersistence
{
    public Guid Id { get; set; }
    public Guid ProductId { get; set; }
    public ProductPersistence Product { get; set; } = null!;
    public byte[] Bytes { get; set; } = [];
    public string ContentType { get; set; } = "";
    public string AlternativeText { get; set; } = "";
    public string FileName { get; set; } = "";
    public int Ordinal { get; set; }
}
