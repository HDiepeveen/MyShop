using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MyShop.Infrastructure.Persistence.Models;

namespace MyShop.Infrastructure.Persistence.Configurations;

internal sealed class EmailMessagePersistenceConfiguration : IEntityTypeConfiguration<EmailMessagePersistence>
{
    public void Configure(EntityTypeBuilder<EmailMessagePersistence> builder)
    {
        builder.ToTable("EmailMessages");
        builder.HasKey(message => message.Id);
        builder.Property(message => message.Recipient).HasMaxLength(320).IsRequired();
        builder.Property(message => message.Subject).HasMaxLength(200).IsRequired();
        builder.Property(message => message.Body).IsRequired();
        builder.HasIndex(message => new { message.SentAt, message.NextAttemptAt });
    }
}
