using ITSupportSystem.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace ITSupportSystem.API.Tests.Infrastructure;

/// <summary>
/// Boots the real API in memory, but points EF Core at a private in-memory
/// SQLite database instead of Data/AppDb.db. Every factory gets its own
/// database, so tests can't see each other's data.
/// </summary>
public class ApiFactory : WebApplicationFactory<Program>
{
    // The seeded admin's login. Tests run in the "Testing" environment, where user
    // secrets don't load, so the factory supplies these values itself.
    public const string AdminUserName = "admin";
    public const string AdminEmail = "admin@example.com";
    public const string AdminPassword = "TestOnly-Admin1!";

    // An in-memory SQLite database only lives while a connection to it is open,
    // so we hold one open for the lifetime of the factory.
    private readonly SqliteConnection _connection = new("DataSource=:memory:");

    public ApiFactory()
    {
        _connection.Open();

        // Program.cs seeds roles + the admin right after Build(), before anything
        // else runs, so the tables have to exist before the app starts.
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite(_connection)
            .Options;

        using var db = new ApplicationDbContext(options);
        db.Database.EnsureCreated();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.UseSetting("SeedAdmin:UserName", AdminUserName);
        builder.UseSetting("SeedAdmin:Email", AdminEmail);
        builder.UseSetting("SeedAdmin:Password", AdminPassword);

        builder.ConfigureServices(services =>
        {
            // Remove the app's UseSqlite(connectionString) registration...
            services.RemoveAll<DbContextOptions<ApplicationDbContext>>();
            services.RemoveAll<IDbContextOptionsConfiguration<ApplicationDbContext>>();

            // ...and replace it with the shared in-memory connection.
            services.AddDbContext<ApplicationDbContext>(o => o.UseSqlite(_connection));
        });
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing) _connection.Dispose();
    }
}
