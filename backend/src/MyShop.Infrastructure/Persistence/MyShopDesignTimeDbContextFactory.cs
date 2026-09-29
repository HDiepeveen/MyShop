using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace MyShop.Infrastructure.Persistence;

public sealed class MyShopDesignTimeDbContextFactory : IDesignTimeDbContextFactory<MyShopDbContext>
{
    public MyShopDbContext CreateDbContext(string[] args)
    {
        // A design-only target keeps schema generation independent of API startup and configuration.
        // Database updates must explicitly select their target with dotnet ef --connection.
        var options = new DbContextOptionsBuilder<MyShopDbContext>()
            .UseSqlServer(@"Server=(localdb)\MSSQLLocalDB;Database=MyShopDesignTime;Integrated Security=True;TrustServerCertificate=True")
            .Options;
        return new MyShopDbContext(options);
    }
}
