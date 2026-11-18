namespace HDWebFramework.Infrastructure.Persistence.Models;

internal sealed class RdwSettingsPersistence
{
    public int Id { get; set; }
    public Guid Revision { get; set; }
    public string ColumnsJson { get; set; } = "[]";
}
