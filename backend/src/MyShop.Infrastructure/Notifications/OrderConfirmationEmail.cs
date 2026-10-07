using System.Globalization;
using System.Text;
using MyShop.Domain.Checkout;

namespace MyShop.Infrastructure.Notifications;

internal static class OrderConfirmationEmail
{
    internal static string Body(Order order)
    {
        var body = new StringBuilder().AppendLine($"Bedankt voor je bestelling, {order.Customer.Name}.")
            .AppendLine().AppendLine($"Bestelnummer: {order.Number}")
            .AppendLine($"Geplaatst op: {order.PlacedAt.ToUniversalTime():yyyy-MM-dd HH:mm} UTC")
            .AppendLine(order.PaymentMethod == OrderPaymentMethod.Online ? "Betaalmethode: online betalen" : "Betaalmethode: later betalen")
            .AppendLine();
        foreach (var line in order.Lines)
            body.AppendLine($"{line.Quantity} × {line.ProductName} / {line.VariantName}: {line.Total.Currency} {line.Total.Amount.ToString("0.00", CultureInfo.InvariantCulture)}");
        if (order.DeliveryMethod is { } delivery)
            body.AppendLine($"Bezorging: {delivery.Name} — {delivery.Fee.Currency} {delivery.Fee.Amount.ToString("0.00", CultureInfo.InvariantCulture)}");
        body.AppendLine();
        foreach (var total in order.Totals.OrderBy(total => total.Currency))
            body.AppendLine($"Totaal: {total.Currency} {total.Amount.ToString("0.00", CultureInfo.InvariantCulture)}");
        if (!string.IsNullOrWhiteSpace(order.PaymentInstructions))
            body.AppendLine().AppendLine("Betaalinstructies:").AppendLine(order.PaymentInstructions);
        return body.AppendLine().AppendLine("Dit is een bestelbevestiging, geen factuur.").ToString();
    }
}
