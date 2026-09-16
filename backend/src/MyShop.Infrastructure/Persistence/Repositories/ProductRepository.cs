using Microsoft.EntityFrameworkCore;
using MyShop.Application.Catalog.Abstractions;
using MyShop.Domain.Catalog;
using MyShop.Infrastructure.Persistence.Mappers;
using MyShop.Infrastructure.Persistence.Models;

namespace MyShop.Infrastructure.Persistence.Repositories;

internal sealed class ProductRepository : IProductRepository
{
    private readonly MyShopDbContext _dbContext;

    internal ProductRepository(MyShopDbContext dbContext)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
    }

    public async Task<ProductSnapshot?> GetByIdAsync(
        ProductId id,
        CancellationToken cancellationToken)
    {
        var persistence = await CompleteGraph(_dbContext.Products)
            .AsNoTracking()
            .SingleOrDefaultAsync(product => product.Id == id.Value, cancellationToken);

        return persistence is null
            ? null
            : ProductPersistenceMapper.ToSnapshot(persistence);
    }

    public async Task<ProductConcurrencyToken> AddAsync(
        Product product,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(product);

        var revision = Guid.NewGuid();
        var persistence = new ProductPersistence
        {
            Id = product.Id.Value,
            Version = revision
        };

        ProductPersistenceSynchronizer.Synchronize(product, persistence);
        _dbContext.Products.Add(persistence);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return ProductConcurrencyToken.Create(product.Id, revision);
    }

    public async Task<ProductConcurrencyToken> SaveAsync(
        Product product,
        ProductConcurrencyToken expectedToken,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(product);
        ArgumentNullException.ThrowIfNull(expectedToken);

        if (expectedToken.ProductId != product.Id)
            throw new ArgumentException("Concurrency token must belong to the product.", nameof(expectedToken));

        var persistence = await CompleteGraph(_dbContext.Products)
            .SingleOrDefaultAsync(row => row.Id == product.Id.Value, cancellationToken);
        if (persistence is null)
            throw new ProductConcurrencyException(product.Id);

        return await SaveTrackedAsync(product, expectedToken, persistence, cancellationToken);
    }

    internal async Task<ProductConcurrencyToken> SaveTrackedAsync(
        Product product,
        ProductConcurrencyToken expectedToken,
        ProductPersistence persistence,
        CancellationToken cancellationToken)
    {
        ProductPersistenceSynchronizer.Synchronize(product, persistence);

        var revision = Guid.NewGuid();
        var version = _dbContext.Entry(persistence).Property(row => row.Version);
        version.OriginalValue = expectedToken.Revision;
        version.CurrentValue = revision;

        try
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new ProductConcurrencyException(product.Id);
        }

        return ProductConcurrencyToken.Create(product.Id, revision);
    }

    internal static IQueryable<ProductPersistence> CompleteGraph(
        IQueryable<ProductPersistence> products) => products
        .AsSplitQuery()
        .Include(product => product.Variants)
            .ThenInclude(variant => variant.AttributeValues)
                .ThenInclude(value => value.MultiChoiceValues)
        .Include(product => product.Categories)
        .Include(product => product.AttributeValues)
            .ThenInclude(value => value.MultiChoiceValues);
}
