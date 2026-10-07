using Microsoft.EntityFrameworkCore;
using MyShop.Domain.Catalog;
using MyShop.Domain.Checkout;
using MyShop.Infrastructure.Persistence.Repositories;
namespace MyShop.Infrastructure.Tests.Integration;
[Collection(SqlServerCollection.Name)]
public sealed class OrderVatStorageTests(SqlServerDatabase database)
{
    [SqlServerFact]
    public async Task Stores_immutable_tax_amounts_for_online_and_pay_later_orders()
    {
        foreach (var method in new[] { OrderPaymentMethod.PayLater, OrderPaymentMethod.Online })
        {
            var product = Guid.NewGuid(); var variant = Guid.NewGuid();
            var order = Order.Place(Guid.NewGuid(), DateTimeOffset.UtcNow, OrderCustomer.Create("Customer", "customer@example.test"),
                DeliveryAddress.Create("Street 1", "1234 AB", "Utrecht", "NL"),
                [(product, variant, "Product", "Variant", 2, Money.Create(121, "EUR"))], paymentMethod: method,
                vatRates: new Dictionary<(Guid, Guid), (decimal, bool)> { [(product, variant)] = (21, false) });
            await using (var context = database.CreateContext())
            {
                var repository = new OrderRepository(context);
                if (method == OrderPaymentMethod.Online) await repository.AddPaidAsync(order, Guid.NewGuid(), "paid", [], CancellationToken.None);
                else await repository.AddAsync(order, Guid.NewGuid(), null, [], CancellationToken.None);
            }
            await using var read = database.CreateContext();
            var saved = await read.OrderLines.AsNoTracking().SingleAsync(line => line.OrderId == order.Id);
            Assert.Equal(21m, saved.VatRate); Assert.Equal(200m, saved.NetAmount); Assert.Equal(42m, saved.VatAmount);
            Assert.Equal(242m, saved.TotalAmount); Assert.False(saved.VatExempt);
        }
    }
}
