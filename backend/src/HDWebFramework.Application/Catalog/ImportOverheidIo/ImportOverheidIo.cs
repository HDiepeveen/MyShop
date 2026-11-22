using HDWebFramework.Application.Catalog.Abstractions;
using HDWebFramework.Application.Catalog.Rdw;
using HDWebFramework.Application.Catalog.OverheidIo;
using HDWebFramework.Domain.Catalog;

namespace HDWebFramework.Application.Catalog.ImportOverheidIo;

public sealed class ImportOverheidIo(IProductRepository products, IProductTypeRepository types, IOverheidIoClient client)
{
    public async Task<bool> ExecuteAsync(Guid productId, Guid attributeId, string licensePlate, CancellationToken ct)
    {
        var normalized = OverheidIoAttributeValue.NormalizePlate(licensePlate);
        var snapshot = await products.GetByIdAsync(ProductId.From(productId), ct)
            ?? throw new ArgumentException("Product bestaat niet meer.");
        var type = await types.GetByIdAsync(snapshot.Product.ProductTypeId, ct);
        var definition = type?.AttributeDefinitions.SingleOrDefault(a => a.Id.Value == attributeId);
        if (definition is null || definition.DataType != AttributeDataType.OverheidIo || definition.Scope != AttributeScope.Product)
            throw new ArgumentException("Kies een Overheid.io-kenmerk op productniveau.");
        var json = await client.FindAsync(licensePlate.Trim().Replace(" ", "").ToUpperInvariant(), ct);
        if (json is null) return false;
        var value = OverheidIoAttributeValue.Create(AttributeDefinitionId.From(attributeId), json);
        if (value.LicensePlate != normalized) throw new InvalidOperationException("Overheid.io antwoordt met een ander kenteken.");
        if (snapshot.Product.SetAttributeValue(value))
            await products.SaveAsync(snapshot.Product, snapshot.ConcurrencyToken, ct);
        return true;
    }
}
