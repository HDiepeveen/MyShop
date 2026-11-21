namespace HDWebFramework.Infrastructure.Persistence.Models;
internal sealed class OverheidIoSettingsPersistence
{
    public int Id { get; set; }
    public Guid Revision { get; set; }
    public string ColumnsJson { get; set; } = "[]";
    public string? ProtectedApiKey { get; set; }
}
