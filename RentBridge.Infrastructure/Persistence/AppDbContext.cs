using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using RentBridge.Domain.Aggregates;
using RentBridge.Domain.Common;
using RentBridge.Domain.Entities;
using RentBridge.Infrastructure.Persistence.Repositories;

namespace RentBridge.Infrastructure.Persistence;

public class AppDbContext : DbContext
    {
        private readonly IMediator _mediator;
        private readonly ILogger<AppDbContext> _logger;

        public AppDbContext(
            DbContextOptions<AppDbContext> options,
            IMediator mediator,
            ILogger<AppDbContext> logger)
            : base(options)
        {
            _mediator = mediator;
            _logger = logger;
        }

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

        // Drain the event queues before publishing. A handler that saves again
        // re-enters this method, and without this it would re-collect the same
        // still-tracked events and republish them, recursing without bound.
        foreach (var entry in ChangeTracker.Entries<Entity<Guid>>())
        {
            entry.Entity.ClearDomainEvents();
        }

        foreach (var domainEvent in domainEvents)
        {
            try
            {
                await _mediator.Publish(domainEvent, cancellationToken);
            }
            catch (Exception ex)
            {
                // The write is already committed; a handler failure must not be
                // reported to the caller as a failed mutation.
                _logger.LogError(ex,
                    "Domain event {EventType} handler failed after commit; the change is persisted.",
                    domainEvent.GetType().Name);
            }
        }

        return result;
    }
}
