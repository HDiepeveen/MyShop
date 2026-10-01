using MyShop.Application.Customers.Abstractions;

namespace MyShop.Application.Customers.ManageCustomers;

public sealed record ListManagedCustomersQuery(int Offset = 0, int Limit = 20, string? Search = null);
public sealed class ListManagedCustomers(IManagedCustomerRepository customers)
{
    public async Task<ManagedCustomerPage> ExecuteAsync(ListManagedCustomersQuery query,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query); cancellationToken.ThrowIfCancellationRequested();
        if (query.Offset < 0 || query.Limit is < 1 or > 100) throw new ArgumentOutOfRangeException(nameof(query));
        var search = query.Search?.Trim();
        if (search?.Length > 200) throw new ArgumentException("Search must not exceed 200 characters.");
        return await customers.ListAsync(query.Offset, query.Limit,
            string.IsNullOrEmpty(search) ? null : search, DateTimeOffset.UtcNow, cancellationToken);
    }
}

public sealed class GetManagedCustomer(IManagedCustomerRepository customers)
{
    public Task<ManagedCustomerDetail?> ExecuteAsync(string id, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested(); ArgumentException.ThrowIfNullOrWhiteSpace(id);
        return customers.GetAsync(id, DateTimeOffset.UtcNow, cancellationToken);
    }
}

public sealed record SetCustomerLockCommand(string Id, string Revision, bool Locked);
public enum SetCustomerLockFailure { NotFound, ConcurrencyConflict }
public sealed record SetCustomerLockResult(string? Revision, SetCustomerLockFailure? Failure);
public sealed class SetCustomerLock(IManagedCustomerRepository customers)
{
    public async Task<SetCustomerLockResult> ExecuteAsync(SetCustomerLockCommand command,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command); cancellationToken.ThrowIfCancellationRequested();
        ArgumentException.ThrowIfNullOrWhiteSpace(command.Id); ArgumentException.ThrowIfNullOrWhiteSpace(command.Revision);
        var current = await customers.GetAsync(command.Id, DateTimeOffset.UtcNow, cancellationToken);
        if (current is null) return new(null, SetCustomerLockFailure.NotFound);
        if (!string.Equals(current.Revision, command.Revision, StringComparison.Ordinal))
            return new(null, SetCustomerLockFailure.ConcurrencyConflict);
        var revision = await customers.SetLockedAsync(command.Id, command.Revision, command.Locked, cancellationToken);
        return revision is null ? new(null, SetCustomerLockFailure.ConcurrencyConflict) : new(revision, null);
    }
}
