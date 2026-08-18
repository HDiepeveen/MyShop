namespace MyShop.Domain.Catalog;

public sealed class ProductVariant
{
    internal ProductVariant(ProductVariantId id, string name)
    {
        if (id == default)
            throw new ArgumentException("Product variant ID must not be empty.", nameof(id));

        Id = id;
        Name = ValidateName(name);
    }

    public ProductVariantId Id { get; }
    public string Name { get; private set; }

    internal void Rename(string name) => Name = ValidateName(name);

    private static string ValidateName(string name)
    {
        ArgumentNullException.ThrowIfNull(name);
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Product variant name must not be empty or whitespace.", nameof(name));
        return name;
    }
}
