using System.Globalization;
using System.Text.RegularExpressions;
using MyShop.Application.Checkout.Abstractions;
using MyShop.Domain.Checkout;
using MyShop.Domain.Catalog;

namespace MyShop.Application.Billing;

public sealed record CompanySettings(string Name, string AddressLine, string PostalCode, string City,
    string VatId, string KvkNumber, string InvoicePrefix, Guid Revision);
public sealed record VatRateDefinition(Guid Id, string Name, decimal Percentage, bool Exempt, bool Enabled, Guid Revision);
public sealed record SaveVatRate(Guid? Id, string Name, decimal Percentage, bool Exempt, bool Enabled, Guid? Revision);
public interface IBillingSettingsRepository
{
    Task<CompanySettings> GetCompanyAsync(CancellationToken token);
    Task<CompanySettings?> SaveCompanyAsync(CompanySettings settings, CancellationToken token);
    Task<IReadOnlyList<VatRateDefinition>> ListRatesAsync(bool enabledOnly, CancellationToken token);
    Task<VatRateDefinition?> SaveRateAsync(SaveVatRate rate, CancellationToken token);
}
public sealed record InvoiceBuyer(string Name, string AddressLine, string PostalCode, string City, string CountryCode, string? VatId);
public sealed record InvoiceLine(string Description, int Quantity, string UnitNet, string Net, string Vat, string Gross, decimal Rate, bool Exempt);
public sealed record InvoiceTotal(decimal Rate, bool Exempt, string Net, string Vat, string Gross);
public sealed record InvoiceDocument(Guid Id, Guid OrderId, string Number, string OrderNumber, DateTimeOffset IssuedAt,
    DateOnly SupplyDate, CompanySettings Seller, InvoiceBuyer Buyer, IReadOnlyList<InvoiceLine> Lines,
    IReadOnlyList<InvoiceTotal> Totals, string Net, string Vat, string Gross, string Currency, string TaxStatement);
public sealed record IssueInvoiceCommand(Guid OrderId, Guid OrderRevision, DateOnly SupplyDate, InvoiceBuyer Buyer, bool TaxReviewed, string TaxStatement);
public interface IInvoiceRepository
{
    Task<InvoiceDocument?> GetAsync(Guid orderId, string? customerUserId, CancellationToken token);
    Task<InvoiceDocument?> AddAsync(InvoiceDocument document, Guid orderRevision, CancellationToken token);
}

public sealed class BillingSettings(IBillingSettingsRepository repository)
{
    public Task<CompanySettings> CompanyAsync(CancellationToken token) => repository.GetCompanyAsync(token);
    public Task<IReadOnlyList<VatRateDefinition>> RatesAsync(bool enabledOnly, CancellationToken token) => repository.ListRatesAsync(enabledOnly, token);
    public async Task<CompanySettings?> SaveCompanyAsync(CompanySettings value, CancellationToken token)
    {
        ArgumentNullException.ThrowIfNull(value);
        value = value with { Name = Text(value.Name, 200), AddressLine = Text(value.AddressLine, 200),
            PostalCode = Text(value.PostalCode, 32), City = Text(value.City, 100), VatId = Text(value.VatId, 32).Replace(" ", "").ToUpperInvariant(),
            KvkNumber = Text(value.KvkNumber, 8), InvoicePrefix = Text(value.InvoicePrefix, 20).ToUpperInvariant() };
        if (value.Revision == Guid.Empty || !Regex.IsMatch(value.InvoicePrefix, "^[A-Z0-9-]{1,20}$")
            || (value.VatId.Length > 0 && !Regex.IsMatch(value.VatId, "^NL[0-9]{9}B[0-9]{2}$"))
            || (value.KvkNumber.Length > 0 && !Regex.IsMatch(value.KvkNumber, "^[0-9]{8}$")))
            throw new ArgumentException("Controleer btw-id, KvK-nummer en factuurprefix.");
        return await repository.SaveCompanyAsync(value, token);
    }
    public Task<VatRateDefinition?> SaveRateAsync(SaveVatRate value, CancellationToken token)
    {
        ArgumentNullException.ThrowIfNull(value);
        value = value with { Name = Text(value.Name, 100) };
        if (value.Name.Length == 0 || value.Percentage < 0 || value.Percentage > 100 || decimal.Round(value.Percentage, 2) != value.Percentage
            || (value.Exempt && value.Percentage != 0) || (value.Id is not null && (value.Id == Guid.Empty || value.Revision is null || value.Revision == Guid.Empty)))
            throw new ArgumentException("Gebruik een percentage van 0 tot en met 100 met maximaal twee decimalen; vrijgesteld vereist 0%.");
        return repository.SaveRateAsync(value, token);
    }
    internal static string Text(string? value, int maximum)
    {
        value = value?.Trim() ?? "";
        if (value.Length > maximum || value.Any(char.IsControl)) throw new ArgumentException("Een veld bevat te veel of ongeldige tekens.");
        return value;
    }
}

