using Microsoft.EntityFrameworkCore;
using SharePointExternalUserManager.Api.Data.Entities;

namespace SharePointExternalUserManager.Api.Data;

/// <summary>
/// Application database context for SharePoint External User Manager SaaS platform
/// Supports multi-tenant data isolation with tenant-scoped queries
/// </summary>
public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    // DbSets
    public DbSet<TenantEntity> Tenants => Set<TenantEntity>();
    public DbSet<ClientEntity> Clients => Set<ClientEntity>();
    public DbSet<SubscriptionEntity> Subscriptions => Set<SubscriptionEntity>();
    public DbSet<AuditLogEntity> AuditLogs => Set<AuditLogEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure Tenant
        modelBuilder.Entity<TenantEntity>(entity =>
        {
            // Unique constraint on EntraIdTenantId
            entity.HasIndex(e => e.EntraIdTenantId)
                .IsUnique()
                .HasDatabaseName("UQ_Tenants_EntraIdTenantId");

            // Index on Status for filtering
            entity.HasIndex(e => e.Status)
                .HasDatabaseName("IX_Tenants_Status");

            // Index on CreatedDate for queries
            entity.HasIndex(e => e.CreatedDate)
                .HasDatabaseName("IX_Tenants_CreatedDate");
        });

        // Configure Client
        modelBuilder.Entity<ClientEntity>(entity =>
        {
            // Index on TenantId for tenant isolation
            entity.HasIndex(e => e.TenantId)
                .HasDatabaseName("IX_Clients_TenantId");

            // Composite index for tenant-scoped queries
            entity.HasIndex(e => new { e.TenantId, e.CreatedDate })
                .HasDatabaseName("IX_Clients_TenantId_CreatedDate");

            // Index on ClientReference for lookups
            entity.HasIndex(e => new { e.TenantId, e.ClientReference })
                .HasDatabaseName("IX_Clients_TenantId_ClientReference");

            // Index on ProvisioningStatus for filtering
            entity.HasIndex(e => new { e.TenantId, e.ProvisioningStatus })
                .HasDatabaseName("IX_Clients_TenantId_ProvisioningStatus");

            // Configure relationship with Tenant
            entity.HasOne(e => e.Tenant)
                .WithMany(t => t.Clients)
                .HasForeignKey(e => e.TenantId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Configure Subscription
        modelBuilder.Entity<SubscriptionEntity>(entity =>
        {
            // Index on TenantId for tenant isolation
            entity.HasIndex(e => e.TenantId)
                .HasDatabaseName("IX_Subscriptions_TenantId");

            // Index on Status for filtering
            entity.HasIndex(e => new { e.TenantId, e.Status })
                .HasDatabaseName("IX_Subscriptions_TenantId_Status");

            // Index on TrialExpiry for trial expiration queries
            entity.HasIndex(e => e.TrialExpiry)
                .HasDatabaseName("IX_Subscriptions_TrialExpiry")
                .HasFilter("[TrialExpiry] IS NOT NULL");

            // Index on StripeSubscriptionId for webhook lookups
            entity.HasIndex(e => e.StripeSubscriptionId)
                .HasDatabaseName("IX_Subscriptions_StripeSubscriptionId")
                .HasFilter("[StripeSubscriptionId] IS NOT NULL");

            // Configure relationship with Tenant
            entity.HasOne(e => e.Tenant)
                .WithMany(t => t.Subscriptions)
                .HasForeignKey(e => e.TenantId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Configure AuditLog
        modelBuilder.Entity<AuditLogEntity>(entity =>
        {
            // Composite index for tenant-scoped audit queries
            entity.HasIndex(e => new { e.TenantId, e.Timestamp })
                .IsDescending(false, true) // TenantId ASC, Timestamp DESC
                .HasDatabaseName("IX_AuditLogs_TenantId_Timestamp");

            // Index on CorrelationId for request tracing
            entity.HasIndex(e => e.CorrelationId)
                .HasDatabaseName("IX_AuditLogs_CorrelationId")
                .HasFilter("[CorrelationId] IS NOT NULL");

            // Index on Action for action-based queries
            entity.HasIndex(e => new { e.TenantId, e.Action })
                .HasDatabaseName("IX_AuditLogs_TenantId_Action");

            // Index on UserId for user activity queries
            entity.HasIndex(e => new { e.TenantId, e.UserId })
                .HasDatabaseName("IX_AuditLogs_TenantId_UserId")
                .HasFilter("[UserId] IS NOT NULL");

            // Configure relationship with Tenant
            entity.HasOne(e => e.Tenant)
                .WithMany(t => t.AuditLogs)
                .HasForeignKey(e => e.TenantId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }

    /// <summary>
    /// Override SaveChanges to automatically update ModifiedDate
    /// </summary>
    public override int SaveChanges()
    {
        UpdateTimestamps();
        return base.SaveChanges();
    }

    /// <summary>
    /// Override SaveChangesAsync to automatically update ModifiedDate
    /// </summary>
    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        UpdateTimestamps();
        return base.SaveChangesAsync(cancellationToken);
    }

    private void UpdateTimestamps()
    {
        var entries = ChangeTracker.Entries()
            .Where(e => e.State == EntityState.Added || e.State == EntityState.Modified);

        foreach (var entry in entries)
        {
            // Update ModifiedDate for all entities
            if (entry.Entity.GetType().GetProperty("ModifiedDate") != null)
            {
                entry.Property("ModifiedDate").CurrentValue = DateTime.UtcNow;
            }

            // Set CreatedDate only on new entities
            if (entry.State == EntityState.Added &&
                entry.Entity.GetType().GetProperty("CreatedDate") != null)
            {
                entry.Property("CreatedDate").CurrentValue = DateTime.UtcNow;
            }
        }
    }
}
