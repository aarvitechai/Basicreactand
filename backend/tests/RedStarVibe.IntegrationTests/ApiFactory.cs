using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using RedStarVibe.Application.Abstractions;
using RedStarVibe.Infrastructure.Persistence;

namespace RedStarVibe.IntegrationTests;

/// <summary>
/// Boots the real API in-memory, but swaps SQL Server for an in-memory SQLite database
/// so tests are self-contained and run in CI without a database server.
/// </summary>
public sealed class ApiFactory : WebApplicationFactory<Program>
{
    private readonly SqliteConnection _connection = new("DataSource=:memory:");

    public CapturingEmailSender Emails { get; } = new();

    public CapturingSmsSender Sms { get; } = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        _connection.Open();

        builder.UseEnvironment("Development");
        builder.ConfigureAppConfiguration((_, config) => config.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Database:ApplyMigrationsOnStartup"] = "false",
            ["RateLimiting:Auth:PermitLimit"] = "1000",
            ["RateLimiting:Sms:PermitLimit"] = "1000",
            ["RateLimiting:SmsVerify:PermitLimit"] = "1000",
        }));

        builder.ConfigureServices(services =>
        {
            var toRemove = services
                .Where(d => d.ServiceType == typeof(DbContextOptions<AppDbContext>)
                         || d.ServiceType == typeof(IDbContextOptionsConfiguration<AppDbContext>))
                .ToList();
            foreach (var descriptor in toRemove)
            {
                services.Remove(descriptor);
            }

            services.AddDbContext<AppDbContext>(options => options.UseSqlite(_connection));

            services.RemoveAll<IEmailSender>();
            services.AddSingleton<IEmailSender>(Emails);
            services.RemoveAll<ISmsSender>();
            services.AddSingleton<ISmsSender>(Sms);

            using var provider = services.BuildServiceProvider();
            using var scope = provider.CreateScope();
            scope.ServiceProvider.GetRequiredService<AppDbContext>().Database.EnsureCreated();
        });
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing)
        {
            _connection.Dispose();
            Emails.Dispose();
        }
    }
}
