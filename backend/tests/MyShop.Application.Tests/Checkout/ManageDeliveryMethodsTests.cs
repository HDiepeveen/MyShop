using MyShop.Application.Checkout.Abstractions;
using MyShop.Application.Checkout.ManageDeliveryMethods;

namespace MyShop.Application.Tests.Checkout;

public sealed class ManageDeliveryMethodsTests
{
    [Fact]
    public async Task CreatesTrimmedMethodWithNormalizedMoney()
    {
        var repository = new Repository();
        var result = await new SaveDeliveryMethod(repository).ExecuteAsync(
            new(null, "  Avondbezorging ", " Na 18:00 ", 4.955m, "eur", true, null),
            CancellationToken.None);
        Assert.Null(result.Failure);
        Assert.Equal("Avondbezorging", result.Method!.Name);
        Assert.Equal("Na 18:00", result.Method.Description);
        Assert.Equal(4.96m, result.Method.Amount);
        Assert.Equal("EUR", result.Method.Currency);
    }

    [Fact]
    public async Task RejectsDuplicateName()
    {
        var repository = new Repository { Duplicate = true };
        await Assert.ThrowsAsync<ArgumentException>(() => new SaveDeliveryMethod(repository)
            .ExecuteAsync(new(null, "Standaardbezorging", null, 0m, "EUR", true, null),
                CancellationToken.None));
    }

    [Fact]
    public async Task ReportsStaleUpdate()
    {
        var repository = new Repository { Existing = new(Guid.NewGuid(), "Oud", null, 0m,
            "EUR", true, Guid.NewGuid()), RejectUpdate = true };
        var result = await new SaveDeliveryMethod(repository).ExecuteAsync(new(repository.Existing.Id,
            "Nieuw", null, 1m, "EUR", true, repository.Existing.Revision), CancellationToken.None);
        Assert.Equal(SaveDeliveryMethodFailure.ConcurrencyConflict, result.Failure);
    }

    private sealed class Repository : IDeliveryMethodRepository
    {
        public bool Duplicate { get; init; }
        public bool RejectUpdate { get; init; }
        public DeliveryMethodSnapshot? Existing { get; init; }
        public Task<bool> NameExistsAsync(string name, Guid? excludingId, CancellationToken cancellationToken) => Task.FromResult(Duplicate);
        public Task<DeliveryMethodSnapshot?> GetAsync(Guid id, CancellationToken cancellationToken) => Task.FromResult(Existing?.Id == id ? Existing : null);
        public Task<IReadOnlyList<DeliveryMethodSnapshot>> ListAsync(bool enabledOnly, CancellationToken cancellationToken) => Task.FromResult<IReadOnlyList<DeliveryMethodSnapshot>>([]);
        public Task<DeliveryMethodSnapshot> AddAsync(string name, string? description, decimal amount, string currency, bool enabled, CancellationToken cancellationToken) => Task.FromResult(new DeliveryMethodSnapshot(Guid.NewGuid(), name, description, amount, currency, enabled, Guid.NewGuid()));
        public Task<DeliveryMethodSnapshot?> UpdateAsync(Guid id, string name, string? description, decimal amount, string currency, bool enabled, Guid expectedRevision, CancellationToken cancellationToken) => Task.FromResult<DeliveryMethodSnapshot?>(RejectUpdate ? null : new(id, name, description, amount, currency, enabled, Guid.NewGuid()));
        public Task<bool> DeleteAsync(Guid id, Guid expectedRevision, CancellationToken cancellationToken) => Task.FromResult(true);
    }
}
