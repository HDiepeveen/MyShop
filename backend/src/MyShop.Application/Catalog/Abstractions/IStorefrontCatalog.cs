using MyShop.Domain.Catalog;

namespace MyShop.Application.Catalog.Abstractions;

public interface IStorefrontCatalog
{
    Task<StorefrontPage> ListAsync(int offset, int limit, string? search, CategoryId? categoryId,
        DateTimeOffset at, CancellationToken cancellationToken,
        StorefrontSort sort = StorefrontSort.NameAscending, bool availableOnly = false);
    Task<IReadOnlyList<StorefrontCategory>> ListCategoriesAsync(CancellationToken cancellationToken);
    Task<StorefrontProduct?> GetAsync(ProductId id, CancellationToken cancellationToken);
}

public enum StorefrontSort { NameAscending, NameDescending }

public sealed record StorefrontPriceRange(string Currency, decimal MinimumAmount, decimal MaximumAmount);
public sealed record StorefrontItem(Guid Id, string Name, string? ImageUrl, string ImageAlt,
    bool IsAvailable, IReadOnlyList<StorefrontPriceRange> Prices)
{
    public string? WebAddress { get; init; }
}
public sealed record StorefrontPage(DateTimeOffset At, IReadOnlyList<StorefrontItem> Items,
    int TotalCount, int Offset, int Limit)
{
    public string WelcomeText { get; init; } = "Welkom bij MyShop";
    public string Introduction { get; init; } = "Bekijk onze producten en kies de variant die bij je past.";
    public string Heading { get; init; } = "Ontdek ons assortiment";
    public string SeoTitle { get; init; } = "Assortiment · MyShop";
}
public sealed record StorefrontCategory(Guid Id, string Name);
public sealed record StorefrontProductAttribute(Guid AttributeDefinitionId, string Name, string Value);
public sealed record StorefrontVariantAttribute(Guid AttributeDefinitionId, string Value);
public sealed record StorefrontVariantDefinition(Guid Id, string Name);
public sealed record StorefrontVariant(Guid Id, string Name, bool IsAvailable)
{
    public IReadOnlyList<StorefrontVariantAttribute> Attributes { get; init; } = [];
}
public sealed record StorefrontProduct(Guid Id, string Name, string Description, string? ImageUrl,
    string ImageAlt, IReadOnlyList<StorefrontCategory> Categories,
    IReadOnlyList<StorefrontVariant> Variants)
{
    public string SeoTitle { get; init; } = "";
    public string SeoDescription { get; init; } = "";
    public string WebAddress { get; init; } = "";
    public bool CheckoutEnabled { get; init; } = true;
    public IReadOnlyList<StorefrontProductAttribute> Attributes { get; init; } = [];
    public IReadOnlyList<ProductImageInfo> Images { get; init; } = [];
    public IReadOnlyList<StorefrontVariantDefinition> VariantDefinitions { get; init; } = [];
}
