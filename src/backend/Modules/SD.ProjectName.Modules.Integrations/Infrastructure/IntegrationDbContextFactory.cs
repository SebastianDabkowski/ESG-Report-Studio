using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace SD.ProjectName.Modules.Integrations.Infrastructure;

/// <summary>
/// Factory for creating IntegrationDbContext instances at design time (for migrations)
/// </summary>
public class IntegrationDbContextFactory : IDesignTimeDbContextFactory<IntegrationDbContext>
{
    public IntegrationDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<IntegrationDbContext>();
        optionsBuilder.UseSqlServer("Server=(localdb)\\mssqllocaldb;Database=ESGReportStudio;Trusted_Connection=True;MultipleActiveResultSets=true");

        return new IntegrationDbContext(optionsBuilder.Options);
    }
}
