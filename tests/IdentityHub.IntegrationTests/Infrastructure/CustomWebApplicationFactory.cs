using IdentityHub.Application.Interfaces;
using IdentityHub.Infrastructure.Data;
using IdentityHub.IntegrationTests.Infrastructure.Fakes;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Xunit;

namespace IdentityHub.IntegrationTests.Infrastructure;

/// <summary>
/// Spins up the real ASP.NET Core pipeline with:
///   - SQLite in-memory database (instead of SQL Server)
///   - FakeGraphService (no Azure AD calls)
///   - TestAuthHandler (no real JWT validation)
/// </summary>
public class CustomWebApplicationFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    // Keep a single shared in-memory connection so EF Core uses the same database
    // instance across multiple DbContext scopes during a test run.
    private readonly SqliteConnection _connection = new("DataSource=:memory:");

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // Provide minimal config so the app does not throw on startup
        builder.ConfigureAppConfiguration(config =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["EntraId:Instance"] = "https://login.microsoftonline.com/",
                ["EntraId:TenantId"] = TestAuthHandler.TestTenantId,
                ["EntraId:ClientId"] = "00000000-0000-0000-0000-000000000000",
                ["EntraId:Domain"] = "test.onmicrosoft.com",
                ["EntraId:Audience"] = "api://00000000-0000-0000-0000-000000000000",
                // Dummy connection string – replaced below with the SQLite connection
                ["ConnectionStrings:AuthorizationDb"] = "DataSource=:memory:",
            });
        });

        builder.ConfigureServices(services =>
        {
            // ── Database ──────────────────────────────────────────────────────
            // Remove the SQL Server DbContext registered in Program.cs
            services.RemoveAll<DbContextOptions<IdentityHubDbContext>>();
            // Register SQLite using the shared in-memory connection
            services.AddDbContext<IdentityHubDbContext>(options =>
                options.UseSqlite(_connection));

            // ── Graph Service ─────────────────────────────────────────────────
            // Replace the real Graph service (requires Azure credentials) with fake
            services.RemoveAll<IGraphService>();
            services.AddScoped<IGraphService, FakeGraphService>();

            // ── Authentication ────────────────────────────────────────────────
            // Register the test scheme handler.
            services.AddAuthentication()
                .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(
                    TestAuthHandler.SchemeName, _ => { });

            // PostConfigure runs AFTER all other Configure<AuthenticationOptions> calls
            // (including those from AddMicrosoftIdentityWebApi), so our scheme wins.
            services.PostConfigure<AuthenticationOptions>(options =>
            {
                options.DefaultScheme = TestAuthHandler.SchemeName;
                options.DefaultAuthenticateScheme = TestAuthHandler.SchemeName;
                options.DefaultChallengeScheme = TestAuthHandler.SchemeName;
                options.DefaultForbidScheme = TestAuthHandler.SchemeName;
            });

            // ── Authorization ─────────────────────────────────────────────────
            // Replace the authorization service entirely so all [Authorize] and
            // [RequirePermission] checks pass in tests. Business logic is what
            // we're testing here, not the authorization framework itself.
            services.RemoveAll<IAuthorizationService>();
            services.AddSingleton<IAuthorizationService, PermissiveAuthorizationService>();

            // Keep permissive handlers as a belt-and-suspenders fallback for any
            // code paths that call IAuthorizationHandler directly.
            services.RemoveAll<IAuthorizationHandler>();
            services.AddScoped<IAuthorizationHandler, PermissiveAuthorizationHandler>();
            services.AddScoped<IAuthorizationHandler, PermissiveDenyAnonymousHandler>();

            // Add AllowAnonymousFilter globally so [Authorize] attributes are bypassed
            // for all controller actions. Authentication still runs (TestAuthHandler)
            // so TenantIsolationMiddleware can extract claims and set the tenant context.
            services.Configure<MvcOptions>(options =>
                options.Filters.Add(new AllowAnonymousFilter()));
        });

        builder.UseEnvironment("Testing");
    }

    public async Task InitializeAsync()
    {
        _connection.Open();

        // Create the SQLite schema from the EF Core model
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IdentityHubDbContext>();
        await db.Database.EnsureCreatedAsync();
    }

    public new async Task DisposeAsync()
    {
        await base.DisposeAsync();
        await _connection.DisposeAsync();
    }
}
