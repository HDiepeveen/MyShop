namespace MyShop.Application.Customers.Abstractions;

public sealed record CustomerProfileSnapshot(string UserId, string Name, string AddressLine,
    string PostalCode, string City, string CountryCode, Guid Revision);

public interface ICustomerProfileRepository
{
    Task<CustomerProfileSnapshot?> GetAsync(string userId, CancellationToken cancellationToken);
    Task<CustomerProfileSnapshot?> SaveAsync(string userId, string name, string addressLine,
        string postalCode, string city, string countryCode, Guid? expectedRevision,
        CancellationToken cancellationToken);
}
