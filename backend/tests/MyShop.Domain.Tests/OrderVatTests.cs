using MyShop.Domain.Catalog;
using MyShop.Domain.Checkout;
namespace MyShop.Domain.Tests;
public sealed class OrderVatTests
{
    [Fact]
    public void Captures_tax_per_line_and_leaves_unknown_legacy_tax_unspecified()
    {
        var product = Guid.NewGuid(); var variant = Guid.NewGuid();
        var customer = OrderCustomer.Create("Customer", "customer@example.test");
        var address = DeliveryAddress.Create("Street 1", "1234 AB", "Utrecht", "NL");
        var lines = new[] { (product, variant, "Product", "Variant", 2, Money.Create(121, "EUR")) };
        var taxed = Order.Place(Guid.NewGuid(), DateTimeOffset.UtcNow, customer, address, lines,
            vatRates: new Dictionary<(Guid, Guid), (decimal, bool)> { [(product, variant)] = (21, false) });
        var line = Assert.Single(taxed.Lines);
        Assert.Equal(242m, line.Total.Amount); Assert.Equal(200m, line.NetAmount); Assert.Equal(42m, line.VatAmount);
        Assert.Equal(line.Total.Amount, line.NetAmount + line.VatAmount); Assert.Equal(21m, line.VatRate);
        var legacy = Order.Place(Guid.NewGuid(), DateTimeOffset.UtcNow, customer, address, lines);
        Assert.Null(legacy.Lines[0].VatRate); Assert.Null(legacy.Lines[0].NetAmount);
    }
}
