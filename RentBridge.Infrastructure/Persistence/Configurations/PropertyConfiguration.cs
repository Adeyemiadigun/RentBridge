using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RentBridge.Domain.Aggregates;
using RentBridge.Domain.Enums;

namespace RentBridge.Infrastructure.Persistence.Configurations;

public class PropertyConfiguration : IEntityTypeConfiguration<Property>
{
    public void Configure(EntityTypeBuilder<Property> b)
    {
        b.ToTable("properties");
        b.HasKey(p => p.Id);

        b.Property(p => p.IsVerified)
         .HasColumnName("is_verified")
         .HasConversion<bool>();

        b.Property(p => p.PropertyType)
         .HasColumnName("property_type");

        b.Property(p => p.Bedrooms)
         .HasColumnName("bedrooms");

        b.Property(p => p.Bathrooms)
         .HasColumnName("bathrooms");

        b.Property(p => p.AvailableFrom)
         .HasColumnName("available_from");

        b.Property(p => p.Amenities)
         .HasColumnName("amenities")
         .HasColumnType("jsonb");

        b.HasIndex(p => p.OwnerUserId);

        b.HasIndex(p => p.IsVerified);

        b.OwnsOne(p => p.PropertyAddress, a =>
        {
            a.Property(x => x.Street).HasColumnName("street");
            a.Property(x => x.City).HasColumnName("city");
            a.Property(x => x.Area).HasColumnName("area");
            a.Property(x => x.State).HasColumnName("state");
        });

        b.OwnsMany(p => p.Documents, d =>
        {
            d.ToTable("ownership_documents");
            d.WithOwner().HasForeignKey("PropertyId");
            d.HasKey(d => d.Id);

            d.Property(x => x.Status)
             .HasConversion<string>()
             .HasMaxLength(30);
        });
    }
}
