namespace MyShop.Application.Catalog.Abstractions;

public interface IVatRateAvailability
{
    Task<bool> IsAvailableAsync(decimal percentage, bool exempt, CancellationToken cancellationToken);
}
