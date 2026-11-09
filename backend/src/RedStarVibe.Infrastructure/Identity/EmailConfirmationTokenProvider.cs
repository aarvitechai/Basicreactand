using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace RedStarVibe.Infrastructure.Identity;

/// <summary>
/// Separate token provider so email-confirmation links can live longer (24 h) than password-reset links (1 h);
/// people often confirm the next day, but a reset link should be short-lived.
/// </summary>
internal sealed class EmailConfirmationTokenProvider<TUser>(
    IDataProtectionProvider dataProtectionProvider,
    IOptions<EmailConfirmationTokenProviderOptions> options,
    ILogger<DataProtectorTokenProvider<TUser>> logger)
    : DataProtectorTokenProvider<TUser>(dataProtectionProvider, options, logger)
    where TUser : class
{
    public const string ProviderName = "EmailConfirmation";
}

internal sealed class EmailConfirmationTokenProviderOptions : DataProtectionTokenProviderOptions
{
    public EmailConfirmationTokenProviderOptions()
    {
        Name = EmailConfirmationTokenProvider<object>.ProviderName;
        TokenLifespan = TimeSpan.FromHours(24);
    }
}