public sealed class IssueInvoice(IBillingSettingsRepository settings, IOrderReadRepository orders, IInvoiceRepository invoices)
{
    public async Task<InvoiceDocument?> ExecuteAsync(IssueInvoiceCommand command, CancellationToken token)
    {
        ArgumentNullException.ThrowIfNull(command);
        ArgumentNullException.ThrowIfNull(command.Buyer);
        token.ThrowIfCancellationRequested();
        var existing = await invoices.GetAsync(command.OrderId, null, token);
        if (existing is not null) return existing;
        var order = await orders.GetAsync(command.OrderId, token);
        if (order is null || order.Revision != command.OrderRevision || order.Status is OrderStatus.Cancelled or OrderStatus.Refunded) return null;
        var seller = await settings.GetCompanyAsync(token);
        if (new[] { seller.Name, seller.AddressLine, seller.PostalCode, seller.City, seller.VatId }.Any(string.IsNullOrWhiteSpace))
            throw new ArgumentException("Vul eerst de volledige bedrijfsgegevens en het btw-id in.");
        var buyer = command.Buyer with { Name = BillingSettings.Text(command.Buyer.Name, 200), AddressLine = BillingSettings.Text(command.Buyer.AddressLine, 200),
            PostalCode = BillingSettings.Text(command.Buyer.PostalCode, 32), City = BillingSettings.Text(command.Buyer.City, 100),
            CountryCode = BillingSettings.Text(command.Buyer.CountryCode, 2).ToUpperInvariant(), VatId = BillingSettings.Text(command.Buyer.VatId, 32) };
        if (new[] { buyer.Name, buyer.AddressLine, buyer.PostalCode, buyer.City }.Any(string.IsNullOrWhiteSpace)
            || !Regex.IsMatch(buyer.CountryCode, "^[A-Z]{2}$") || command.SupplyDate == default)
            throw new ArgumentException("Vul de volledige factuurgegevens en leveringsdatum of datum van vooruitbetaling in.");
        if (order.Totals.Count != 1 || order.Totals[0].Currency != "EUR")
            throw new ArgumentException("Facturen ondersteunen nu uitsluitend bestellingen in EUR.");
        if (order.Lines.Any(line => line.VatRate is null || line.NetAmount is null || line.VatAmount is null))
            throw new ArgumentException("Deze bestelling bevat oude regels zonder vastgelegde btw. Er wordt geen tarief geraden.");
        if (order.Lines.Count == 0 || order.Lines.Any(line => line.Quantity is < 1 or > 99 || line.Currency != "EUR"
            || line.NetAmount < 0 || line.VatAmount < 0 || line.TotalAmount < 0 || line.VatRate < 0 || line.VatRate > 100
            || (line.VatExempt && line.VatRate != 0) || decimal.Round(line.NetAmount!.Value / line.Quantity, 2) * line.Quantity != line.NetAmount))
            throw new ArgumentException("De opgeslagen factuurregels zijn niet consistent.");
        var statement = BillingSettings.Text(command.TaxStatement, 1000);
        if (!command.TaxReviewed || (order.Lines.Any(line => line.VatRate == 0 || line.VatExempt) && statement.Length == 0))
            throw new ArgumentException("Controleer de btw-behandeling. Geef bij 0% of vrijgesteld de toepasselijke btw-vermelding op.");
        var entries = order.Lines.Select(line => new Entry(line.ProductName + " / " + line.VariantName, line.Quantity,
            line.NetAmount!.Value / line.Quantity, line.NetAmount.Value, line.VatAmount!.Value, line.TotalAmount, line.VatRate!.Value, line.VatExempt)).ToList();
        if (order.DeliveryMethod is { Amount: > 0 } delivery)
        {
            if (delivery.Currency != "EUR") throw new ArgumentException("Bezorgkosten moeten in EUR zijn.");
            var groups = entries.GroupBy(line => (line.Rate, line.Exempt)).Select(group => new { group.Key, Weight = group.Sum(line => line.Net) * (1 + group.Key.Rate / 100m) })
                .OrderBy(group => group.Key.Rate).ThenBy(group => group.Key.Exempt).ToArray();
            var weight = groups.Sum(group => group.Weight);
            if (weight == 0) throw new ArgumentException("De btw op bezorgkosten kan bij deze bestelling niet worden verdeeld.");
            decimal remaining = delivery.Amount;
            for (var index = 0; index < groups.Length; index++)
            {
                var group = groups[index];
                var gross = index == groups.Length - 1 ? remaining : decimal.Round(delivery.Amount * (group.Weight / weight), 2, MidpointRounding.AwayFromZero);
                gross = Math.Min(remaining, gross); remaining -= gross;
                var split = VatPrice.FromGross(Money.Create(gross, "EUR"), group.Key.Rate, group.Key.Exempt);
                entries.Add(new("Bezorging: " + delivery.Name, 1, split.Net.Amount, split.Net.Amount, split.Vat.Amount, gross, group.Key.Rate, group.Key.Exempt));
            }
        }
        if (entries.Sum(line => line.Gross) != order.Totals[0].Amount || entries.Any(line => line.Net + line.Vat != line.Gross))
            throw new ArgumentException("De factuurbedragen sluiten niet aan op de opgeslagen bestelling.");
        var totals = entries.GroupBy(line => (line.Rate, line.Exempt)).OrderBy(group => group.Key.Rate).ThenBy(group => group.Key.Exempt)
            .Select(group => new InvoiceTotal(group.Key.Rate, group.Key.Exempt, F(group.Sum(line => line.Net)), F(group.Sum(line => line.Vat)), F(group.Sum(line => line.Gross)))).ToArray();
        var document = new InvoiceDocument(Guid.NewGuid(), order.Id, "", order.Number, DateTimeOffset.UtcNow, command.SupplyDate, seller, buyer,
            entries.Select(line => new InvoiceLine(line.Description, line.Quantity, F(line.UnitNet), F(line.Net), F(line.Vat), F(line.Gross), line.Rate, line.Exempt)).ToArray(), totals,
            F(entries.Sum(line => line.Net)), F(entries.Sum(line => line.Vat)), F(entries.Sum(line => line.Gross)), "EUR", statement);
        return await invoices.AddAsync(document, order.Revision, token);
    }
    private static string F(decimal value) => value.ToString("0.00", CultureInfo.InvariantCulture);
    private sealed record Entry(string Description, int Quantity, decimal UnitNet, decimal Net, decimal Vat, decimal Gross, decimal Rate, bool Exempt);
}
