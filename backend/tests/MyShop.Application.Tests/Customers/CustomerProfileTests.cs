using MyShop.Application.Customers.Abstractions;
using MyShop.Application.Customers.GetCustomerProfile;
using MyShop.Application.Customers.UpdateCustomerProfile;

namespace MyShop.Application.Tests.Customers;

public sealed class CustomerProfileTests
{
    [Fact]
    public async Task SavesFreeCountryAndContactDetailsAndRequiresPhone()
    {
        var useCase = new UpdateCustomerProfile(new Repository());
        await Assert.ThrowsAsync<ArgumentException>(() => useCase.ExecuteAsync(new(
            "user", "Ada", "Street 1", "1234 AB", "Paramaribo", "Suriname", null), default));
        var result = await useCase.ExecuteAsync(new(
            "user", "Ada", "Street 1", "1234 AB", "Paramaribo", " Suriname ", null,
            " +597 123456 "), default);
        Assert.Equal("Suriname", result.Profile!.CountryCode);
        Assert.Equal("+597 123456", result.Profile.PhoneNumber);
    }

    [Fact]
    public async Task SavesTrimmedProfileWithNormalizedCountryAndRevision()
    {
        var repository = new Repository();
        var result = await new UpdateCustomerProfile(repository).ExecuteAsync(new(
            "user", " Ada ", " Straat 1 ", " 1234 AB ", " Utrecht ", "nl", null, "0301234567"), default);
        Assert.Null(result.Failure);
        Assert.Equal("Ada", result.Profile!.Name);
        Assert.Equal("NL", result.Profile.CountryCode);
        Assert.NotEqual(Guid.Empty, result.Profile.Revision);
        Assert.Equal(result.Profile, await new GetCustomerProfile(repository).ExecuteAsync("user", default));
    }

    [Fact]
    public async Task ReportsConcurrencyAndRejectsInvalidCountry()
    {
        var repository = new Repository { Reject = true };
        var result = await new UpdateCustomerProfile(repository).ExecuteAsync(new(
            "user", "Ada", "Straat 1", "1234 AB", "Utrecht", "NL", Guid.NewGuid(), "0301234567"), default);
        Assert.Equal(UpdateCustomerProfileFailure.ConcurrencyConflict, result.Failure);
        await Assert.ThrowsAsync<ArgumentException>(() => new UpdateCustomerProfile(repository).ExecuteAsync(new(
            "user", "Ada", "Straat 1", "1234 AB", "Utrecht", " ", null, "0301234567"), default));
    }

    private sealed class Repository : ICustomerProfileRepository
    {
        public bool Reject { get; init; }
        private CustomerProfileSnapshot? profile;
        public Task<CustomerProfileSnapshot?> GetAsync(string userId, CancellationToken cancellationToken) => Task.FromResult(profile);
        public Task<CustomerProfileSnapshot?> SaveAsync(string userId, string name, string addressLine,
            string postalCode, string city, string countryCode, Guid? expectedRevision, CancellationToken cancellationToken, string phoneNumber = "")
        {
            profile = Reject ? null : new(userId, name, addressLine, postalCode, city, countryCode, Guid.NewGuid(), phoneNumber);
            return Task.FromResult(profile);
        }
    }
}
