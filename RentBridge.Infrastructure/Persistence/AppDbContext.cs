using MediatR;
using Microsoft.EntityFrameworkCore;
using RentBridge.Domain.Aggregates;
using RentBridge.Domain.Common;
using RentBridge.Domain.Entities;
using RentBridge.Infrastructure.Persistence.Repositories;

namespace RentBridge.Infrastructure.Persistence;

public class AppDbContext : DbContext
{
    private readonly IMediator _mediator;

    public AppDbContext(DbContextOptions<AppDbContext> options, IMediator mediator)
        : base(options) => _mediator = mediator;

    public DbSet<User> Users => Set<User>();
    public DbSet<KycVerification> KycVerifications => Set<KycVerification>();
    public DbSet<Property> Properties => Set<Property>();
    public DbSet<Listing> Listings => Set<Listing>();
    public DbSet<Lease> Leases => Set<Lease>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<LedgerEntry> LedgerEntries => Set<LedgerEntry>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);

        // Keyless result shape for the raw-SQL ledger time-series query.
        modelBuilder.Entity<TransactionMetricsRow>().HasNoKey();
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var domainEvents = ChangeTracker.Entries<Entity<Guid>>()
            .SelectMany(e => e.Entity.DomainEvents)
            .ToList();

        var result = await base.SaveChangesAsync(cancellationToken);

        foreach (var domainEvent in domainEvents)
        {
            await _mediator.Publish(domainEvent, cancellationToken);
        }

        return result;
    }
}
