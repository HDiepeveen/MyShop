using System.Collections.ObjectModel;

namespace MyShop.Domain.Catalog;

public sealed class Product
{
    private readonly List<ProductVariant> _variants = [];
    private readonly ReadOnlyCollection<ProductVariant> _readOnlyVariants;

    private Product(string name, ProductTypeId productTypeId, string initialVariantName)
    {
        if (productTypeId == default)
            throw new ArgumentException("Product type ID must not be empty.", nameof(productTypeId));

        Id = ProductId.New();
        ProductTypeId = productTypeId;
        Name = ValidateName(name);
        _readOnlyVariants = _variants.AsReadOnly();
        AddVariant(initialVariantName);
    }

    public ProductId Id { get; }
    public ProductTypeId ProductTypeId { get; }
    public string Name { get; private set; }
    public IReadOnlyCollection<ProductVariant> Variants => _readOnlyVariants;

    public static Product Create(string name, ProductTypeId productTypeId, string initialVariantName) =>
        new(name, productTypeId, initialVariantName);

    public void Rename(string name) => Name = ValidateName(name);

    public ProductVariant AddVariant(string name)
    {
        var variant = new ProductVariant(ProductVariantId.New(), name);
        _variants.Add(variant);
        return variant;
    }

    public void RenameVariant(ProductVariantId variantId, string name) => FindVariant(variantId).Rename(name);

    public void RemoveVariant(ProductVariantId variantId)
    {
        var variant = FindVariant(variantId);

        if (_variants.Count == 1)
            throw new InvalidOperationException("A product must contain at least one variant.");

        _variants.Remove(variant);
    }

    private ProductVariant FindVariant(ProductVariantId variantId) =>
        _variants.SingleOrDefault(variant => variant.Id == variantId)
        ?? throw new InvalidOperationException($"Variant with ID '{variantId}' does not belong to this product.");

    private static string ValidateName(string name)
    {
        ArgumentNullException.ThrowIfNull(name);
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Product name must not be empty or whitespace.", nameof(name));
        return name;
    }
}
