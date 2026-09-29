using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RentBridge.Domain.Aggregates;
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

        b.Property(l => l.ListingType)
         .HasConversion<string>()
         .HasMaxLength(30)
         .HasColumnName("listing_type")
         .HasDefaultValue(ListingType.Rent);

        b.Property(l => l.PaymentPlan)
         .HasConversion<string>()
         .HasMaxLength(30)
         .HasColumnName("payment_plan")
         .HasDefaultValue(PaymentPlan.Outright);

        b.Property(l => l.OtherExpenses)
         .HasColumnName("other_expenses");


        b.OwnsOne(l => l.Price, p =>
        {
            p.Property(x => x.Amount).HasColumnName("price_amount");
            p.Property(x => x.Currency).HasColumnName("price_currency");
        });

        b.OwnsOne(l => l.CautionFee, f =>
        {
            f.Property(x => x.Amount).HasColumnName("caution_fee_amount").HasPrecision(18, 2);
            f.Property(x => x.Currency).HasColumnName("caution_fee_currency").HasMaxLength(8);
        });

        b.OwnsOne(l => l.RealHouseFee, f =>
        {
            f.Property(x => x.Amount).HasColumnName("real_house_fee_amount").HasPrecision(18, 2);
            f.Property(x => x.Currency).HasColumnName("real_house_fee_currency").HasMaxLength(8);
        });

        b.OwnsOne(l => l.AgentFee, f =>
        {
            f.Property(x => x.Amount).HasColumnName("agent_fee_amount").HasPrecision(18, 2);
            f.Property(x => x.Currency).HasColumnName("agent_fee_currency").HasMaxLength(8);
        });

        b.OwnsMany(l => l.Images, img =>
        {
            img.ToTable("listing_images");
            img.WithOwner().HasForeignKey("ListingId");
            img.HasKey(i => i.Id);

            img.Property(i => i.Url).HasColumnName("url");
            img.Property(i => i.Position).HasColumnName("position");

            img.HasIndex(i => i.ListingId);
        });
    }
}
