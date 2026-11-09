using Microsoft.AspNetCore.Identity;

namespace RedStarVibe.Infrastructure.Identity;

/// <summary>Identity user persisted in the AspNetUsers table. UserName is always the email address.</summary>
public sealed class ApplicationUser : IdentityUser
{
    public required string DisplayName { get; set; }

    public DateTimeOffset CreatedAtUtc { get; set; }
}
