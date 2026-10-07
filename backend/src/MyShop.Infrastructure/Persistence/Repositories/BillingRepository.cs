using System.Globalization;
using System.Text.Json;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using MyShop.Application.Billing;
using MyShop.Infrastructure.Persistence.Configurations;
using MyShop.Infrastructure.Persistence.Models;
namespace MyShop.Infrastructure.Persistence.Repositories;
internal sealed class BillingRepository(MyShopDbContext context) : IBillingSettingsRepository, IInvoiceRepository, MyShop.Application.Catalog.Abstractions.IVatRateAvailability
{
    public Task<bool> IsAvailableAsync(decimal percentage, bool exempt, CancellationToken cancellationToken) =>
        context.VatRates.AnyAsync(row => row.Enabled && row.Percentage == percentage && row.Exempt == exempt, cancellationToken);
    public async Task<CompanySettings> GetCompanyAsync(CancellationToken token)
    {
        var row = await context.CompanySettings.AsNoTracking().SingleAsync(token);
        return new(row.Name, row.AddressLine, row.PostalCode, row.City, row.VatId, row.KvkNumber, row.InvoicePrefix, row.Version);
    }
    public async Task<CompanySettings?> SaveCompanyAsync(CompanySettings value, CancellationToken token)
    {
        var revision = Guid.NewGuid();
        var changed = await context.CompanySettings.Where(row => row.Id == CompanySettingsPersistenceConfiguration.Id && row.Version == value.Revision)
            .ExecuteUpdateAsync(update => update.SetProperty(row => row.Name, value.Name).SetProperty(row => row.AddressLine, value.AddressLine)
                .SetProperty(row => row.PostalCode, value.PostalCode).SetProperty(row => row.City, value.City).SetProperty(row => row.VatId, value.VatId)
                .SetProperty(row => row.KvkNumber, value.KvkNumber).SetProperty(row => row.InvoicePrefix, value.InvoicePrefix).SetProperty(row => row.Version, revision), token);
        return changed == 0 ? null : value with { Revision = revision };
    }
    public async Task<IReadOnlyList<VatRateDefinition>> ListRatesAsync(bool enabledOnly, CancellationToken token) =>
        await context.VatRates.AsNoTracking().Where(row => !enabledOnly || row.Enabled).OrderBy(row => row.Percentage).ThenBy(row => row.Exempt)
            .Select(row => new VatRateDefinition(row.Id, row.Name, row.Percentage, row.Exempt, row.Enabled, row.Version)).ToArrayAsync(token);
    public async Task<VatRateDefinition?> SaveRateAsync(SaveVatRate value, CancellationToken token)
    {
        var id = value.Id ?? Guid.NewGuid(); var revision = Guid.NewGuid();
        if (await context.VatRates.AnyAsync(row => row.Id != id && row.Percentage == value.Percentage && row.Exempt == value.Exempt, token))
            throw new ArgumentException("Dit percentage en deze btw-behandeling bestaan al.");
        if (value.Id is null)
        {
            context.VatRates.Add(new() { Id = id, Name = value.Name, Percentage = value.Percentage, Exempt = value.Exempt, Enabled = value.Enabled, Version = revision });
            try { await context.SaveChangesAsync(token); }
            catch (DbUpdateException error) when (error.InnerException is SqlException { Number: 2601 or 2627 })
            { throw new ArgumentException("Dit percentage bestaat al."); }
        }
        else
        {
            int changed;
            try { changed = await context.VatRates.Where(row => row.Id == id && row.Version == value.Revision)
                .ExecuteUpdateAsync(update => update.SetProperty(row => row.Name, value.Name).SetProperty(row => row.Percentage, value.Percentage)
                    .SetProperty(row => row.Exempt, value.Exempt).SetProperty(row => row.Enabled, value.Enabled).SetProperty(row => row.Version, revision), token); }
            catch (SqlException error) when (error.Number is 2601 or 2627) { throw new ArgumentException("Dit percentage bestaat al."); }
            if (changed == 0) return null;
        }
        return new(id, value.Name, value.Percentage, value.Exempt, value.Enabled, revision);
    }
    public async Task<InvoiceDocument?> GetAsync(Guid orderId, string? customerUserId, CancellationToken token)
    {
        if (customerUserId is not null && !await context.Orders.AnyAsync(order => order.Id == orderId && order.CustomerUserId == customerUserId, token)) return null;
        var json = await context.Invoices.AsNoTracking().Where(row => row.OrderId == orderId).Select(row => row.Document).SingleOrDefaultAsync(token);
        return json is null ? null : JsonSerializer.Deserialize<InvoiceDocument>(json);
    }
    public async Task<InvoiceDocument?> AddAsync(InvoiceDocument document, Guid orderRevision, CancellationToken token)
    {
        await using var transaction = await context.Database.BeginTransactionAsync(token);
        var locked = await context.Orders.Where(row => row.Id == document.OrderId && row.Version == orderRevision
            && row.Status != 4 && row.Status != 5).ExecuteUpdateAsync(update => update.SetProperty(row => row.Version, row => row.Version), token);
        if (locked == 0) return null;
        var existing = await GetAsync(document.OrderId, null, token);
        if (existing is not null) return existing;
        await using var command = context.Database.GetDbConnection().CreateCommand();
        command.Transaction = transaction.GetDbTransaction();
        command.CommandText = "UPDATE InvoiceCounters SET NextNumber = NextNumber + 1 OUTPUT deleted.NextNumber WHERE Id = @id AND NextNumber < 9223372036854775807";
        var parameter = command.CreateParameter(); parameter.ParameterName = "@id"; parameter.Value = InvoiceCounterPersistenceConfiguration.Id; command.Parameters.Add(parameter);
        var next = (long?)await command.ExecuteScalarAsync(token) ?? throw new InvalidOperationException("Invoice numbering is unavailable.");
        document = document with { Number = document.Seller.InvoicePrefix + document.IssuedAt.Year.ToString(CultureInfo.InvariantCulture) + "-" + next.ToString("D6", CultureInfo.InvariantCulture) };
        context.Invoices.Add(new() { Id = document.Id, OrderId = document.OrderId, Number = document.Number, IssuedAt = document.IssuedAt, Document = JsonSerializer.Serialize(document) });
        await context.SaveChangesAsync(token); await transaction.CommitAsync(token); return document;
    }
}
