using MyShop.Application.Customers.Abstractions;

namespace MyShop.Application.Customers.GetCustomerProfile;

public sealed class GetCustomerProfile(ICustomerProfileRepository profiles)
{
    private readonly ICustomerProfileRepository profiles = profiles ?? throw new ArgumentNullException(nameof(profiles));

    public Task<CustomerProfileSnapshot?> ExecuteAsync(string userId, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userId);
        cancellationToken.ThrowIfCancellationRequested();
        return profiles.GetAsync(userId, cancellationToken);
    }
}
