using Microsoft.EntityFrameworkCore;
using System.Globalization;
using MyShop.Application.Catalog.Abstractions;
using MyShop.Domain.Catalog;
using MyShop.Infrastructure.Persistence.Mappers;

namespace MyShop.Infrastructure.Persistence.Repositories;

internal sealed class StorefrontCatalog(MyShopDbContext context) : IStorefrontCatalog
{
    public async Task<StorefrontPage> ListAsync(int offset, int limit, string? search, CategoryId? categoryId,
        DateTimeOffset at, CancellationToken cancellationToken,
        StorefrontSort sort = StorefrontSort.NameAscending, bool availableOnly = false)
    {
        var query = context.Products.AsNoTracking().Where(product => product.IsPublished);
        if (search is not null) query = query.Where(product => product.Name.Contains(search));
        if (categoryId is not null)
            query = query.Where(product => product.Categories.Any(category =>
                category.CategoryId == categoryId.Value.Value));
        if (availableOnly) query = query.Where(product => product.Variants.Any(variant =>
            variant.StockQuantity == null || variant.StockQuantity > 0));
        var ordered = sort == StorefrontSort.NameDescending
            ? query.OrderByDescending(product => product.Name).ThenBy(product => product.Id)
            : query.OrderBy(product => product.Name).ThenBy(product => product.Id);
        var count = await query.CountAsync(cancellationToken);
        var rows = await ordered
            .Skip(offset).Take(limit)
            .Select(product => new { product.Id, product.Name, product.ImageUrl, product.ImageAlt, Slug = product.Seo == null ? null : product.Seo.WebAddress })
            .ToListAsync(cancellationToken);
        var productIds = rows.Select(row => row.Id).ToArray();
        var persistedVariants = productIds.Length == 0
            ? []
            : await context.ProductVariants.AsNoTracking()
                .Where(variant => productIds.Contains(variant.ProductId))
                .Include(variant => variant.PriceRules)
                .ToListAsync(cancellationToken);
        var variantsByProduct = ProductPersistenceMapper.ToPricingVariants(persistedVariants);
        var items = rows.Select(row =>
        {
            var variants = variantsByProduct.GetValueOrDefault(row.Id, []);
            return new StorefrontItem(row.Id, row.Name, row.ImageUrl, row.ImageAlt,
                variants.Any(variant => variant.CanFulfill(1)), PriceRanges(variants, at))
            { WebAddress = row.Slug ?? MyShop.Application.Catalog.Seo.SeoText.AutomaticAddress(row.Name, row.Id) };
        }).ToList();
        var seo = await context.CatalogSeoSettings.AsNoTracking().SingleAsync(cancellationToken);
        return new(at, items, count, offset, limit) { Heading = seo.Heading, SeoTitle = seo.SeoTitle };
    }

    private static IReadOnlyList<StorefrontPriceRange> PriceRanges(
        IReadOnlyList<ProductVariant> variants, DateTimeOffset at) => variants
        .Where(variant => variant.Price is not null && variant.CanFulfill(1))
        .Select(variant => variant.CalculatePrice(at))
        .GroupBy(price => price.Currency)
        .OrderBy(group => group.Key, StringComparer.Ordinal)
        .Select(group => new StorefrontPriceRange(group.Key,
            group.Min(price => price.Amount), group.Max(price => price.Amount)))
        .ToList();

    public async Task<IReadOnlyList<StorefrontCategory>> ListCategoriesAsync(
        CancellationToken cancellationToken) => await context.Categories.AsNoTracking()
        .Where(category => context.ProductCategories.Any(productCategory =>
            productCategory.CategoryId == category.Id && productCategory.Product.IsPublished))
        .OrderBy(category => category.Name).ThenBy(category => category.Id)
        .Select(category => new StorefrontCategory(category.Id, category.Name))
        .ToListAsync(cancellationToken);

