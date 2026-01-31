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
    }
}
