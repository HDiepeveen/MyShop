using MyShop.Application.Customers.Abstractions;

namespace MyShop.Application.Customers.UpdateCustomerProfile;

public sealed record UpdateCustomerProfileCommand(string UserId, string Name, string AddressLine,
    string PostalCode, string City, string CountryCode, Guid? Revision);
public enum UpdateCustomerProfileFailure { ConcurrencyConflict }
public sealed record UpdateCustomerProfileResult(CustomerProfileSnapshot? Profile, UpdateCustomerProfileFailure? Failure);

public sealed class UpdateCustomerProfile(ICustomerProfileRepository profiles)
{
    private readonly ICustomerProfileRepository profiles = profiles ?? throw new ArgumentNullException(nameof(profiles));

    public async Task<UpdateCustomerProfileResult> ExecuteAsync(UpdateCustomerProfileCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        cancellationToken.ThrowIfCancellationRequested();
        var userId = Required(command.UserId, 450, nameof(command.UserId));
        var name = Required(command.Name, 200, nameof(command.Name));
        var address = Required(command.AddressLine, 200, nameof(command.AddressLine));
        var postalCode = Required(command.PostalCode, 32, nameof(command.PostalCode));
        var city = Required(command.City, 100, nameof(command.City));
        var country = Required(command.CountryCode, 2, nameof(command.CountryCode)).ToUpperInvariant();
        if (country.Length != 2 || country.Any(character => character is < 'A' or > 'Z'))
            throw new ArgumentException("Country code must contain two letters.", nameof(command));
        var saved = await profiles.SaveAsync(userId, name, address, postalCode, city, country,
            command.Revision, cancellationToken);
        return saved is null ? new(null, UpdateCustomerProfileFailure.ConcurrencyConflict) : new(saved, null);
    }

    private static string Required(string value, int maximum, string parameter)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value, parameter);
        value = value.Trim();
        if (value.Length > maximum) throw new ArgumentException($"Value must contain at most {maximum} characters.", parameter);
        return value;
    }
}
