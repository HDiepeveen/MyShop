namespace MyShop.Application.Checkout.Abstractions;

public interface IDeliveryMethodRepository
{
    Task<IReadOnlyList<DeliveryMethodSnapshot>> ListAsync(bool enabledOnly,
        CancellationToken cancellationToken);
    Task<DeliveryMethodSnapshot?> GetAsync(Guid id, CancellationToken cancellationToken);
    Task<bool> NameExistsAsync(string name, Guid? excludingId, CancellationToken cancellationToken);
    Task<DeliveryMethodSnapshot> AddAsync(string name, string? description, decimal amount,
        string currency, bool enabled, CancellationToken cancellationToken);
    Task<DeliveryMethodSnapshot?> UpdateAsync(Guid id, string name, string? description,
        decimal amount, string currency, bool enabled, Guid expectedRevision,
        CancellationToken cancellationToken);
    Task<bool> DeleteAsync(Guid id, Guid expectedRevision, CancellationToken cancellationToken);
}

public sealed record DeliveryMethodSnapshot(Guid Id, string Name, string? Description,
    decimal Amount, string Currency, bool Enabled, Guid Revision);
