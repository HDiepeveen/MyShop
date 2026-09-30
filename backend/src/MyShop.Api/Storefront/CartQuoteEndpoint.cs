using System.Globalization;
using Microsoft.AspNetCore.Mvc;
using MyShop.Application.Catalog.QuoteStorefrontCart;

namespace MyShop.Api.Storefront;

public static class CartQuoteEndpoint
{
    public static void MapCartQuote(this IEndpointRouteBuilder endpoints) =>
        endpoints.MapGet("/api/shop/cart/quote", ExecuteAsync).AllowAnonymous();

    public static async Task<IResult> ExecuteAsync([FromQuery] string[] lines,
        [FromServices] QuoteStorefrontCart useCase, CancellationToken cancellationToken)
    {
        if (lines.Length > 20) return Results.BadRequest();
        var parsed = new List<CartQuoteLine>();
        foreach (var line in lines)
        {
            if (line.Length > 80) return Results.BadRequest();
            var parts = line.Split(':');
            if (parts.Length != 3 || !Guid.TryParseExact(parts[0], "D", out var productId)
                || !Guid.TryParseExact(parts[1], "D", out var variantId)
                || !int.TryParse(parts[2], NumberStyles.None, CultureInfo.InvariantCulture, out var quantity))
                return Results.BadRequest();
            parsed.Add(new(productId, variantId, quantity));
        }
        try
        {
            var quote = await useCase.ExecuteAsync(new(parsed, DateTimeOffset.UtcNow), cancellationToken);
            // Decimal strings preserve every cent in clients, including values beyond JavaScript's safe integer range.
            return Results.Ok(new CartQuoteResponse(quote.At,
                quote.Lines.Select(line => new CartQuoteLineResponse(line.ProductId, line.VariantId, line.Quantity,
                    line.Name, line.Variant, Format(line.Amount), line.Currency, Format(line.Total), line.Failure)).ToArray(),
                quote.Totals.Select(total => new CartQuoteTotalResponse(total.Currency, Format(total.Amount)!)).ToArray()));
        }
        catch (ArgumentException) { return Results.BadRequest(); }
    }
    private static string? Format(decimal? amount) => amount?.ToString("F2", CultureInfo.InvariantCulture);
}

public sealed record CartQuoteResponse(DateTimeOffset At, IReadOnlyList<CartQuoteLineResponse> Lines, IReadOnlyList<CartQuoteTotalResponse> Totals);
public sealed record CartQuoteLineResponse(Guid ProductId, Guid VariantId, int Quantity, string? Name, string? Variant,
    string? Amount, string? Currency, string? Total, string? Failure);
public sealed record CartQuoteTotalResponse(string Currency, string Amount);
