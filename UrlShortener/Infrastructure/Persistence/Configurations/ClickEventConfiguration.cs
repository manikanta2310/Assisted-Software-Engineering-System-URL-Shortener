using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using UrlShortener.Domain.Entities;
using UrlShortener.Domain;

namespace UrlShortener.Infrastructure.Persistence.Configurations;

internal sealed class ClickEventConfiguration : IEntityTypeConfiguration<ClickEvent>
{
    public void Configure(EntityTypeBuilder<ClickEvent> builder)
    {
        builder.ToTable("ClickEvents");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Code)
            .IsRequired()
            .HasMaxLength(ShortCodeRules.MaxLength);

        builder.Property(x => x.IpHash).HasMaxLength(64);

        builder.Property(x => x.Referer).HasMaxLength(2048);

        // Supports the "clicks since <date> for <code>" analytics query.
        builder.HasIndex(x => new { x.Code, x.OccurredAt });
    }
}
