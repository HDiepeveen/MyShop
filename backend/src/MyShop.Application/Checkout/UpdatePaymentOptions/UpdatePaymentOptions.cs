using MyShop.Application.Checkout.Abstractions;

namespace MyShop.Application.Checkout.UpdatePaymentOptions;

public sealed record UpdatePaymentOptionsCommand(bool PayLaterEnabled, bool OnlinePaymentEnabled,
    string? PayLaterInstructions, Guid Revision);
public enum UpdatePaymentOptionsFailure { OnlinePaymentNotConfigured, ConcurrencyConflict }
public sealed record UpdatePaymentOptionsResult(PaymentOptionsSnapshot? Settings, UpdatePaymentOptionsFailure? Failure)
{
    public static UpdatePaymentOptionsResult Failed(UpdatePaymentOptionsFailure failure) => new(null, failure);
}

public sealed class UpdatePaymentOptions(IPaymentOptionsRepository repository, IOnlinePaymentAvailability online)
{
    private readonly IPaymentOptionsRepository repository = repository ?? throw new ArgumentNullException(nameof(repository));
    private readonly IOnlinePaymentAvailability online = online ?? throw new ArgumentNullException(nameof(online));

    public async Task<UpdatePaymentOptionsResult> ExecuteAsync(UpdatePaymentOptionsCommand command,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        cancellationToken.ThrowIfCancellationRequested();
        if (command.Revision == Guid.Empty) throw new ArgumentException("Revision is required.", nameof(command));
        if (!command.PayLaterEnabled && !command.OnlinePaymentEnabled)
            throw new ArgumentException("At least one payment option must be enabled.", nameof(command));
        if (command.OnlinePaymentEnabled && !online.IsConfigured)
            return UpdatePaymentOptionsResult.Failed(UpdatePaymentOptionsFailure.OnlinePaymentNotConfigured);
        var payLaterInstructions = string.IsNullOrWhiteSpace(command.PayLaterInstructions)
            ? null
            : command.PayLaterInstructions.Trim();
        if (payLaterInstructions?.Length > 2000)
            throw new ArgumentException("Pay-later instructions must contain at most 2000 characters.", nameof(command));
        var saved = await repository.SaveAsync(command.PayLaterEnabled, command.OnlinePaymentEnabled,
            payLaterInstructions, command.Revision, cancellationToken);
        return saved is null
            ? UpdatePaymentOptionsResult.Failed(UpdatePaymentOptionsFailure.ConcurrencyConflict)
            : new(saved, null);
    }
}
