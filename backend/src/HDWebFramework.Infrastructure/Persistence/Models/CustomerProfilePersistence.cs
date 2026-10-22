namespace HDWebFramework.Infrastructure.Persistence.Models;

internal sealed class CustomerProfilePersistence
{
    public Guid Id { get; set; }
    public string UserId { get; set; } = "";
    public string Name { get; set; } = "";
    public string AddressLine { get; set; } = "";
    public string PostalCode { get; set; } = "";
    public string City { get; set; } = "";
    public string PhoneNumber { get; set; } = "";
    public string Notes { get; set; } = "";
    public string CountryCode { get; set; } = "";
    public Guid Version { get; set; }
}
