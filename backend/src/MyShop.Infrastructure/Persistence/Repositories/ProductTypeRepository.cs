using Microsoft.EntityFrameworkCore;
using MyShop.Application.Catalog.Abstractions;
using MyShop.Domain.Catalog;
using MyShop.Infrastructure.Persistence.Mappers;
using MyShop.Infrastructure.Persistence.Models;

namespace MyShop.Infrastructure.Persistence.Repositories;

internal sealed class ProductTypeRepository : IProductTypeRepository, IProductTypeListRepository
{
    private readonly MyShopDbContext _dbContext;

    internal ProductTypeRepository(MyShopDbContext dbContext)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
    }

    public async Task<ProductType?> GetByIdAsync(
        ProductTypeId id,
        CancellationToken cancellationToken)
    {
        var persistence = await _dbContext.ProductTypes
            .AsNoTracking()
            .Include(productType => productType.AttributeDefinitions)
            .SingleOrDefaultAsync(productType => productType.Id == id.Value, cancellationToken);

        return persistence is null
            ? null
            : ProductTypePersistenceMapper.ToDomain(persistence);
    }

    public async Task<IReadOnlyList<ProductTypeListItem>> ListAsync(
        string? searchTerm,
        CancellationToken cancellationToken) =>
        await ListQuery(_dbContext.ProductTypes, searchTerm).ToListAsync(cancellationToken);

    internal static IQueryable<ProductTypeListItem> ListQuery(
        IQueryable<ProductTypePersistence> productTypes,
        string? searchTerm)
    {
        if (searchTerm is not null)
            productTypes = productTypes.Where(productType => productType.Name.Contains(searchTerm));

        return productTypes
            .AsNoTracking()
            .OrderBy(productType => productType.Name)
            .ThenBy(productType => productType.Id)
            .Select(productType => new ProductTypeListItem(
                productType.Id,
                productType.Name,
                productType.AttributeDefinitions.Count));
    }
}
