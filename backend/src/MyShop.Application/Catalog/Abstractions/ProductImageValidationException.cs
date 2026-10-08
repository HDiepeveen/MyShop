namespace MyShop.Application.Catalog.Abstractions;

public sealed class ProductImageValidationException(string code, string message) : ArgumentException(message)
{
    public string Code { get; } = code;
}
