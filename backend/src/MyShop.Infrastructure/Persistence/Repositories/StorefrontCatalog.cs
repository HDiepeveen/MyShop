using Microsoft.EntityFrameworkCore;
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
            .Select(product => new { product.Id, product.Name, product.ImageUrl, product.ImageAlt })
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
                variants.Any(variant => variant.CanFulfill(1)), PriceRanges(variants, at));
        }).ToList();
        return new(at, items, count, offset, limit);
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

    public Task<StorefrontProduct?> GetAsync(ProductId id, CancellationToken cancellationToken) =>
        context.Products.AsNoTracking()
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
                        variant.StockQuantity == null || variant.StockQuantity > 0)).ToList()))
            .SingleOrDefaultAsync(cancellationToken);
}
