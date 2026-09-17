using Microsoft.EntityFrameworkCore;
using MyShop.Application.Catalog.Abstractions;
using MyShop.Domain.Catalog;

namespace MyShop.Infrastructure.Persistence.Repositories;

internal sealed class ProductSkuLookup : IProductSkuLookup
{
    private readonly MyShopDbContext _dbContext;

    internal ProductSkuLookup(MyShopDbContext dbContext)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
    }

    public async Task<ProductSkuOwner?> FindOwnerAsync(
        Sku sku,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(sku);

        var result = await OwnerQuery(_dbContext.ProductVariants, sku)
            .SingleOrDefaultAsync(cancellationToken);

        return result is null
            ? null
            : new ProductSkuOwner(
                ProductId.From(result.ProductId),
                ProductVariantId.From(result.ProductVariantId));
    }

    internal static IQueryable<ProductSkuOwnerProjection> OwnerQuery(
        IQueryable<Models.ProductVariantPersistence> variants,
        Sku sku)
    {
        ArgumentNullException.ThrowIfNull(variants);
        ArgumentNullException.ThrowIfNull(sku);

        return variants
            .AsNoTracking()
            .Where(variant => variant.Sku == sku.Value)
            .Select(variant => new ProductSkuOwnerProjection(variant.ProductId, variant.Id));
    }

    internal sealed record ProductSkuOwnerProjection(Guid ProductId, Guid ProductVariantId);
}
