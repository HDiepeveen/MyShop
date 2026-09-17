using Microsoft.EntityFrameworkCore;
using MyShop.Application.Catalog.Abstractions;
using MyShop.Domain.Catalog;
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
        ProductTypeId? productTypeId,
        CancellationToken cancellationToken)
    {
        var products = FilterQuery(_dbContext.Products.AsNoTracking(), productTypeId);
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

    internal static IQueryable<ProductPersistence> FilterQuery(
        IQueryable<ProductPersistence> products,
        ProductTypeId? productTypeId) =>
        productTypeId is null
            ? products
            : products.Where(product => product.ProductTypeId == productTypeId.Value.Value);
}
