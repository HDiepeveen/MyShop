namespace MyShop.Application.Checkout.Abstractions;

public enum OnlinePaymentStatus { Open, Pending, Paid, Failed, Canceled, Expired, Authorized }
public sealed record OnlinePaymentVerification(OnlinePaymentStatus Status, bool MatchesPayment);

public interface IOnlinePaymentStatusReader
{
    Task<OnlinePaymentVerification> VerifyAsync(OnlinePaymentStartRecord payment, CancellationToken cancellationToken);
}

public sealed class OnlinePaymentProviderException : Exception
{
    public OnlinePaymentProviderException() : base("De betaalprovider is tijdelijk niet bereikbaar. Probeer opnieuw.") { }
}