    public async Task<StorefrontProduct?> GetAsync(ProductId id, CancellationToken cancellationToken)
    {
        var detail = await context.Products.AsNoTracking()
            .Where(product => product.Id == id.Value && product.IsPublished)
            .Select(product => new StorefrontProduct(product.Id, product.Name, product.Description,
                product.ImageUrl, product.ImageAlt,
                product.Categories.Join(context.Categories,
                        link => link.CategoryId, category => category.Id,
                        (link, category) => new { link.Ordinal, Category = category })
                    .OrderBy(item => item.Ordinal).ThenBy(item => item.Category.Name)
                    .ThenBy(item => item.Category.Id)
                    .Select(item => new StorefrontCategory(item.Category.Id, item.Category.Name)).ToList(),
                product.Variants.OrderBy(variant => variant.Ordinal)
                    .Select(variant => new StorefrontVariant(variant.Id, variant.Name,
                        variant.StockQuantity == null || variant.StockQuantity > 0)).ToList())
            {
                Images = product.Images.OrderBy(image => image.Ordinal).ThenBy(image => image.Id)
                    .Select(image => new ProductImageInfo(image.Id, "/api/shop/product-images/" + image.Id,
                        image.AlternativeText, image.FileName)).ToList()
            })
            .SingleOrDefaultAsync(cancellationToken);
        if (detail is null) return null;
        var definitions = await context.AttributeDefinitions.AsNoTracking()
            .Where(d => d.ProductTypeId == context.Products.Where(p => p.Id == id.Value)
                .Select(p => p.ProductTypeId).First()
                && (d.Scope == AttributeScope.Product ||
                    (d.Scope == AttributeScope.Variant && d.DataType != AttributeDataType.MultiChoice)))
            .OrderBy(d => d.Code).ThenBy(d => d.Id).ToListAsync(cancellationToken);
        var productDefinitions = definitions.Where(d => d.Scope == AttributeScope.Product).ToArray();
        var productValues = await context.ProductAttributeValues.AsNoTracking()
            .Where(v => v.ProductId == id.Value)
            .Include(v => v.MultiChoiceValues).ToListAsync(cancellationToken);
        definitions = definitions.Where(d => d.Scope == AttributeScope.Variant).ToList();
        var variantIds = detail.Variants.Select(v => v.Id).ToArray();
        var values = await context.ProductVariantAttributeValues.AsNoTracking()
            .Where(v => variantIds.Contains(v.ProductVariantId) && v.DataType != AttributeDataType.MultiChoice)
            .ToListAsync(cancellationToken);
        var valuesByVariant = values
            .Where(v => definitions.Any(d => d.Id == v.AttributeDefinitionId && d.DataType == v.DataType))
            .GroupBy(v => v.ProductVariantId)
            .ToDictionary(g => g.Key, g => (IReadOnlyList<StorefrontVariantAttribute>)g.Select(v =>
                new StorefrontVariantAttribute(v.AttributeDefinitionId,
                    OptionValue(AttributeValuePersistenceMapper.ToDomain(v)))).ToArray());
        var checkoutEnabled = await context.PaymentOptions.AsNoTracking()
            .Select(options => options.CheckoutEnabled).SingleAsync(cancellationToken);
        return detail with
        {
            CheckoutEnabled = checkoutEnabled,
            Attributes = productDefinitions.Join(productValues, d => d.Id, v => v.AttributeDefinitionId,
                (d, v) => new { Definition = d, Value = v })
                .Where(item => item.Definition.DataType == item.Value.DataType)
                .Select(item => new StorefrontProductAttribute(item.Definition.Id, item.Definition.DisplayName,
                    OptionValue(AttributeValuePersistenceMapper.ToDomain(item.Value)))).ToArray(),
            VariantDefinitions = definitions.Select(d => new StorefrontVariantDefinition(d.Id, d.DisplayName)).ToArray(),
            Variants = detail.Variants.Select(v => v with
            {
                Attributes = valuesByVariant.GetValueOrDefault(v.Id, [])
            }).ToArray()
        };
    }

    private static string OptionValue(AttributeValue value) => value switch
    {
        MultiChoiceAttributeValue choices => string.Join(", ", choices.Values.Select(choice => choice.Value)),
        TextAttributeValue text => text.Value,
        ChoiceAttributeValue choice => choice.Value.Value,
        IntegerAttributeValue integer => integer.Value.ToString(CultureInfo.InvariantCulture),
        DecimalAttributeValue number => number.Value.ToString("G29", CultureInfo.InvariantCulture),
        BooleanAttributeValue boolean => boolean.Value ? "Ja" : "Nee",
        DateAttributeValue date => date.Value.ToString("dd-MM-yyyy", CultureInfo.InvariantCulture),
        _ => throw new InvalidOperationException("Only scalar variant options can be selected.")
    };
}
