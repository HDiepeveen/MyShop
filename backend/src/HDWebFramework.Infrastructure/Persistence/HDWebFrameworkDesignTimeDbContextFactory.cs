using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace HDWebFramework.Infrastructure.Persistence;

public sealed class HDWebFrameworkDesignTimeDbContextFactory : IDesignTimeDbContextFactory<HDWebFrameworkDbContext>
{
    public HDWebFrameworkDbContext CreateDbContext(string[] args)
    {
        // A design-only target keeps schema generation independent of API startup and configuration.
        // Database updates must explicitly select their target with dotnet ef --connection.
        var options = new DbContextOptionsBuilder<HDWebFrameworkDbContext>()
            .UseSqlServer(@"Server=(localdb)\MSSQLLocalDB;Database=HDWebFrameworkDesignTime;Integrated Security=True;TrustServerCertificate=True")
            .Options;
        return new HDWebFrameworkDbContext(options);
    }
}
