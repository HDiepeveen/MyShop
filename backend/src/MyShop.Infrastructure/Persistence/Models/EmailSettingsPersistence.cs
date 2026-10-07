namespace MyShop.Infrastructure.Persistence.Models;

internal sealed class EmailSettingsPersistence
{
    public Guid Id { get; set; }
    public bool Managed { get; set; }
    public bool Enabled { get; set; }
    public string Host { get; set; } = "";
    public int Port { get; set; }
    public string UserName { get; set; } = "";
    public string FromAddress { get; set; } = "";
    public string FromName { get; set; } = "";
    public string PublicBaseUrl { get; set; } = "";
    public string? ProtectedPassword { get; set; }
    public Guid Version { get; set; }
}
