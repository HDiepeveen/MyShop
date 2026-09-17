using Microsoft.EntityFrameworkCore;
using MyShop.Application.Catalog.Abstractions;
using MyShop.Infrastructure.Persistence.Models;

namespace MyShop.Infrastructure.Persistence.Repositories;

internal sealed class ProductListRepository
    : IProductListRepository
{
    private readonly MyShopDbContext _dbContext;

    internal ProductListRepository(MyShopDbContext dbContext) =>
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));

    public async Task<ProductListPage> ListAsync(
        int offset,
        int limit,
        CancellationToken cancellationToken)
    {
        var products = _dbContext.Products.AsNoTracking();
        var totalCount = await products.CountAsync(cancellationToken);
        var items = await ItemsQuery(products, offset, limit).ToListAsync(cancellationToken);
        return new ProductListPage(items, totalCount);
    }

    internal static IQueryable<ProductListItem> ItemsQuery(
        IQueryable<ProductPersistence> products,
        int offset,
        int limit) =>
        products
            .AsNoTracking()
            .OrderBy(product => product.Name)
            .ThenBy(product => product.Id)
            .Skip(offset)
            .Take(limit)
            .Select(product => new ProductListItem(
                product.Id,
                product.ProductTypeId,
                product.Name,
                product.Variants.Count));
}
