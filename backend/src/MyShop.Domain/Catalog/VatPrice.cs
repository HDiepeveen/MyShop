namespace MyShop.Domain.Catalog;

public sealed record VatPrice
{
    private VatPrice(Money net, Money vat, Money gross, decimal rate, bool exempt)
    { Net = net; Vat = vat; Gross = gross; Rate = rate; Exempt = exempt; }
    public Money Net { get; }
    public Money Vat { get; }
    public Money Gross { get; }
    public decimal Rate { get; }
    public bool Exempt { get; }
    public static VatPrice FromNet(Money net, decimal rate, bool exempt = false)
    {
        Validate(rate, exempt);
        if (net == default) throw new ArgumentException("Price is required.", nameof(net));
        var vat = Money.Create(decimal.Round(net.Amount * rate / 100m, 2, MidpointRounding.AwayFromZero), net.Currency);
        var gross = Money.Create(net.Amount + vat.Amount, net.Currency);
        return new(net, vat, gross, rate, exempt);
    }
    public static VatPrice FromGross(Money gross, decimal rate, bool exempt = false)
    {
        Validate(rate, exempt);
        if (gross == default) throw new ArgumentException("Price is required.", nameof(gross));
        var vat = Money.Create(decimal.Round(gross.Amount * rate / (100m + rate), 2, MidpointRounding.AwayFromZero), gross.Currency);
        var net = Money.Create(gross.Amount - vat.Amount, gross.Currency);
        return new(net, vat, gross, rate, exempt);
    }
    private static void Validate(decimal rate, bool exempt)
    {
        if (rate < 0 || rate > 100 || decimal.Round(rate, 2) != rate || (exempt && rate != 0))
            throw new ArgumentException("VAT must be a percentage from 0 to 100 with at most two decimals; exemption requires zero.", nameof(rate));
    }
}
