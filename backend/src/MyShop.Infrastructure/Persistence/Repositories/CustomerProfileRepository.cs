using Microsoft.EntityFrameworkCore;
using MyShop.Application.Customers.Abstractions;
using MyShop.Infrastructure.Persistence.Models;

namespace MyShop.Infrastructure.Persistence.Repositories;

internal sealed class CustomerProfileRepository(MyShopDbContext context) : ICustomerProfileRepository
{
    public Task<CustomerProfileSnapshot?> GetAsync(string userId, CancellationToken cancellationToken) =>
        context.CustomerProfiles.AsNoTracking().Where(profile => profile.UserId == userId)
            .Select(profile => new CustomerProfileSnapshot(profile.UserId, profile.Name, profile.AddressLine,
                profile.PostalCode, profile.City, profile.CountryCode, profile.Version))
            .SingleOrDefaultAsync(cancellationToken);

    public async Task<CustomerProfileSnapshot?> SaveAsync(string userId, string name, string addressLine,
        string postalCode, string city, string countryCode, Guid? expectedRevision, CancellationToken cancellationToken)
    {
        var replacement = Guid.NewGuid();
        if (expectedRevision is null)
        {
            if (await context.CustomerProfiles.AnyAsync(profile => profile.UserId == userId, cancellationToken)) return null;
            context.CustomerProfiles.Add(new CustomerProfilePersistence { Id = Guid.NewGuid(), UserId = userId, Name = name,
                AddressLine = addressLine, PostalCode = postalCode, City = city, CountryCode = countryCode,
                Version = replacement });
            try { await context.SaveChangesAsync(cancellationToken); }
            catch (DbUpdateException) { return null; }
        }
        else
        {
            var changed = await context.CustomerProfiles
                .Where(profile => profile.UserId == userId && profile.Version == expectedRevision)
                .ExecuteUpdateAsync(update => update.SetProperty(profile => profile.Name, name)
                    .SetProperty(profile => profile.AddressLine, addressLine)
                    .SetProperty(profile => profile.PostalCode, postalCode)
                    .SetProperty(profile => profile.City, city)
                    .SetProperty(profile => profile.CountryCode, countryCode)
                    .SetProperty(profile => profile.Version, replacement), cancellationToken);
            if (changed == 0) return null;
        }
        return new(userId, name, addressLine, postalCode, city, countryCode, replacement);
    }
}
