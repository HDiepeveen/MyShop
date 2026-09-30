using MyShop.Application.Catalog.Abstractions;
using MyShop.Domain.Catalog;

namespace MyShop.Application.Catalog.QuoteStorefrontCart;

public sealed record CartQuoteLine(Guid ProductId, Guid VariantId, int Quantity);
public sealed record QuoteStorefrontCartQuery(IReadOnlyList<CartQuoteLine> Lines, DateTimeOffset At);
public sealed record CartQuotedLine(Guid ProductId, Guid VariantId, int Quantity, string? Name, string? Variant,
    decimal? Amount, string? Currency, decimal? Total, string? Failure);
public sealed record CartQuotedTotal(string Currency, decimal Amount);
public sealed record StorefrontCartQuote(DateTimeOffset At, IReadOnlyList<CartQuotedLine> Lines, IReadOnlyList<CartQuotedTotal> Totals);

public sealed class QuoteStorefrontCart(IProductRepository products)
{
    private readonly IProductRepository products = products ?? throw new ArgumentNullException(nameof(products));

    public async Task<StorefrontCartQuote> ExecuteAsync(QuoteStorefrontCartQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);
        ArgumentNullException.ThrowIfNull(query.Lines);
        cancellationToken.ThrowIfCancellationRequested();
        // Bound the whole request before any persistence access.
        var lines = query.Lines.ToArray();
        if (lines.Length > 20 || lines.Any(line => line is null || line.ProductId == Guid.Empty || line.VariantId == Guid.Empty || line.Quantity is < 1 or > 99)
            || lines.Select(line => (line.ProductId, line.VariantId)).Distinct().Count() != lines.Length)
            throw new ArgumentException("Cart must contain at most 20 unique variants with quantities from 1 to 99.", nameof(query));

        var snapshots = new Dictionary<Guid, ProductSnapshot?>();
        foreach (var id in lines.Select(line => line.ProductId).Distinct())
            snapshots[id] = await products.GetByIdAsync(ProductId.From(id), cancellationToken);

        var quoted = lines.Select(line =>
        {
            var product = snapshots[line.ProductId]?.Product;
            var variant = product?.Presentation.IsPublished == true
                ? product.Variants.SingleOrDefault(variant => variant.Id.Value == line.VariantId) : null;
            if (variant is null)
                return new CartQuotedLine(line.ProductId, line.VariantId, line.Quantity, null, null, null, null, null, "unavailable");
            if (variant.Price is null)
                return new CartQuotedLine(line.ProductId, line.VariantId, line.Quantity, product!.Name, variant.Name, null, null, null, "priceMissing");
            var price = variant.CalculatePrice(query.At);
            return new CartQuotedLine(line.ProductId, line.VariantId, line.Quantity, product!.Name, variant.Name,
                price.Amount, price.Currency, price.Amount * line.Quantity, null);
        }).ToArray();
        var totals = quoted.Any(line => line.Failure is not null) ? [] : quoted.GroupBy(line => line.Currency!)
            .Select(group => new CartQuotedTotal(group.Key, group.Sum(line => line.Total!.Value))).ToArray();
        return new(query.At, quoted, totals);
    }
}
