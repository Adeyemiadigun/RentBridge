using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RentBridge.Domain.Aggregates.Users;
using RentBridge.Domain.Enums;

namespace RentBridge.Infrastructure.Persistence.Configurations;

public class ListingConfiguration : IEntityTypeConfiguration<Listing>
{
    public void Configure(EntityTypeBuilder<Listing> b)
    {
        b.ToTable("listings");
        b.HasKey(l => l.Id);

        b.HasIndex(l => l.OwnerUserId);
        b.HasIndex(l => l.Status);

        b.Property(l => l.Status)
         .HasConversion<string>()
         .HasMaxLength(30);

        b.OwnsOne(l => l.Price, p =>
        {
            p.Property(x => x.Amount).HasColumnName("price_amount");
            p.Property(x => x.Currency).HasColumnName("price_currency");
        });
    }
}
