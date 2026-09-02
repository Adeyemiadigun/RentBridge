using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace RentBridge.Infrastructure.Persistence.Configurations
{
    public class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
    {
        public void Configure(EntityTypeBuilder<RefreshToken> b)
        {
            b.ToTable("refresh_tokens");
            b.HasKey(t => t.Id);

            b.HasIndex(t => t.TokenHash).IsUnique();
            b.HasIndex(t => t.UserId);

            b.Property(t => t.TokenHash).HasMaxLength(64).IsRequired();
            b.Property(t => t.ReplacedByTokenHash).HasMaxLength(64);
            b.Property(t => t.CreatedByIp).HasMaxLength(45);
            b.Property(t => t.UserAgent).HasMaxLength(512);
        }
    }
}
