using MyShop.Application.Catalog.Abstractions;
using MyShop.Domain.Catalog;

namespace MyShop.Application.Catalog.GenerateProductVariants;

public sealed record VariantCombinationValue(AttributeDefinitionId AttributeDefinitionId, CatalogAttributeValueInput Value);
public sealed record VariantCombination(string Name, IReadOnlyList<VariantCombinationValue> Values);
public sealed record GenerateProductVariantsCommand(
    ProductId ProductId, Guid Revision, IReadOnlyList<VariantCombination> Combinations,
    decimal? NetAmount = null, decimal? VatRate = null, bool VatExempt = false, int? StockQuantity = null);
public enum GenerateProductVariantsFailure { ProductNotFound, ProductTypeNotFound }
public sealed record GenerateProductVariantsResult(GenerateProductVariantsFailure? Failure, int Added = 0, int Skipped = 0);

public sealed class GenerateProductVariants
{
    private readonly IProductRepository _products;
    private readonly IProductTypeRepository _types;
    private readonly IVatRateAvailability _rates;

    public GenerateProductVariants(IProductRepository products, IProductTypeRepository types, IVatRateAvailability rates)
    {
        _products = products ?? throw new ArgumentNullException(nameof(products));
        _types = types ?? throw new ArgumentNullException(nameof(types));
        _rates = rates ?? throw new ArgumentNullException(nameof(rates));
    }

    public async Task<GenerateProductVariantsResult> ExecuteAsync(
        GenerateProductVariantsCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        cancellationToken.ThrowIfCancellationRequested();
        if (command.ProductId == default || command.Revision == Guid.Empty)
            throw new ArgumentException("Product en revisie zijn verplicht.");
        if (command.Combinations is null || command.Combinations.Count is < 1 or > 100)
            throw new ArgumentException("Kies tussen 1 en 100 combinaties per keer.");
        if (command.StockQuantity is < 0)
            throw new ArgumentException("De beginvoorraad moet een heel aantal vanaf 0 zijn.");
        if (command.NetAmount is null && (command.VatRate is not null || command.VatExempt))
            throw new ArgumentException("Een btw-keuze vereist een beginprijs.");
        Money? price = null;
        if (command.NetAmount is { } net)
        {
            if (decimal.Round(net, 2) != net)
                throw new ArgumentException("Gebruik een beginprijs met maximaal twee decimalen.");
            if (command.VatRate is not { } rate)
                throw new ArgumentException("Kies btw bij de beginprijs.");
            price = VatPrice.FromNet(Money.Create(net, "EUR"), rate, command.VatExempt).Gross;
            if (!await _rates.IsAvailableAsync(rate, command.VatExempt, cancellationToken))
                throw new ArgumentException("Dit btw-percentage is niet beschikbaar. Stel het eerst in bij facturatie.");
        }
        var snapshot = await _products.GetByIdAsync(command.ProductId, cancellationToken);
        if (snapshot is null) return new(GenerateProductVariantsFailure.ProductNotFound);
        if (snapshot.ConcurrencyToken.Revision != command.Revision)
            throw new ProductConcurrencyException(command.ProductId);
        var product = snapshot.Product;
        var type = await _types.GetByIdAsync(product.ProductTypeId, cancellationToken);
        if (type is null) return new(GenerateProductVariantsFailure.ProductTypeNotFound);
        var definitions = type.AttributeDefinitions.Where(d => d.Scope == AttributeScope.Variant).ToArray();
        var planned = new List<(string Name, AttributeValue[] Values)>();
        foreach (var combination in command.Combinations)
        {
            if (combination is null || string.IsNullOrWhiteSpace(combination.Name)
                || combination.Name.Trim().Length > 200 || combination.Name.Any(char.IsControl))
                throw new ArgumentException("Gebruik een variantnaam van maximaal 200 tekens.");
            if (combination.Values is null || combination.Values.Count is < 1 or > 10
                || combination.Values.Any(v => v is null || v.Value is null)
                || combination.Values.Select(v => v.AttributeDefinitionId).Distinct().Count() != combination.Values.Count)
                throw new ArgumentException("Gebruik 1 tot 10 verschillende variantkenmerken.");
            var values = new List<AttributeValue>();
            foreach (var item in combination.Values)
            {
                var definition = definitions.SingleOrDefault(d => d.Id == item.AttributeDefinitionId);
                if (definition is null || definition.DataType != item.Value.DataType
                    || definition.DataType == AttributeDataType.MultiChoice)
                    throw new ArgumentException("Een kenmerk is gewijzigd of hoort niet bij deze varianten. Vernieuw het product.");
                var input = item.Value switch
                {
                    TextAttributeValueInput text => new TextAttributeValueInput(ValidateOption(text.Value)),
                    ChoiceAttributeValueInput choice => new ChoiceAttributeValueInput(ValidateOption(choice.Value)),
                    _ => item.Value
                };
                values.Add(CatalogAttributeValueFactory.Create(item.AttributeDefinitionId, input));
            }
            // Use every scalar dimension, including optional ones, to avoid confusing partial matches.
            if (definitions.Where(d => d.DataType != AttributeDataType.MultiChoice)
                .Any(d => !values.Any(v => v.AttributeDefinitionId == d.Id)))
                throw new ArgumentException("Vul voor elk variantkenmerk een optie in.");
            var array = values.ToArray();
            if (planned.Any(p => Matches(p.Values, array)))
                throw new ArgumentException("Dezelfde combinatie is meer dan één keer gekozen.");
            planned.Add((combination.Name.Trim(), array));
        }
        // All validation finishes before the aggregate is changed.
        var skipped = 0;
        foreach (var plan in planned)
        {
            if (product.Variants.Any(v => Matches(v.AttributeValues, plan.Values)))
            {
                skipped++;
                continue;
            }
            var variant = product.AddVariant(plan.Name);
            foreach (var value in plan.Values) product.SetVariantAttributeValue(variant.Id, value);
            if (price is not null)
                product.SetVariantPrice(variant.Id, price.Value, command.VatRate, command.VatExempt, command.NetAmount);
            if (command.StockQuantity is { } stock) product.SetVariantStockQuantity(variant.Id, stock);
        }
        var added = planned.Count - skipped;
        if (added > 0) await _products.SaveAsync(product, snapshot.ConcurrencyToken, cancellationToken);
        return new(null, added, skipped);
    }

    private static bool Matches(IEnumerable<AttributeValue> existing, IEnumerable<AttributeValue> requested) =>
        requested.All(value => existing.Any(candidate => candidate == value));

    private static string ValidateOption(string value)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Trim().Length > 100 || value.Any(char.IsControl))
            throw new ArgumentException("Gebruik opties van 1 tot 100 tekens, zonder regeleinden.");
        return value.Trim();
    }
}
