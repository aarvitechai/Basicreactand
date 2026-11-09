using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using RedStarVibe.Application.Abstractions;
using RedStarVibe.Application.Auth;
using RedStarVibe.Application.Weather;
using RedStarVibe.Infrastructure.Identity;
using RedStarVibe.Infrastructure.Persistence;
using RedStarVibe.Infrastructure.Time;
using RedStarVibe.Infrastructure.Weather;

namespace RedStarVibe.Infrastructure;

public static class DependencyInjection
{
    public const string ConnectionStringName = "DefaultConnection";
    public const string AuthCookieName = "rsv.auth";

    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentNullException.ThrowIfNull(environment);

        services.AddSingleton<IDateTimeProvider, SystemDateTimeProvider>();
        services.AddSingleton<IWeatherDataSource, RandomWeatherDataSource>();

        AddPersistence(services, configuration);
        AddIdentity(services, environment);

        return services;
    }

    private static void AddPersistence(IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString(ConnectionStringName);

        services.AddDbContext<AppDbContext>(options =>
        {
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                throw new InvalidOperationException(
                    $"Connection string '{ConnectionStringName}' is not configured. " +
                    $"Set ConnectionStrings__{ConnectionStringName} or add it to appsettings / user-secrets.");
            }

            options.UseSqlServer(connectionString, sql => sql.EnableRetryOnFailure(maxRetryCount: 5));
        });

        services.AddHealthChecks().AddDbContextCheck<AppDbContext>("database", tags: ["ready"]);
    }

    private static void AddIdentity(IServiceCollection services, IHostEnvironment environment)
    {
        services
            .AddIdentityCore<ApplicationUser>(options =>
            {
                options.User.RequireUniqueEmail = true;

                options.Password.RequiredLength = 8;
                options.Password.RequireDigit = true;
                options.Password.RequireLowercase = true;
                options.Password.RequireUppercase = true;
                options.Password.RequireNonAlphanumeric = true;

                options.Lockout.AllowedForNewUsers = true;
                options.Lockout.MaxFailedAccessAttempts = 5;
                options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);

                // Flip to true once an email sender is wired up for confirmation links.
                options.SignIn.RequireConfirmedAccount = false;
            })
            .AddEntityFrameworkStores<AppDbContext>()
            .AddSignInManager()
            .AddDefaultTokenProviders();

        services
            .AddAuthentication(IdentityConstants.ApplicationScheme)
            .AddIdentityCookies();

        services.ConfigureApplicationCookie(options =>
        {
            options.Cookie.Name = AuthCookieName;
            options.Cookie.HttpOnly = true;
            options.Cookie.SameSite = SameSiteMode.Strict;
            options.Cookie.SecurePolicy = environment.IsDevelopment()
                ? CookieSecurePolicy.SameAsRequest
                : CookieSecurePolicy.Always;
            options.ExpireTimeSpan = TimeSpan.FromHours(8);
            options.SlidingExpiration = true;

            // This is an API: respond with status codes instead of redirecting to a login page.
            options.Events.OnRedirectToLogin = ctx =>
            {
                ctx.Response.StatusCode = StatusCodes.Status401Unauthorized;
                return Task.CompletedTask;
            };
            options.Events.OnRedirectToAccessDenied = ctx =>
            {
                ctx.Response.StatusCode = StatusCodes.Status403Forbidden;
                return Task.CompletedTask;
            };
        });

        // Persist cookie-encryption keys in SQL so sessions survive restarts and work across instances.
        services.AddDataProtection()
            .SetApplicationName("RedStarVibe")
            .PersistKeysToDbContext<AppDbContext>();

        services.AddScoped<IAuthService, IdentityAuthService>();
    }
}
