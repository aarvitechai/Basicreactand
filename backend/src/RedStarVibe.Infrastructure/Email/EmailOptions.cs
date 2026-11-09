using System.ComponentModel.DataAnnotations;

namespace RedStarVibe.Infrastructure.Email;

public sealed class EmailOptions
{
    public const string SectionName = "Email";

    [Required, EmailAddress]
    public string FromAddress { get; set; } = "no-reply@redstarvibe.local";

    [Required]
    public string FromName { get; set; } = "Red Star Vibe";

    /// <summary>SMTP settings. When <see cref="SmtpOptions.Host"/> is empty, Development logs emails instead of sending.</summary>
    public SmtpOptions Smtp { get; set; } = new();
}

public sealed class SmtpOptions
{
    public string? Host { get; set; }

    [Range(1, 65535)]
    public int Port { get; set; } = 587;

    public string? Username { get; set; }

    /// <summary>Secret: supply via user-secrets or environment variable (Email__Smtp__Password), never appsettings.</summary>
    public string? Password { get; set; }

    /// <summary>Use STARTTLS (port 587). Set false only for local test servers such as smtp4dev/Mailpit.</summary>
    public bool UseStartTls { get; set; } = true;
}

public sealed class AppUrlOptions
{
    public const string SectionName = "App";

    /// <summary>
    /// Public URL of the frontend, used to build links in emails. Configured rather than taken from the
    /// request's Host header, which an attacker could spoof to send users a reset link pointing to their site.
    /// </summary>
    [Required, Url]
    public string FrontendBaseUrl { get; set; } = string.Empty;
}
