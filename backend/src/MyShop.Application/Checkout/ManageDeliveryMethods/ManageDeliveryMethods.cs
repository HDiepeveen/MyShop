using MyShop.Application.Checkout.Abstractions;
using MyShop.Domain.Catalog;

namespace MyShop.Application.Checkout.ManageDeliveryMethods;

public sealed class ListDeliveryMethods(IDeliveryMethodRepository methods)
{
    public Task<IReadOnlyList<DeliveryMethodSnapshot>> ExecuteAsync(bool publicOnly,
        CancellationToken cancellationToken) => methods.ListAsync(publicOnly, cancellationToken);
}

public sealed record SaveDeliveryMethodCommand(Guid? Id, string Name, string? Description,
    decimal Amount, string Currency, bool Enabled, Guid? Revision);
public enum SaveDeliveryMethodFailure { NotFound, ConcurrencyConflict }
public sealed record SaveDeliveryMethodResult(DeliveryMethodSnapshot? Method,
    SaveDeliveryMethodFailure? Failure);

public sealed class SaveDeliveryMethod(IDeliveryMethodRepository methods)
{
    public async Task<SaveDeliveryMethodResult> ExecuteAsync(SaveDeliveryMethodCommand command,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        cancellationToken.ThrowIfCancellationRequested();
        var name = Required(command.Name, 100, nameof(command.Name));
        var description = Optional(command.Description, 500, nameof(command.Description));
        var money = Money.Create(command.Amount, command.Currency);
        if (await methods.NameExistsAsync(name, command.Id, cancellationToken))
            throw new ArgumentException("Er bestaat al een bezorgoptie met deze naam.", nameof(command.Name));
        if (command.Id is null)
            return new(await methods.AddAsync(name, description, money.Amount, money.Currency,
                command.Enabled, cancellationToken), null);
        if (command.Id == Guid.Empty || command.Revision is null || command.Revision == Guid.Empty)
            throw new ArgumentException("A valid ID and revision are required for an update.");
        if (await methods.GetAsync(command.Id.Value, cancellationToken) is null)
            return new(null, SaveDeliveryMethodFailure.NotFound);
        var saved = await methods.UpdateAsync(command.Id.Value, name, description, money.Amount,
            money.Currency, command.Enabled, command.Revision.Value, cancellationToken);
        return saved is null
            ? new(null, SaveDeliveryMethodFailure.ConcurrencyConflict)
            : new(saved, null);
    }

    private static string Required(string value, int maximum, string parameter)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value, parameter);
        value = value.Trim();
        if (value.Length > maximum) throw new ArgumentException($"Value must contain at most {maximum} characters.", parameter);
        return value;
    }

    private static string? Optional(string? value, int maximum, string parameter)
    {
        value = string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        if (value?.Length > maximum) throw new ArgumentException($"Value must contain at most {maximum} characters.", parameter);
        return value;
    }
}

public enum DeleteDeliveryMethodFailure { NotFound, ConcurrencyConflict }
public sealed class DeleteDeliveryMethod(IDeliveryMethodRepository methods)
{
    public async Task<DeleteDeliveryMethodFailure?> ExecuteAsync(Guid id, Guid revision,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (id == Guid.Empty || revision == Guid.Empty) throw new ArgumentException("A valid ID and revision are required.");
        if (await methods.GetAsync(id, cancellationToken) is null)
            return DeleteDeliveryMethodFailure.NotFound;
        return await methods.DeleteAsync(id, revision, cancellationToken)
            ? null
            : DeleteDeliveryMethodFailure.ConcurrencyConflict;
    }
}
