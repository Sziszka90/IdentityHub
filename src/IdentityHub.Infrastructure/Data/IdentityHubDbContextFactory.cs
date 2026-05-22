using System.IO;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace IdentityHub.Infrastructure.Data
{
    public class IdentityHubDbContextFactory : IDesignTimeDbContextFactory<IdentityHubDbContext>
    {
        public IdentityHubDbContext CreateDbContext(string[] args)
        {
            // EF Core sets the working directory to the startup project directory at design time.
            var basePath = Directory.GetCurrentDirectory();

            var configuration = new ConfigurationBuilder()
                .SetBasePath(basePath)
                .AddJsonFile("appsettings.json", optional: true)
                .AddJsonFile("appsettings.Development.json", optional: true)
                .AddEnvironmentVariables()
                .Build();

            var connectionString = configuration.GetConnectionString("AuthorizationDb");
            if (string.IsNullOrEmpty(connectionString))
            {
                throw new InvalidOperationException(
                    $"Connection string 'AuthorizationDb' not found. Searched in: {basePath}");
            }

            var optionsBuilder = new DbContextOptionsBuilder<IdentityHubDbContext>();
            optionsBuilder.UseSqlServer(connectionString);

            return new IdentityHubDbContext(optionsBuilder.Options);
        }
    }
}
