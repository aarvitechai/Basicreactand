using Microsoft.AspNetCore.DataProtection.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using RedStarVibe.Infrastructure.Identity;

namespace RedStarVibe.Infrastructure.Persistence;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options)
    : IdentityDbContext<ApplicationUser>(options), IDataProtectionKeyContext
{
    /// <summary>Data Protection keys (encrypt auth cookies). Stored in the DB so every instance shares them.</summary>
    public DbSet<DataProtectionKey> DataProtectionKeys => Set<DataProtectionKey>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        base.OnModelCreating(builder);

        builder.Entity<ApplicationUser>(user =>
        {
            user.Property(u => u.DisplayName).HasMaxLength(100).IsRequired();
            user.Property(u => u.CreatedAtUtc).IsRequired();

            // Identity's default email index is non-unique; enforce uniqueness at the database level
            // so concurrent registrations with the same email cannot both succeed.
            user.HasIndex(u => u.NormalizedEmail)
                .HasDatabaseName("UX_AspNetUsers_NormalizedEmail")
                .IsUnique()
                .HasFilter("[NormalizedEmail] IS NOT NULL");
        });
    }
}
