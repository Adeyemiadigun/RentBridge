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

        // PayoutAccount — owned 1:1 child; escrow payout destination
        b.OwnsOne(u => u.PayoutAccount, pa =>
        {
            pa.Property(x => x.Provider).HasColumnName("payout_provider").HasMaxLength(40);
            pa.Property(x => x.RecipientCode).HasColumnName("payout_recipient_code").HasMaxLength(120);
            pa.Property(x => x.BankCode).HasColumnName("payout_bank_code").HasMaxLength(20);
            pa.Property(x => x.BankName).HasColumnName("payout_bank_name").HasMaxLength(150);
            pa.Property(x => x.AccountNumberLast4).HasColumnName("payout_account_last4").HasMaxLength(4);
            pa.Property(x => x.AccountName).HasColumnName("payout_account_name").HasMaxLength(200);
            pa.Property(x => x.IsActive).HasColumnName("payout_account_active");
            pa.Property(x => x.VerifiedAt).HasColumnName("payout_account_verified_at");
        });
        b.Navigation(u => u.PayoutAccount).IsRequired(false);
    }
}
