using HDWebFramework.Application.Catalog.Abstractions;
using HDWebFramework.Application.Catalog.Rdw;
using HDWebFramework.Domain.Catalog;

namespace HDWebFramework.Application.Catalog.ImportRdw;

public sealed class ImportRdw(IProductRepository products, IProductTypeRepository types, IRdwClient client)
{
    public async Task<bool> ExecuteAsync(Guid productId, Guid attributeId, string licensePlate, CancellationToken ct)
    {
        var normalized = RdwAttributeValue.NormalizePlate(licensePlate);
        var snapshot = await products.GetByIdAsync(ProductId.From(productId), ct)
            ?? throw new ArgumentException("Product bestaat niet meer.");
        var type = await types.GetByIdAsync(snapshot.Product.ProductTypeId, ct);
        var definition = type?.AttributeDefinitions.SingleOrDefault(a => a.Id.Value == attributeId);
        if (definition is null || definition.DataType != AttributeDataType.Rdw || definition.Scope != AttributeScope.Product)
            throw new ArgumentException("Kies een RDW-kenmerk op productniveau.");
        var json = await client.FindAsync(normalized, ct);
        if (json is null) return false;
        var value = RdwAttributeValue.Create(AttributeDefinitionId.From(attributeId), json);
        if (value.LicensePlate != normalized) throw new InvalidOperationException("RDW antwoordt met een ander kenteken.");
        if (snapshot.Product.SetAttributeValue(value))
            await products.SaveAsync(snapshot.Product, snapshot.ConcurrencyToken, ct);
        return true;
    }
}
