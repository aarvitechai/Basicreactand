using System.Text.RegularExpressions;

namespace RedStarVibe.Application.Common;

/// <summary>Phone numbers are stored in E.164 format: '+', country code, subscriber number (max 15 digits).</summary>
public static partial class PhoneNumbers
{
    public const string InvalidMessage = "Enter your number in international format, e.g. +91 98765 43210.";

    /// <summary>Strips spaces, dashes, dots and parentheses, then validates E.164. Returns null if invalid.</summary>
    public static string? Normalize(string? input)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return null;
        }

        var compact = SeparatorPattern().Replace(input.Trim(), string.Empty);
        if (compact.StartsWith("00", StringComparison.Ordinal))
        {
            compact = "+" + compact[2..];
        }

        return E164Pattern().IsMatch(compact) ? compact : null;
    }

    [GeneratedRegex(@"[\s\-\.\(\)]")]
    private static partial Regex SeparatorPattern();

    [GeneratedRegex(@"^\+[1-9]\d{7,14}$")]
    private static partial Regex E164Pattern();
}
