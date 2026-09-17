using Microsoft.EntityFrameworkCore;
using MyShop.Application.Catalog.Abstractions;
using MyShop.Domain.Catalog;
using MyShop.Infrastructure.Persistence.Mappers;
using MyShop.Infrastructure.Persistence.Models;

namespace MyShop.Infrastructure.Persistence.Repositories;

internal sealed class ProductTypeRepository
    : IProductTypeRepository, IProductTypeListRepository, IProductTypeWriter,
      IProductTypeUsageRepository, IProductTypeDeleter
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

    public async Task AddAsync(ProductType productType, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(productType);

        var persistence = new ProductTypePersistence { Id = productType.Id.Value };
        ProductTypePersistenceSynchronizer.Synchronize(productType, persistence);
        _dbContext.ProductTypes.Add(persistence);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task SaveAsync(ProductType productType, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(productType);

        var persistence = await CompleteGraph(_dbContext.ProductTypes)
            .SingleOrDefaultAsync(row => row.Id == productType.Id.Value, cancellationToken)
            ?? throw new InvalidOperationException(
                $"Product type '{productType.Id}' no longer exists.");

        ProductTypePersistenceSynchronizer.Synchronize(productType, persistence);
        await _dbContext.SaveChangesAsync(cancellationToken);
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

    internal static IQueryable<ProductTypePersistence> CompleteGraph(
        IQueryable<ProductTypePersistence> productTypes) =>
        productTypes.Include(productType => productType.AttributeDefinitions);

    public async Task<int> CountProductsAsync(
        ProductTypeId productTypeId,
        CancellationToken cancellationToken) =>
        await ProductsQuery(_dbContext.Products, productTypeId).CountAsync(cancellationToken);

    internal static IQueryable<ProductPersistence> ProductsQuery(
        IQueryable<ProductPersistence> products,
        ProductTypeId productTypeId) =>
        products.AsNoTracking().Where(product => product.ProductTypeId == productTypeId.Value);

    public async Task DeleteAsync(ProductTypeId productTypeId, CancellationToken cancellationToken)
    {
        var persistence = await _dbContext.ProductTypes.SingleOrDefaultAsync(
            row => row.Id == productTypeId.Value, cancellationToken)
            ?? throw new InvalidOperationException($"Product type '{productTypeId}' no longer exists.");
        _dbContext.ProductTypes.Remove(persistence);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
