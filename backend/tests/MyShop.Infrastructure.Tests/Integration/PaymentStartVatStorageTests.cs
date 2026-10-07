using Microsoft.EntityFrameworkCore;
using MyShop.Application.Checkout.Abstractions;
using MyShop.Infrastructure.Persistence.Repositories;
namespace MyShop.Infrastructure.Tests.Integration;
[Collection(SqlServerCollection.Name)]
public sealed class PaymentStartVatStorageTests(SqlServerDatabase database)
{
    [SqlServerFact]
    public async Task Round_trips_the_vat_treatment_captured_at_payment_start()
    {
        var token = Guid.NewGuid();
        var start = new OnlinePaymentStartRecord(token, "TestPay", "reference-" + token.ToString("N"), "provider-" + token.ToString("N"),
            new Uri("https://example.test/pay"), new("Customer", "customer@example.test"), new("Street 1", "1234 AB", "Utrecht", "NL"),
            [new(Guid.NewGuid(), Guid.NewGuid(), "Product", "Variant", 2, 121m, "EUR", 242m, 21m, false)],
            [new("EUR", 242m)], new(Guid.NewGuid(), "Delivery", null, 0m, "EUR"), DateTimeOffset.UtcNow);
        await using (var context = database.CreateContext())
            await new OnlinePaymentStartRepository(context).SaveAsync(start, CancellationToken.None);
        await using var read = database.CreateContext();
        var saved = (await new OnlinePaymentStartRepository(read).GetByCheckoutTokenAsync(token, CancellationToken.None))!;
        Assert.Equal(21m, saved.Lines[0].VatRate); Assert.False(saved.Lines[0].VatExempt);
        Assert.Equal(121m, saved.Lines[0].UnitAmount); Assert.Equal(242m, saved.Lines[0].TotalAmount);
    }
}
