using MyShop.Domain.Catalog;
using MyShop.Domain.Checkout;

namespace MyShop.Domain.Tests.Checkout;

public sealed class OrderTests
{
    [Fact]
    public void PlaceCapturesTrimmedCustomerAddressLinesAndExactTotals()
    {
        var id = Guid.NewGuid();
        var order = Order.Place(id, DateTimeOffset.Parse("2026-09-30T10:00:00+02:00"),
            OrderCustomer.Create("  Ada Lovelace ", " ada@example.com "),
            DeliveryAddress.Create(" Main street 1 ", " 1234 AB ", " Amsterdam ", " nl "),
            [
                (Guid.NewGuid(), Guid.NewGuid(), " Shirt ", " Small ", 2, Money.Create(12.50m, "eur")),
                (Guid.NewGuid(), Guid.NewGuid(), " Socks ", " Pair ", 1, Money.Create(5m, "EUR"))
            ]);

        Assert.Equal(id, order.Id);
        Assert.Equal($"MS-{id:N}".ToUpperInvariant(), order.Number);
        Assert.Equal(DateTimeOffset.Parse("2026-09-30T08:00:00Z"), order.PlacedAt);
        Assert.Equal("Ada Lovelace", order.Customer.Name);
        Assert.Equal("NL", order.DeliveryAddress.CountryCode);
        Assert.Equal(OrderPaymentMethod.PayLater, order.PaymentMethod);
        Assert.Equal(OrderStatus.AwaitingPayment, order.Status);
        Assert.Equal(30m, Assert.Single(order.Totals).Amount);
    }

    [Theory]
    [InlineData("", "ada@example.com")]
    [InlineData("Ada", "invalid")]
    public void CustomerRejectsInvalidValues(string name, string email) =>
        Assert.Throws<ArgumentException>(() => OrderCustomer.Create(name, email));

    [Theory]
    [InlineData("", "1234 AB", "Amsterdam", "NL")]
    [InlineData("Street 1", "1234 AB", "Amsterdam", "NLD")]
    [InlineData("Street 1", "1234 AB", "Amsterdam", "1L")]
    public void AddressRejectsInvalidValues(string line, string postalCode, string city, string country) =>
        Assert.Throws<ArgumentException>(() => DeliveryAddress.Create(line, postalCode, city, country));

    [Fact]
    public void PlaceRejectsDuplicateOrEmptyLines()
    {
        var product = Guid.NewGuid();
        var variant = Guid.NewGuid();
        var customer = OrderCustomer.Create("Ada", "ada@example.com");
        var address = DeliveryAddress.Create("Street 1", "1234 AB", "Amsterdam", "NL");
        Assert.Throws<ArgumentException>(() => Order.Place(Guid.NewGuid(), DateTimeOffset.UtcNow,
            customer, address, []));
        Assert.Throws<ArgumentException>(() => Order.Place(Guid.NewGuid(), DateTimeOffset.UtcNow,
            customer, address, [
                (product, variant, "Shirt", "Small", 1, Money.Create(10, "EUR")),
                (product, variant, "Shirt", "Small", 1, Money.Create(10, "EUR"))
            ]));
    }
}
