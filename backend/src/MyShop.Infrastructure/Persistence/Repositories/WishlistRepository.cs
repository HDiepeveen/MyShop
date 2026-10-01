using Microsoft.EntityFrameworkCore;
using MyShop.Application.Customers.Abstractions;
using MyShop.Infrastructure.Persistence.Models;

namespace MyShop.Infrastructure.Persistence.Repositories;

internal sealed class WishlistRepository(MyShopDbContext context) : IWishlistRepository
{
    public async Task<WishlistPage> ListAsync(string userId, int offset, int limit,
        CancellationToken cancellationToken)
    {
        var query = context.WishlistItems.Where(item => item.UserId == userId);
        var total = await query.CountAsync(cancellationToken);
        var items = await query.OrderByDescending(item => item.AddedAt).ThenBy(item => item.ProductId)
            .Skip(offset).Take(limit).Select(item => new WishlistItem(item.ProductId,
                item.Product.Name, item.Product.ImageUrl, item.Product.ImageAlt,
                item.Product.IsPublished, item.AddedAt)).ToArrayAsync(cancellationToken);
        return new(items, total);
    }

    public Task<bool> ContainsAsync(string userId, Guid productId, CancellationToken cancellationToken) =>
        context.WishlistItems.AnyAsync(item => item.UserId == userId && item.ProductId == productId,
            cancellationToken);

    public async Task<bool> AddAsync(string userId, Guid productId, DateTimeOffset addedAt,
        CancellationToken cancellationToken)
    {
        if (!await context.Products.AnyAsync(product => product.Id == productId && product.IsPublished,
                cancellationToken)) return false;
        if (await ContainsAsync(userId, productId, cancellationToken)) return true;
        var item = new WishlistItemPersistence
            { UserId = userId, ProductId = productId, AddedAt = addedAt };
        context.WishlistItems.Add(item);
        try
        {
            await context.SaveChangesAsync(cancellationToken);
            return true;
        }
        catch (DbUpdateException)
        {
            context.Entry(item).State = EntityState.Detached;
            if (await ContainsAsync(userId, productId, cancellationToken)) return true;
            throw;
        }
    }

    public async Task RemoveAsync(string userId, Guid productId, CancellationToken cancellationToken)
    {
        await context.WishlistItems.Where(item => item.UserId == userId && item.ProductId == productId)
            .ExecuteDeleteAsync(cancellationToken);
    }
}
