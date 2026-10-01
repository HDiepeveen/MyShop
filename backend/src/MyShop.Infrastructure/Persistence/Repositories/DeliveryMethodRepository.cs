using Microsoft.EntityFrameworkCore;
using MyShop.Application.Checkout.Abstractions;
using MyShop.Infrastructure.Persistence.Models;

namespace MyShop.Infrastructure.Persistence.Repositories;

internal sealed class DeliveryMethodRepository(MyShopDbContext context) : IDeliveryMethodRepository
{
    public async Task<IReadOnlyList<DeliveryMethodSnapshot>> ListAsync(bool enabledOnly,
        CancellationToken cancellationToken)
    {
        var query = context.DeliveryMethods.AsNoTracking();
        if (enabledOnly) query = query.Where(method => method.Enabled);
        return await query.OrderBy(method => method.Name).ThenBy(method => method.Id)
            .Select(method => Map(method)).ToArrayAsync(cancellationToken);
    }

    public Task<DeliveryMethodSnapshot?> GetAsync(Guid id, CancellationToken cancellationToken) =>
        context.DeliveryMethods.AsNoTracking().Where(method => method.Id == id)
            .Select(method => Map(method)).SingleOrDefaultAsync(cancellationToken);

    public Task<bool> NameExistsAsync(string name, Guid? excludingId,
        CancellationToken cancellationToken) => context.DeliveryMethods.AnyAsync(method =>
            method.Name == name && (excludingId == null || method.Id != excludingId), cancellationToken);

    public async Task<DeliveryMethodSnapshot> AddAsync(string name, string? description,
        decimal amount, string currency, bool enabled, CancellationToken cancellationToken)
    {
        var method = new DeliveryMethodPersistence { Id = Guid.NewGuid(), Name = name,
            Description = description, Amount = amount, Currency = currency, Enabled = enabled,
            Version = Guid.NewGuid() };
        context.DeliveryMethods.Add(method);
        await context.SaveChangesAsync(cancellationToken);
        return Map(method);
    }

    public async Task<DeliveryMethodSnapshot?> UpdateAsync(Guid id, string name,
        string? description, decimal amount, string currency, bool enabled, Guid expectedRevision,
        CancellationToken cancellationToken)
    {
        var replacement = Guid.NewGuid();
        var changed = await context.DeliveryMethods.Where(method => method.Id == id
                && method.Version == expectedRevision)
            .ExecuteUpdateAsync(update => update.SetProperty(method => method.Name, name)
                .SetProperty(method => method.Description, description)
                .SetProperty(method => method.Amount, amount)
                .SetProperty(method => method.Currency, currency)
                .SetProperty(method => method.Enabled, enabled)
                .SetProperty(method => method.Version, replacement), cancellationToken);
        return changed == 0 ? null : new(id, name, description, amount, currency, enabled, replacement);
    }

    public async Task<bool> DeleteAsync(Guid id, Guid expectedRevision,
        CancellationToken cancellationToken) => await context.DeliveryMethods
        .Where(method => method.Id == id && method.Version == expectedRevision)
        .ExecuteDeleteAsync(cancellationToken) == 1;

    private static DeliveryMethodSnapshot Map(DeliveryMethodPersistence method) =>
        new(method.Id, method.Name, method.Description, method.Amount, method.Currency,
            method.Enabled, method.Version);
}
