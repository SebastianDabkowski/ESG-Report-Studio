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
    public DbSet<CanonicalEntity> CanonicalEntities { get; set; } = null!;
    public DbSet<CanonicalEntityVersion> CanonicalEntityVersions { get; set; } = null!;
    public DbSet<CanonicalAttribute> CanonicalAttributes { get; set; } = null!;
    public DbSet<CanonicalMapping> CanonicalMappings { get; set; } = null!;
    public DbSet<WebhookSubscription> WebhookSubscriptions { get; set; } = null!;
    public DbSet<WebhookDelivery> WebhookDeliveries { get; set; } = null!;

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
            
            // Foreign key to canonical entity
            entity.HasOne(e => e.CanonicalEntity)
                .WithMany()
                .HasForeignKey(e => e.CanonicalEntityId)
                .OnDelete(DeleteBehavior.SetNull);
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
            
            // Foreign key to canonical entity
            entity.HasOne(e => e.CanonicalEntity)
                .WithMany()
                .HasForeignKey(e => e.CanonicalEntityId)
                .OnDelete(DeleteBehavior.SetNull);
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

        // CanonicalEntity configuration
        modelBuilder.Entity<CanonicalEntity>(entity =>
        {
            entity.ToTable("CanonicalEntities");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.EntityType).IsRequired();
            entity.Property(e => e.SchemaVersion).IsRequired();
            entity.Property(e => e.ExternalId).HasMaxLength(200);
            entity.Property(e => e.Data).HasColumnType("nvarchar(max)");
            entity.Property(e => e.SourceSystem).IsRequired().HasMaxLength(200);
            entity.Property(e => e.SourceVersion).HasMaxLength(100);
            entity.Property(e => e.ImportedByJobId).HasMaxLength(200);
            entity.Property(e => e.VendorExtensions).HasColumnType("nvarchar(max)");
            entity.Property(e => e.UpdatedBy).HasMaxLength(200);
            
            entity.HasIndex(e => e.EntityType);
            entity.HasIndex(e => new { e.SourceSystem, e.ExternalId });
            entity.HasIndex(e => e.ImportedByJobId);
            entity.HasIndex(e => e.SchemaVersion);
            entity.HasIndex(e => e.IsApproved);
            
            // Foreign key relationship to schema version
            entity.HasOne(e => e.Schema)
                .WithMany(v => v.Entities)
                .HasForeignKey(e => new { e.EntityType, e.SchemaVersion })
                .HasPrincipalKey(v => new { v.EntityType, v.Version })
                .OnDelete(DeleteBehavior.Restrict);
        });

        // CanonicalEntityVersion configuration
        modelBuilder.Entity<CanonicalEntityVersion>(entity =>
        {
            entity.ToTable("CanonicalEntityVersions");
            entity.HasKey(v => v.Id);
            entity.Property(v => v.EntityType).IsRequired();
            entity.Property(v => v.Version).IsRequired();
            entity.Property(v => v.SchemaDefinition).HasColumnType("nvarchar(max)");
            entity.Property(v => v.Description).IsRequired().HasMaxLength(1000);
            entity.Property(v => v.MigrationRules).HasColumnType("nvarchar(max)");
            entity.Property(v => v.CreatedBy).IsRequired().HasMaxLength(200);
            
            // Composite unique index for entity type + version
            entity.HasIndex(v => new { v.EntityType, v.Version }).IsUnique();
            entity.HasIndex(v => v.IsActive);
            entity.HasIndex(v => v.IsDeprecated);
        });

        // CanonicalAttribute configuration
        modelBuilder.Entity<CanonicalAttribute>(entity =>
        {
            entity.ToTable("CanonicalAttributes");
            entity.HasKey(a => a.Id);
            entity.Property(a => a.EntityType).IsRequired();
            entity.Property(a => a.SchemaVersion).IsRequired();
            entity.Property(a => a.AttributeName).IsRequired().HasMaxLength(200);
            entity.Property(a => a.DataType).IsRequired().HasMaxLength(50);
            entity.Property(a => a.Description).IsRequired().HasMaxLength(1000);
            entity.Property(a => a.ExampleValues).HasMaxLength(500);
            entity.Property(a => a.ValidationRules).HasColumnType("nvarchar(max)");
            entity.Property(a => a.DefaultValue).HasMaxLength(500);
            entity.Property(a => a.ReplacedBy).HasMaxLength(200);
            
            entity.HasIndex(a => new { a.EntityType, a.SchemaVersion, a.AttributeName }).IsUnique();
            entity.HasIndex(a => a.IsRequired);
            entity.HasIndex(a => a.IsDeprecated);
        });

        // CanonicalMapping configuration
        modelBuilder.Entity<CanonicalMapping>(entity =>
        {
            entity.ToTable("CanonicalMappings");
            entity.HasKey(m => m.Id);
            entity.Property(m => m.ConnectorId).IsRequired();
            entity.Property(m => m.TargetEntityType).IsRequired();
            entity.Property(m => m.TargetSchemaVersion).IsRequired();
            entity.Property(m => m.ExternalField).IsRequired().HasMaxLength(200);
            entity.Property(m => m.CanonicalAttribute).IsRequired().HasMaxLength(200);
            entity.Property(m => m.TransformationType).IsRequired().HasMaxLength(50);
            entity.Property(m => m.TransformationParams).HasColumnType("nvarchar(max)");
            entity.Property(m => m.DefaultValue).HasMaxLength(500);
            entity.Property(m => m.Notes).HasMaxLength(1000);
            entity.Property(m => m.CreatedBy).IsRequired().HasMaxLength(200);
            entity.Property(m => m.UpdatedBy).HasMaxLength(200);
            
            entity.HasIndex(m => m.ConnectorId);
            entity.HasIndex(m => new { m.ConnectorId, m.TargetEntityType });
            entity.HasIndex(m => new { m.ConnectorId, m.TargetEntityType, m.TargetSchemaVersion });
            entity.HasIndex(m => m.IsActive);
            entity.HasIndex(m => m.IsRequired);
            
            // Foreign key relationship to connector
            entity.HasOne(m => m.Connector)
                .WithMany()
                .HasForeignKey(m => m.ConnectorId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // WebhookSubscription configuration
        modelBuilder.Entity<WebhookSubscription>(entity =>
        {
            entity.ToTable("WebhookSubscriptions");
            entity.HasKey(s => s.Id);
            entity.Property(s => s.Name).IsRequired().HasMaxLength(200);
            entity.Property(s => s.EndpointUrl).IsRequired().HasMaxLength(500);
            entity.Property(s => s.SubscribedEvents).IsRequired().HasMaxLength(500);
            entity.Property(s => s.Status).IsRequired();
            entity.Property(s => s.SigningSecret).IsRequired().HasMaxLength(500);
            entity.Property(s => s.VerificationToken).HasMaxLength(200);
            entity.Property(s => s.DegradedReason).HasMaxLength(2000);
            entity.Property(s => s.Description).HasMaxLength(1000);
            entity.Property(s => s.CreatedBy).IsRequired().HasMaxLength(200);
            entity.Property(s => s.UpdatedBy).HasMaxLength(200);
            
            entity.HasIndex(s => s.Status);
            entity.HasIndex(s => s.EndpointUrl);
        });

        // WebhookDelivery configuration
        modelBuilder.Entity<WebhookDelivery>(entity =>
        {
            entity.ToTable("WebhookDeliveries");
            entity.HasKey(d => d.Id);
            entity.Property(d => d.EventType).IsRequired().HasMaxLength(100);
            entity.Property(d => d.CorrelationId).IsRequired().HasMaxLength(100);
            entity.Property(d => d.Payload).IsRequired().HasColumnType("nvarchar(max)");
            entity.Property(d => d.Signature).IsRequired().HasMaxLength(500);
            entity.Property(d => d.Status).IsRequired();
            entity.Property(d => d.LastResponseBody).HasColumnType("nvarchar(max)");
            entity.Property(d => d.LastErrorMessage).HasMaxLength(2000);
            
            entity.HasIndex(d => d.WebhookSubscriptionId);
            entity.HasIndex(d => d.CorrelationId);
            entity.HasIndex(d => d.Status);
            entity.HasIndex(d => d.CreatedAt);
            entity.HasIndex(d => d.NextRetryAt);
            
            // Foreign key relationship
            entity.HasOne(d => d.WebhookSubscription)
                .WithMany()
                .HasForeignKey(d => d.WebhookSubscriptionId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }
}
