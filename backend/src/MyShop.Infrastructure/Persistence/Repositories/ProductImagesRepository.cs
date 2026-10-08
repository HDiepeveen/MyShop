using Microsoft.EntityFrameworkCore;
using MyShop.Application.Catalog.Abstractions;
using MyShop.Domain.Catalog;
using MyShop.Infrastructure.Persistence.Models;

namespace MyShop.Infrastructure.Persistence.Repositories;

internal sealed class ProductImagesRepository(MyShopDbContext context) : IProductImages
{
    private static string Url(Guid id) => $"/api/shop/product-images/{id:D}";

    public async Task<ProductImages?> GetAsync(Guid productId, CancellationToken cancellationToken)
    {
        var product = await context.Products.AsNoTracking().Where(product => product.Id == productId)
            .Select(product => new { product.Version, product.ImageUrl }).SingleOrDefaultAsync(cancellationToken);
        if (product is null) return null;
        var rows = await context.ProductImages.AsNoTracking().Where(image => image.ProductId == productId)
            .OrderBy(image => image.Ordinal).ThenBy(image => image.Id)
            .Select(image => new { image.Id, image.AlternativeText, image.FileName }).ToListAsync(cancellationToken);
        return new(product.Version, product.ImageUrl, rows.Select(image => new ProductImageInfo(image.Id, Url(image.Id), image.AlternativeText, image.FileName)).ToList());
    }

    public Task<ProductImageContent?> ContentAsync(Guid imageId, bool administrator, CancellationToken cancellationToken) =>
        context.ProductImages.AsNoTracking().Where(image => image.Id == imageId && (administrator || image.Product.IsPublished))
            .Select(image => new ProductImageContent(image.Bytes, image.ContentType)).SingleOrDefaultAsync(cancellationToken);

    public Task<bool> OwnsAsync(Guid productId, string url, CancellationToken cancellationToken)
    {
        var imageId = Guid.Parse(url["/api/shop/product-images/".Length..]);
        return context.ProductImages.AnyAsync(image => image.ProductId == productId && image.Id == imageId, cancellationToken);
    }

    // Claim the product revision before reading the gallery. The row lock serializes image
    // changes with ordinary product saves; failures roll back both metadata and file bytes.
    private async Task<bool> ClaimAsync(Guid productId, Guid revision, CancellationToken cancellationToken)
    {
        if (productId == Guid.Empty || revision == Guid.Empty) throw new ArgumentException("Product and revision are required.");
        var changed = await context.Products.Where(product => product.Id == productId && product.Version == revision)
            .ExecuteUpdateAsync(update => update.SetProperty(product => product.Version, Guid.NewGuid()), cancellationToken);
        if (changed == 1) return true;
        if (await context.Products.AnyAsync(product => product.Id == productId, cancellationToken))
            throw new ProductConcurrencyException(ProductId.From(productId));
        return false;
    }

    public async Task<bool> UploadAsync(Guid productId, Guid revision, IReadOnlyList<ProductImageUpload> images, CancellationToken cancellationToken)
    {
        if (images.Count is < 1 or > ProductImageUpload.MaximumImages) throw new ArgumentException("Choose 1 to 10 images.");
        await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);
        if (!await ClaimAsync(productId, revision, cancellationToken)) return false;
        var ordinals = await context.ProductImages.Where(image => image.ProductId == productId).Select(image => image.Ordinal).ToListAsync(cancellationToken);
        if (ordinals.Count + images.Count > ProductImageUpload.MaximumImages) throw new ArgumentException("At most 10 uploaded images per product.");
        var next = ordinals.Count == 0 ? 0 : ordinals.Max() + 1;
        var added = images.Select(image => new ProductImagePersistence { Id = Guid.NewGuid(), ProductId = productId,
            Bytes = image.Bytes, ContentType = image.ContentType, AlternativeText = image.AlternativeText, FileName = image.FileName, Ordinal = next++ }).ToList();
        context.ProductImages.AddRange(added);
        await context.SaveChangesAsync(cancellationToken);
        await context.Products.Where(product => product.Id == productId && product.ImageUrl == null)
            .ExecuteUpdateAsync(update => update.SetProperty(product => product.ImageUrl, Url(added[0].Id))
                .SetProperty(product => product.ImageAlt, added[0].AlternativeText), cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return true;
    }

    public async Task<bool> SetMainAsync(Guid productId, Guid imageId, Guid revision, CancellationToken cancellationToken)
    {
        await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);
        if (!await ClaimAsync(productId, revision, cancellationToken)) return false;
        var image = await context.ProductImages.AsNoTracking().SingleOrDefaultAsync(image => image.Id == imageId && image.ProductId == productId, cancellationToken);
        if (image is null) return false;
        await context.Products.Where(product => product.Id == productId).ExecuteUpdateAsync(update =>
            update.SetProperty(product => product.ImageUrl, Url(image.Id)).SetProperty(product => product.ImageAlt, image.AlternativeText), cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return true;
    }

    public async Task<bool> DeleteAsync(Guid productId, Guid imageId, Guid revision, CancellationToken cancellationToken)
    {
        await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);
        if (!await ClaimAsync(productId, revision, cancellationToken)) return false;
        var deleted = await context.ProductImages.Where(image => image.Id == imageId && image.ProductId == productId).ExecuteDeleteAsync(cancellationToken);
        if (deleted == 0) return false;
        var replacement = await context.ProductImages.AsNoTracking().Where(image => image.ProductId == productId)
            .OrderBy(image => image.Ordinal).ThenBy(image => image.Id)
            .Select(image => new { image.Id, image.AlternativeText }).FirstOrDefaultAsync(cancellationToken);
        await context.Products.Where(product => product.Id == productId && product.ImageUrl == Url(imageId))
            .ExecuteUpdateAsync(update => update.SetProperty(product => product.ImageUrl, replacement == null ? null : Url(replacement.Id))
                .SetProperty(product => product.ImageAlt, replacement == null ? "" : replacement.AlternativeText)
                .SetProperty(product => product.IsPublished, product => replacement != null && product.IsPublished), cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return true;
    }
}
