using MyShop.Application.Checkout.Abstractions;
using MyShop.Application.Checkout.GetAdminPaymentOptions;
using MyShop.Application.Checkout.GetPaymentOptions;
using MyShop.Application.Checkout.UpdatePaymentOptions;

namespace MyShop.Application.Tests.Checkout;

public sealed class PaymentOptionsTests
{
    [Theory]
    [InlineData(true, false, false, "payLater")]
    [InlineData(false, true, true, "online")]
    [InlineData(true, true, true, "payLater,online")]
    [InlineData(false, true, false, "")]
    public async Task PublicOptionsContainOnlyEnabledAndAvailableMethods(
        bool payLater, bool online, bool configured, string expected)
    {
        var store = new Store(payLater, online, "Betaal binnen 14 dagen.");
        var result = await new GetPaymentOptions(store, new Availability(configured))
            .ExecuteAsync(CancellationToken.None);

        Assert.Equal(expected, string.Join(',', result.Items.Select(option => option.Code)));
        if (payLater)
            Assert.Equal("Betaal binnen 14 dagen.", result.Items.Single(option => option.Code == "payLater").Instructions);
    }

    [Fact]
    public async Task AdminOptionsIncludePersistedSettingsAvailabilityAndRevision()
    {
        var store = new Store(true, false, "Betaal binnen 14 dagen.");
        var result = await new GetAdminPaymentOptions(store, new Availability(false))
            .ExecuteAsync(CancellationToken.None);

        Assert.True(result.PayLaterEnabled);
        Assert.False(result.OnlinePaymentEnabled);
        Assert.False(result.OnlinePaymentConfigured);
        Assert.Equal("Betaal binnen 14 dagen.", result.PayLaterInstructions);
        Assert.Equal(store.Revision, result.Revision);
    }

    [Fact]
    public async Task UpdatePersistsAvailableChoiceAndReturnsNewRevision()
    {
        var store = new Store(true, false);
        var result = await new UpdatePaymentOptions(store, new Availability(true))
            .ExecuteAsync(new(false, true, "  Betaal binnen 14 dagen.  ", store.Revision), CancellationToken.None);

        Assert.Null(result.Failure);
        Assert.False(result.Settings!.PayLaterEnabled);
        Assert.True(result.Settings.OnlinePaymentEnabled);
        Assert.Equal("Betaal binnen 14 dagen.", result.Settings.PayLaterInstructions);
        Assert.NotEqual(Guid.Empty, result.Settings.Revision);
        Assert.Equal(1, store.SaveCalls);
    }

    [Fact]
    public async Task UpdateRejectsAnUnavailableOnlineMethodWithoutSaving()
    {
        var store = new Store(true, false);
        var result = await new UpdatePaymentOptions(store, new Availability(false))
            .ExecuteAsync(new(true, true, null, store.Revision), CancellationToken.None);

        Assert.Equal(UpdatePaymentOptionsFailure.OnlinePaymentNotConfigured, result.Failure);
        Assert.Equal(0, store.SaveCalls);
    }

    [Fact]
    public async Task UpdateReportsAStaleRevision()
    {
        var store = new Store(true, false) { RejectSave = true };
        var result = await new UpdatePaymentOptions(store, new Availability(true))
            .ExecuteAsync(new(true, false, null, store.Revision), CancellationToken.None);

        Assert.Equal(UpdatePaymentOptionsFailure.ConcurrencyConflict, result.Failure);
    }

    [Fact]
    public async Task UpdateRequiresARevisionAndAtLeastOneMethod()
    {
        var store = new Store(true, false);
        var useCase = new UpdatePaymentOptions(store, new Availability(true));

        await Assert.ThrowsAsync<ArgumentException>(() =>
            useCase.ExecuteAsync(new(true, false, null, Guid.Empty), CancellationToken.None));
        await Assert.ThrowsAsync<ArgumentException>(() =>
            useCase.ExecuteAsync(new(false, false, null, store.Revision), CancellationToken.None));
        Assert.Equal(0, store.SaveCalls);
    }

    [Fact]
    public async Task UpdateRejectsOverlongInstructionsBeforeSaving()
    {
        var store = new Store(true, false);
        await Assert.ThrowsAsync<ArgumentException>(() => new UpdatePaymentOptions(
            store, new Availability(true)).ExecuteAsync(
                new(true, false, new string('x', 2001), store.Revision), CancellationToken.None));
        Assert.Equal(0, store.SaveCalls);
    }

    private sealed class Availability(bool configured) : IOnlinePaymentAvailability
    {
        public bool IsConfigured => configured;
    }

    private sealed class Store(bool payLater, bool online, string? instructions = null) : IPaymentOptionsRepository
    {
        public Guid Revision { get; } = Guid.NewGuid();
        public bool RejectSave { get; init; }
        public int SaveCalls { get; private set; }

        public Task<PaymentOptionsSnapshot> GetAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(new PaymentOptionsSnapshot(payLater, online, instructions, Revision));
        }

        public Task<PaymentOptionsSnapshot?> SaveAsync(bool payLaterEnabled, bool onlinePaymentEnabled,
            string? payLaterInstructions, Guid expectedRevision, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            SaveCalls++;
            return Task.FromResult<PaymentOptionsSnapshot?>(RejectSave
                ? null
                : new(payLaterEnabled, onlinePaymentEnabled, payLaterInstructions, Guid.NewGuid()));
        }
    }
}
