using Microsoft.EntityFrameworkCore;
using MyShop.Application.Customers.Abstractions;

namespace MyShop.Infrastructure.Persistence.Repositories;

internal sealed class ManagedCustomerRepository(MyShopDbContext context) : IManagedCustomerRepository
{
    private IQueryable<Microsoft.AspNetCore.Identity.IdentityUser> Customers() =>
        context.Users.Where(user => context.UserRoles.Any(userRole => userRole.UserId == user.Id
            && context.Roles.Any(role => role.Id == userRole.RoleId && role.NormalizedName == "CUSTOMER")));

    public async Task<ManagedCustomerPage> ListAsync(int offset, int limit, string? search,
        DateTimeOffset now, CancellationToken cancellationToken)
    {
        var query = Customers();
        if (search is not null)
            query = query.Where(user => user.Email!.Contains(search)
                || context.CustomerProfiles.Any(profile => profile.UserId == user.Id
                    && profile.Name.Contains(search)));
        var total = await query.CountAsync(cancellationToken);
        var items = await query.OrderBy(user => user.Email).ThenBy(user => user.Id)
            .Skip(offset).Take(limit).Select(user => new ManagedCustomerListItem(user.Id, user.Email!,
                context.CustomerProfiles.Where(profile => profile.UserId == user.Id)
                    .Select(profile => profile.Name).SingleOrDefault(),
                user.LockoutEnd != null && user.LockoutEnd > now, user.LockoutEnd,
                context.Orders.Count(order => order.CustomerUserId == user.Id)))
            .ToArrayAsync(cancellationToken);
        return new(items, total);
    }

    public Task<ManagedCustomerDetail?> GetAsync(string userId, DateTimeOffset now,
        CancellationToken cancellationToken) => Customers().Where(user => user.Id == userId)
        .Select(user => new ManagedCustomerDetail(user.Id, user.Email!,
            context.CustomerProfiles.Where(profile => profile.UserId == user.Id)
                .Select(profile => profile.Name).SingleOrDefault(),
            context.CustomerProfiles.Where(profile => profile.UserId == user.Id)
                .Select(profile => profile.AddressLine).SingleOrDefault(),
            context.CustomerProfiles.Where(profile => profile.UserId == user.Id)
                .Select(profile => profile.PostalCode).SingleOrDefault(),
            context.CustomerProfiles.Where(profile => profile.UserId == user.Id)
                .Select(profile => profile.City).SingleOrDefault(),
            context.CustomerProfiles.Where(profile => profile.UserId == user.Id)
                .Select(profile => profile.CountryCode).SingleOrDefault(),
            user.LockoutEnd != null && user.LockoutEnd > now, user.LockoutEnd,
            context.Orders.Count(order => order.CustomerUserId == user.Id),
            context.Orders.Where(order => order.CustomerUserId == user.Id)
                .Max(order => (DateTimeOffset?)order.PlacedAt), user.ConcurrencyStamp!))
        .SingleOrDefaultAsync(cancellationToken);

    public async Task<string?> SetLockedAsync(string userId, string expectedRevision, bool locked,
        CancellationToken cancellationToken)
    {
        var revision = Guid.NewGuid().ToString("N");
        var securityStamp = Guid.NewGuid().ToString();
        var changed = await Customers().Where(user => user.Id == userId
                && user.ConcurrencyStamp == expectedRevision)
            .ExecuteUpdateAsync(update => update
                .SetProperty(user => user.LockoutEnabled, true)
                .SetProperty(user => user.LockoutEnd, locked ? DateTimeOffset.MaxValue : null)
                .SetProperty(user => user.AccessFailedCount,
                    user => locked ? user.AccessFailedCount : 0)
                .SetProperty(user => user.SecurityStamp, securityStamp)
                .SetProperty(user => user.ConcurrencyStamp, revision), cancellationToken);
        return changed == 1 ? revision : null;
    }
}
