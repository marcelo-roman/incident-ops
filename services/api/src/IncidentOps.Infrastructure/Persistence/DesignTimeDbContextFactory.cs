using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace IncidentOps.Infrastructure.Persistence;

internal sealed class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<IncidentOpsDbContext>
{
    private const string PlaceholderConnectionString =
        "Server=localhost,1433;Database=IncidentOps;Integrated Security=false;TrustServerCertificate=true";

    public IncidentOpsDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<IncidentOpsDbContext>()
            .UseSqlServer(PlaceholderConnectionString)
            .Options;

        return new IncidentOpsDbContext(options);
    }
}
