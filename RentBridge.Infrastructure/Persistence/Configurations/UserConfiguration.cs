using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RentBridge.Domain.Aggregates.Users;

namespace RentBridge.Infrastructure.Persistence.Configurations;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> b)
    {
        b.ToTable("users");
        b.HasKey(u => u.Id);

        b.HasIndex(u => u.Email).IsUnique();
        b.HasIndex(u => u.Phone).IsUnique();

        b.OwnsOne(u => u.Email, e =>
            e.Property(x => x.Value).HasColumnName("email"));

        b.OwnsOne(u => u.Phone, p =>
            p.Property(x => x.Value).HasColumnName("phone"));

        // LawyerProfile — owned 1:1 child; columns on the users table
        b.OwnsOne(u => u.LawyerProfile, lp =>
        {
            lp.Property(x => x.BarNumber).HasColumnName("bar_number").HasMaxLength(50);
            lp.Property(x => x.Status).HasColumnName("lawyer_status").HasConversion<string>().HasMaxLength(20);
        });
        b.Navigation(u => u.LawyerProfile).IsRequired(false);
    }
}
