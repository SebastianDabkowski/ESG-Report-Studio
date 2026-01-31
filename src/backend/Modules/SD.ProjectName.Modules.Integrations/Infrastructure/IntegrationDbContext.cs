using Microsoft.EntityFrameworkCore;
using SD.ProjectName.Modules.Integrations.Domain.Entities;

namespace SD.ProjectName.Modules.Integrations.Infrastructure;

/// <summary>
/// DbContext for the Integrations module
/// </summary>
public class IntegrationDbContext : DbContext
{
    public IntegrationDbContext(DbContextOptions<IntegrationDbContext> options)
        : base(options)
    {
    }

    public DbSet<Connector> Connectors { get; set; } = null!;
    public DbSet<IntegrationLog> IntegrationLogs { get; set; } = null!;
    public DbSet<HREntity> HREntities { get; set; } = null!;
    public DbSet<HRSyncRecord> HRSyncRecords { get; set; } = null!;
    public DbSet<FinanceEntity> FinanceEntities { get; set; } = null!;
    public DbSet<FinanceSyncRecord> FinanceSyncRecords { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Connector configuration
        modelBuilder.Entity<Connector>(entity =>
        {
            entity.ToTable("Connectors");
            entity.HasKey(c => c.Id);
            entity.Property(c => c.Name).IsRequired().HasMaxLength(200);
            entity.Property(c => c.ConnectorType).IsRequired().HasMaxLength(100);
            entity.Property(c => c.Status).IsRequired();
            entity.Property(c => c.EndpointBaseUrl).IsRequired().HasMaxLength(500);
            entity.Property(c => c.AuthenticationType).IsRequired().HasMaxLength(50);
            entity.Property(c => c.AuthenticationSecretRef).IsRequired().HasMaxLength(500);
            entity.Property(c => c.Capabilities).IsRequired().HasMaxLength(200);
            entity.Property(c => c.MappingConfiguration).HasColumnType("nvarchar(max)");
            entity.Property(c => c.Description).HasMaxLength(1000);
            entity.Property(c => c.CreatedBy).IsRequired().HasMaxLength(200);
            entity.Property(c => c.UpdatedBy).HasMaxLength(200);
            
            entity.HasIndex(c => c.ConnectorType);
            entity.HasIndex(c => c.Status);
        });

        // IntegrationLog configuration
        modelBuilder.Entity<IntegrationLog>(entity =>
        {
            entity.ToTable("IntegrationLogs");
            entity.HasKey(l => l.Id);
            entity.Property(l => l.CorrelationId).IsRequired().HasMaxLength(100);
            entity.Property(l => l.OperationType).IsRequired().HasMaxLength(50);
            entity.Property(l => l.Status).IsRequired();
            entity.Property(l => l.HttpMethod).HasMaxLength(10);
            entity.Property(l => l.Endpoint).HasMaxLength(500);
            entity.Property(l => l.ErrorMessage).HasMaxLength(2000);
            entity.Property(l => l.ErrorDetails).HasColumnType("nvarchar(max)");
            entity.Property(l => l.RequestSummary).HasColumnType("nvarchar(max)");
            entity.Property(l => l.ResponseSummary).HasColumnType("nvarchar(max)");
            entity.Property(l => l.InitiatedBy).IsRequired().HasMaxLength(200);
            
            entity.HasIndex(l => l.ConnectorId);
            entity.HasIndex(l => l.CorrelationId);
            entity.HasIndex(l => l.Status);
            entity.HasIndex(l => l.StartedAt);
            
            // Foreign key relationship
            entity.HasOne(l => l.Connector)
                .WithMany()
                .HasForeignKey(l => l.ConnectorId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // HREntity configuration
        modelBuilder.Entity<HREntity>(entity =>
        {
            entity.ToTable("HREntities");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.ExternalId).IsRequired().HasMaxLength(200);
            entity.Property(e => e.EntityType).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Data).HasColumnType("nvarchar(max)");
            entity.Property(e => e.MappedData).HasColumnType("nvarchar(max)");
            entity.Property(e => e.IsApproved).IsRequired();
            
            entity.HasIndex(e => e.ConnectorId);
            entity.HasIndex(e => new { e.ConnectorId, e.ExternalId }).IsUnique();
            entity.HasIndex(e => e.EntityType);
            entity.HasIndex(e => e.IsApproved);
            
            // Foreign key relationship
            entity.HasOne(e => e.Connector)
                .WithMany()
                .HasForeignKey(e => e.ConnectorId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // HRSyncRecord configuration
        modelBuilder.Entity<HRSyncRecord>(entity =>
        {
            entity.ToTable("HRSyncRecords");
            entity.HasKey(r => r.Id);
            entity.Property(r => r.CorrelationId).IsRequired().HasMaxLength(100);
            entity.Property(r => r.Status).IsRequired();
            entity.Property(r => r.ExternalId).HasMaxLength(200);
            entity.Property(r => r.RawData).HasColumnType("nvarchar(max)");
            entity.Property(r => r.RejectionReason).HasMaxLength(2000);
            entity.Property(r => r.InitiatedBy).IsRequired().HasMaxLength(200);
            
            entity.HasIndex(r => r.ConnectorId);
            entity.HasIndex(r => r.CorrelationId);
            entity.HasIndex(r => r.Status);
            entity.HasIndex(r => r.SyncedAt);
            
            // Foreign key relationships
            entity.HasOne(r => r.Connector)
                .WithMany()
                .HasForeignKey(r => r.ConnectorId)
                .OnDelete(DeleteBehavior.Restrict);
            
            entity.HasOne(r => r.HREntity)
                .WithMany()
                .HasForeignKey(r => r.HREntityId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        // FinanceEntity configuration
        modelBuilder.Entity<FinanceEntity>(entity =>
        {
            entity.ToTable("FinanceEntities");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.ExternalId).IsRequired().HasMaxLength(200);
            entity.Property(e => e.EntityType).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Data).HasColumnType("nvarchar(max)");
            entity.Property(e => e.MappedData).HasColumnType("nvarchar(max)");
            entity.Property(e => e.IsApproved).IsRequired();
            entity.Property(e => e.SourceSystem).IsRequired().HasMaxLength(200);
            entity.Property(e => e.ImportJobId).IsRequired().HasMaxLength(200);
            
            entity.HasIndex(e => e.ConnectorId);
            entity.HasIndex(e => new { e.ConnectorId, e.ExternalId }).IsUnique();
            entity.HasIndex(e => e.EntityType);
            entity.HasIndex(e => e.IsApproved);
            entity.HasIndex(e => e.ImportJobId);
            
            // Foreign key relationship
            entity.HasOne(e => e.Connector)
                .WithMany()
                .HasForeignKey(e => e.ConnectorId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // FinanceSyncRecord configuration
        modelBuilder.Entity<FinanceSyncRecord>(entity =>
        {
            entity.ToTable("FinanceSyncRecords");
            entity.HasKey(r => r.Id);
            entity.Property(r => r.CorrelationId).IsRequired().HasMaxLength(100);
            entity.Property(r => r.Status).IsRequired();
            entity.Property(r => r.ExternalId).HasMaxLength(200);
            entity.Property(r => r.RawData).HasColumnType("nvarchar(max)");
            entity.Property(r => r.RejectionReason).HasMaxLength(2000);
            entity.Property(r => r.ConflictResolution).HasMaxLength(100);
            entity.Property(r => r.ApprovedOverrideBy).HasMaxLength(200);
            entity.Property(r => r.InitiatedBy).IsRequired().HasMaxLength(200);
            
            entity.HasIndex(r => r.ConnectorId);
            entity.HasIndex(r => r.CorrelationId);
            entity.HasIndex(r => r.Status);
            entity.HasIndex(r => r.SyncedAt);
            entity.HasIndex(r => r.ConflictDetected);
            
            // Foreign key relationships
            entity.HasOne(r => r.Connector)
                .WithMany()
                .HasForeignKey(r => r.ConnectorId)
                .OnDelete(DeleteBehavior.Restrict);
            
            entity.HasOne(r => r.FinanceEntity)
                .WithMany()
                .HasForeignKey(r => r.FinanceEntityId)
                .OnDelete(DeleteBehavior.SetNull);
        });
    }
}
