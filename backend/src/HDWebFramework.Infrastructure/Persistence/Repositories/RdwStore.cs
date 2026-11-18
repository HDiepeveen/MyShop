using System.Text.Json;
using HDWebFramework.Application.Catalog.Rdw;
using HDWebFramework.Domain.Catalog;
using Microsoft.EntityFrameworkCore;

namespace HDWebFramework.Infrastructure.Persistence.Repositories;

internal sealed class RdwStore(HDWebFrameworkDbContext db) : IRdwStore
{
    public async Task<RdwSettings> SettingsAsync(CancellationToken ct)
    {
        var row = await db.Set<Models.RdwSettingsPersistence>().AsNoTracking().SingleAsync(ct);
        return new(row.Revision, JsonSerializer.Deserialize<string[]>(row.ColumnsJson)!);
    }
    public async Task<bool> SaveSettingsAsync(RdwSettings settings, CancellationToken ct)
    {
        if (settings.Columns is null || settings.Columns.Any(key => !RdwColumns.All.Any(c => c.Key == key)))
            throw new ArgumentException("Kies geldige RDW-kolommen.");
        return await db.Set<Models.RdwSettingsPersistence>()
            .Where(x => x.Id == 1 && x.Revision == settings.Revision)
            .ExecuteUpdateAsync(update => update
                .SetProperty(x => x.ColumnsJson, JsonSerializer.Serialize(settings.Columns.Distinct().ToArray()))
                .SetProperty(x => x.Revision, Guid.NewGuid()), ct) == 1;
    }
    public async Task<IReadOnlyList<RdwVehicle>?> PublicVehiclesAsync(Guid productId, CancellationToken ct)
    {
        if (!await db.Products.AnyAsync(p => p.Id == productId && p.IsPublished, ct)) return null;
        var values = await (from value in db.ProductAttributeValues.AsNoTracking()
            join definition in db.AttributeDefinitions on value.AttributeDefinitionId equals definition.Id
            join product in db.Products on value.ProductId equals product.Id
            where product.IsPublished && value.ProductId == productId && value.DataType == AttributeDataType.Rdw &&
                definition.DataType == AttributeDataType.Rdw && definition.ProductTypeId == product.ProductTypeId
            select new { definition.Id, definition.DisplayName, value.TextValue }).ToListAsync(ct);
        var settings = await SettingsAsync(ct);
        return values.Select(v => {
            var vehicle = RdwAttributeValue.Create(AttributeDefinitionId.From(v.Id), v.TextValue!);
            return new RdwVehicle(v.DisplayName, vehicle.LicensePlate, RdwDisplay.Fields(vehicle.Value, settings.Columns));
        }).ToList();
    }
}
