using Microsoft.EntityFrameworkCore;
using MyShop.Application.Catalog.Abstractions;
using MyShop.Domain.Catalog;

namespace MyShop.Infrastructure.Persistence.Repositories;

internal sealed class StorefrontCatalog(MyShopDbContext context) : IStorefrontCatalog
{
    public async Task<StorefrontPage> ListAsync(int offset, int limit, string? search, CategoryId? categoryId,
        CancellationToken cancellationToken)
    {
        var query = context.Products.AsNoTracking().Where(product => product.IsPublished);
        if (search is not null) query = query.Where(product => product.Name.Contains(search));
        if (categoryId is not null)
            query = query.Where(product => product.Categories.Any(category =>
                category.CategoryId == categoryId.Value.Value));
        var count = await query.CountAsync(cancellationToken);
        var items = await query.OrderBy(product => product.Name).ThenBy(product => product.Id)
            .Skip(offset).Take(limit)
            .Select(product => new StorefrontItem(product.Id, product.Name, product.ImageUrl, product.ImageAlt))
            .ToListAsync(cancellationToken);
        return new(items, count, offset, limit);
    }

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
            .Select(product => new StorefrontProduct(product.Id, product.Name, product.Description, product.ImageUrl, product.ImageAlt,
                product.Variants.OrderBy(variant => variant.Ordinal)
                    .Select(variant => new StorefrontVariant(variant.Id, variant.Name)).ToList()))
            .SingleOrDefaultAsync(cancellationToken);
}
