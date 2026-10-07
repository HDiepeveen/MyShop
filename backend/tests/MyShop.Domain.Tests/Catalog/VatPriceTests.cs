using MyShop.Domain.Catalog;
namespace MyShop.Domain.Tests.Catalog;
public sealed class VatPriceTests
{
    [Theory]
    [InlineData(100, 21, 21, 121)]
    [InlineData(100, 9, 9, 109)]
    [InlineData(100, 0, 0, 100)]
    [InlineData(0, 21, 0, 0)]
    [InlineData(0.5, 9, 0.05, 0.55)]
    [InlineData(1.5, 9, 0.14, 1.64)]
    [InlineData(100.50, 21, 21.11, 121.61)]
    public void Calculates_gross_and_vat_with_decimal_arithmetic_rounding(decimal net, decimal rate, decimal vat, decimal gross)
    {
        var result = VatPrice.FromNet(Money.Create(net, "EUR"), rate);
        Assert.Equal(net, result.Net.Amount); Assert.Equal(vat, result.Vat.Amount); Assert.Equal(gross, result.Gross.Amount);
        Assert.Equal(result.Gross.Amount, result.Net.Amount + result.Vat.Amount);
        Assert.Equal(result, VatPrice.FromGross(result.Gross, rate));
    }
    [Fact]
    public void Zero_rate_and_exemption_remain_distinct()
    {
        var zero = VatPrice.FromNet(Money.Create(100, "EUR"), 0);
        var exempt = VatPrice.FromNet(Money.Create(100, "EUR"), 0, true);
        Assert.Equal(zero.Gross, exempt.Gross); Assert.NotEqual(zero, exempt);
    }
    [Theory]
    [InlineData(-1)][InlineData(101)][InlineData(21.111)]
    public void Rejects_invalid_rates(decimal rate) =>
        Assert.Throws<ArgumentException>(() => VatPrice.FromNet(Money.Create(100, "EUR"), rate));
    [Fact]
    public void Rejects_exemption_with_nonzero_rate_and_gross_overflow()
    {
        Assert.Throws<ArgumentException>(() => VatPrice.FromNet(Money.Create(100, "EUR"), 21, true));
        Assert.ThrowsAny<ArgumentException>(() => VatPrice.FromNet(Money.Create(9999999999999999.99m, "EUR"), 21));
    }
}
