using HDWebFramework.Application.Catalog.Rdw;
using HDWebFramework.Infrastructure.Persistence.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System.Text.Json;

namespace HDWebFramework.Infrastructure.Persistence.Configurations;

internal sealed class RdwSettingsConfiguration : IEntityTypeConfiguration<RdwSettingsPersistence>
{
    public void Configure(EntityTypeBuilder<RdwSettingsPersistence> builder)
    {
        builder.ToTable("RdwSettings");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();
        builder.Property(x => x.ColumnsJson).IsRequired();
        builder.HasData(new RdwSettingsPersistence {
            Id = 1, Revision = new Guid("790213d2-9aa5-4f0b-8de7-906eb8eb95b2"),
            ColumnsJson = JsonSerializer.Serialize(RdwColumns.Default)
        });
    }
}
