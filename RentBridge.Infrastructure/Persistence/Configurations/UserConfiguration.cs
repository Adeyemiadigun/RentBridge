using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RentBridge.Domain.Aggregates;

namespace RentBridge.Infrastructure.Persistence.Configurations;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> b)
    {
        b.ToTable("users");
        b.HasKey(u => u.Id);

        b.OwnsOne(u => u.Email, e =>
        {
            e.Property(x => x.Value).HasColumnName("email");
            e.HasIndex(x => x.Value).IsUnique();
        });

        b.OwnsOne(u => u.Phone, p =>
        {
            p.Property(x => x.Value).HasColumnName("phone");
            p.HasIndex(x => x.Value).IsUnique();
        });

        // LawyerProfile — owned 1:1 child; columns on the users table
        b.OwnsOne(u => u.LawyerProfile, lp =>
        {
            lp.Property(x => x.BarNumber).HasColumnName("bar_number").HasMaxLength(50);
            lp.Property(x => x.Status).HasColumnName("lawyer_status").HasConversion<string>().HasMaxLength(20);
        });
        b.Navigation(u => u.LawyerProfile).IsRequired(false);
    }
}
