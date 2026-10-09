using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using UrlShortener.Domain.Entities;
using UrlShortener.Domain;

namespace UrlShortener.Infrastructure.Persistence.Configurations;

internal sealed class ShortUrlConfiguration : IEntityTypeConfiguration<ShortUrl>
{
    public void Configure(EntityTypeBuilder<ShortUrl> builder)
    {
        builder.ToTable("ShortUrls");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Code)
            .IsRequired()
            .HasMaxLength(ShortCodeRules.MaxLength);

        // The unique index is the authoritative guard against duplicate codes —
        // it closes the race window left by the generate-check-insert sequence.
        builder.HasIndex(x => x.Code).IsUnique();

        builder.Property(x => x.LongUrl)
            .IsRequired()
            .HasMaxLength(4096);

        builder.Property(x => x.CreatedAt).IsRequired();
    }
}
