using System.Net.Mail;
using MyShop.Domain.Catalog;

namespace MyShop.Domain.Checkout;

public enum OrderPaymentMethod { PayLater = 1 }
public enum OrderStatus { AwaitingPayment = 1 }

public sealed record OrderCustomer
{
    private OrderCustomer(string name, string email) { Name = name; Email = email; }
    public string Name { get; }
    public string Email { get; }

    public static OrderCustomer Create(string name, string email)
    {
        name = Required(name, 200, nameof(name));
        email = Required(email, 320, nameof(email));
        try
        {
            var parsed = new MailAddress(email);
            if (!string.Equals(parsed.Address, email, StringComparison.Ordinal)) throw new FormatException();
        }
        catch (FormatException) { throw new ArgumentException("Email address is invalid.", nameof(email)); }
        return new(name, email);
    }

    private static string Required(string value, int maximum, string parameter)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value, parameter);
        value = value.Trim();
        if (value.Length > maximum) throw new ArgumentException($"Value must contain at most {maximum} characters.", parameter);
        return value;
    }
}

public sealed record DeliveryAddress
{
    private DeliveryAddress(string addressLine, string postalCode, string city, string countryCode)
    { AddressLine = addressLine; PostalCode = postalCode; City = city; CountryCode = countryCode; }
    public string AddressLine { get; }
    public string PostalCode { get; }
    public string City { get; }
    public string CountryCode { get; }

    public static DeliveryAddress Create(string addressLine, string postalCode, string city, string countryCode)
    {
        addressLine = Required(addressLine, 200, nameof(addressLine));
        postalCode = Required(postalCode, 32, nameof(postalCode));
        city = Required(city, 100, nameof(city));
        countryCode = Required(countryCode, 2, nameof(countryCode)).ToUpperInvariant();
        if (countryCode.Length != 2 || countryCode.Any(character => character is < 'A' or > 'Z'))
            throw new ArgumentException("Country code must contain two letters.", nameof(countryCode));
        return new(addressLine, postalCode, city, countryCode);
    }

    private static string Required(string value, int maximum, string parameter)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value, parameter);
        value = value.Trim();
        if (value.Length > maximum) throw new ArgumentException($"Value must contain at most {maximum} characters.", parameter);
        return value;
    }
}

public sealed record OrderLine(Guid ProductId, Guid VariantId, string ProductName, string VariantName,
    int Quantity, Money UnitPrice, Money Total);
public sealed record OrderTotal(string Currency, decimal Amount);

public sealed class Order
{
    private Order(Guid id, DateTimeOffset placedAt, OrderCustomer customer, DeliveryAddress address,
        IReadOnlyList<OrderLine> lines, IReadOnlyList<OrderTotal> totals)
    {
        Id = id; PlacedAt = placedAt; Customer = customer; DeliveryAddress = address;
        Lines = lines; Totals = totals;
    }

    public Guid Id { get; }
    public string Number => $"MS-{Id:N}".ToUpperInvariant();
    public DateTimeOffset PlacedAt { get; }
    public OrderCustomer Customer { get; }
    public DeliveryAddress DeliveryAddress { get; }
    public OrderPaymentMethod PaymentMethod => OrderPaymentMethod.PayLater;
    public OrderStatus Status => OrderStatus.AwaitingPayment;
    public IReadOnlyList<OrderLine> Lines { get; }
    public IReadOnlyList<OrderTotal> Totals { get; }

    public static Order Place(Guid id, DateTimeOffset placedAt, OrderCustomer customer,
        DeliveryAddress address, IEnumerable<(Guid ProductId, Guid VariantId, string ProductName,
            string VariantName, int Quantity, Money UnitPrice)> lines)
    {
        if (id == Guid.Empty) throw new ArgumentException("Order ID is required.", nameof(id));
        ArgumentNullException.ThrowIfNull(customer);
        ArgumentNullException.ThrowIfNull(address);
        ArgumentNullException.ThrowIfNull(lines);
        var snapshots = lines.Select(line =>
        {
            if (line.ProductId == Guid.Empty || line.VariantId == Guid.Empty || line.Quantity is < 1 or > 99)
                throw new ArgumentException("Order line is invalid.", nameof(lines));
            var productName = Required(line.ProductName, nameof(lines));
            var variantName = Required(line.VariantName, nameof(lines));
            return new OrderLine(line.ProductId, line.VariantId, productName, variantName, line.Quantity,
                line.UnitPrice, Money.Create(line.UnitPrice.Amount * line.Quantity, line.UnitPrice.Currency));
        }).ToArray();
        if (snapshots.Length is < 1 or > 20 || snapshots.Select(line => (line.ProductId, line.VariantId)).Distinct().Count() != snapshots.Length)
            throw new ArgumentException("Order must contain 1 to 20 unique variants.", nameof(lines));
        var totals = snapshots.GroupBy(line => line.Total.Currency)
            .Select(group => new OrderTotal(group.Key, group.Sum(line => line.Total.Amount)))
            .OrderBy(total => total.Currency, StringComparer.Ordinal).ToArray();
        return new(id, placedAt.ToUniversalTime(), customer, address, snapshots, totals);
    }

    private static string Required(string value, int maximum, string parameter)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value, parameter);
        value = value.Trim();
        if (value.Length > maximum) throw new ArgumentException($"Value must contain at most {maximum} characters.", parameter);
        return value;
    }

    private static string Required(string value, string parameter)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value, parameter);
        return value.Trim();
    }
}
