using System.Net.Http.Json;
using System.Text.RegularExpressions;
using System.Web;
using RedStarVibe.Api.Contracts;

namespace RedStarVibe.IntegrationTests;

/// <summary>Helpers for creating accounts in integration tests.</summary>
public static partial class TestAccounts
{
    public const string Password = "Str0ng!Passw0rd";

    public static Uri Url(string path) => new(path, UriKind.Relative);

    public static string UniqueEmail(string prefix = "user") => $"{prefix}-{Guid.NewGuid():N}@example.com";

    /// <summary>Registers an account (unconfirmed). Returns the email.</summary>
    public static async Task<string> RegisterAsync(ApiFactory factory, string? email = null, string? phone = null)
    {
        email ??= UniqueEmail();
        var response = await factory.CreateClient().PostAsJsonAsync(
            Url("/api/v1/auth/register"), new RegisterRequest(email, Password, "Test User", phone));
        response.EnsureSuccessStatusCode();
        return email;
    }

    /// <summary>Extracts (userId, token) from the confirmation email sent to <paramref name="email"/>.</summary>
    public static async Task<(string UserId, string Token)> GetConfirmationLinkAsync(ApiFactory factory, string email, int count = 1)
    {
        var message = await factory.Emails.WaitForEmailToAsync(email, "Confirm", count);
        var link = new Uri(LinkPattern().Match(message.TextBody).Value);
        var query = HttpUtility.ParseQueryString(link.Query);
        return (query["userId"]!, query["token"]!);
    }

    /// <summary>Registers and confirms an account, then returns a client signed in as that user.</summary>
    public static async Task<(HttpClient Client, string Email)> RegisterConfirmedAndSignInAsync(ApiFactory factory, string? phone = null)
    {
        var email = await RegisterAsync(factory, phone: phone);
        var (userId, token) = await GetConfirmationLinkAsync(factory, email);

        var client = factory.CreateClient();
        (await client.PostAsJsonAsync(Url("/api/v1/auth/confirm-email"), new ConfirmEmailRequest(userId, token)))
            .EnsureSuccessStatusCode();
        (await client.PostAsJsonAsync(Url("/api/v1/auth/login"), new LoginRequest(email, Password)))
            .EnsureSuccessStatusCode();
        return (client, email);
    }

    [GeneratedRegex(@"http://localhost:5173/\S+")]
    private static partial Regex LinkPattern();
}
