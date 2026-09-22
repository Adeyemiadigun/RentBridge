using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace RentBridge.Infrastructure.Persistence;

/// <summary>
/// Design-time factory so `dotnet ef` can build AppDbContext without the
/// application service provider. Loads the same appsettings files Program.cs
/// would (Development wins in local runs) so migrations use the dev database
/// without tripping the runtime configuration guards (Hashing/Payment).
/// </summary>
public sealed class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var contentRoot = ResolveContentRoot();

        var configuration = new ConfigurationBuilder()
            .SetBasePath(contentRoot)
            .AddJsonFile("appsettings.json", optional: false)
            .AddJsonFile("appsettings.development.json", optional: true)
            .AddEnvironmentVariables()
            .Build();

        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' is not configured.");

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(connectionString)
            .Options;

        return new AppDbContext(options, mediator: null!);
    }

    /// <summary>
    /// `dotnet ef` can start from either the API (startup) or Infrastructure
    /// project directory; appsettings.json lives in the API project. Walk up
    /// until the file is found.
    /// </summary>
    private static string ResolveContentRoot()
    {
        var current = new DirectoryInfo(Directory.GetCurrentDirectory());
        while (current is not null)
        {
            if (File.Exists(Path.Combine(current.FullName, "appsettings.json")))
            {
                return current.FullName;
            }
            current = current.Parent;
        }

        return Directory.GetCurrentDirectory();
    }
}