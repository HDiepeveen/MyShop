using HDWebFramework.Application.Catalog.OverheidIo;
using HDWebFramework.Infrastructure.Persistence.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System.Text.Json;
namespace HDWebFramework.Infrastructure.Persistence.Configurations;
internal sealed class OverheidIoSettingsConfiguration : IEntityTypeConfiguration<OverheidIoSettingsPersistence>
{
    public void Configure(EntityTypeBuilder<OverheidIoSettingsPersistence> builder)
    {
        builder.ToTable("OverheidIoSettings");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();
        builder.Property(x => x.ColumnsJson).IsRequired();
        builder.HasData(new OverheidIoSettingsPersistence { Id = 1,
            Revision = new Guid("e74b8e60-9f7e-4e08-9a88-70b6777ac215"),
            ColumnsJson = JsonSerializer.Serialize(OverheidIoColumns.Default) });
    }
}
