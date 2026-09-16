using Microsoft.EntityFrameworkCore;
using MyShop.Application.Catalog.Abstractions;
using MyShop.Domain.Catalog;
using MyShop.Infrastructure.Persistence.Mappers;

namespace MyShop.Infrastructure.Persistence.Repositories;

internal sealed class ProductTypeRepository : IProductTypeRepository
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
}
