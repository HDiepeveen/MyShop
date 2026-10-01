namespace MyShop.Application.Customers.Abstractions;

public interface IManagedCustomerRepository
{
    Task<ManagedCustomerPage> ListAsync(int offset, int limit, string? search,
        DateTimeOffset now, CancellationToken cancellationToken);
    Task<ManagedCustomerDetail?> GetAsync(string userId, DateTimeOffset now,
        CancellationToken cancellationToken);
    Task<string?> SetLockedAsync(string userId, string expectedRevision, bool locked,
        CancellationToken cancellationToken);
}

public sealed record ManagedCustomerListItem(string Id, string Email, string? Name, bool IsLocked,
    DateTimeOffset? LockedUntil, int OrderCount);
public sealed record ManagedCustomerPage(IReadOnlyList<ManagedCustomerListItem> Items, int TotalCount);
public sealed record ManagedCustomerDetail(string Id, string Email, string? Name, string? AddressLine,
    string? PostalCode, string? City, string? CountryCode, bool IsLocked,
    DateTimeOffset? LockedUntil, int OrderCount, DateTimeOffset? LastOrderAt, string Revision);
