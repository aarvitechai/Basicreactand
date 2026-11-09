using System.Net;
using System.Net.Http.Json;
using System.Text.RegularExpressions;
using System.Web;
using Microsoft.AspNetCore.Mvc;
using RedStarVibe.Api.Contracts;

namespace RedStarVibe.IntegrationTests.Auth;

public sealed partial class PasswordResetTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    private const string OriginalPassword = "Str0ng!Passw0rd";
    private const string NewPassword = "N3w!Passw0rdXYZ";

    private static Uri Url(string path) => new(path, UriKind.Relative);

    private async Task<string> RegisterAsync()
    {
        var email = $"reset-{Guid.NewGuid():N}@example.com";
        var response = await factory.CreateClient()
            .PostAsJsonAsync(Url("/api/v1/auth/register"), new RegisterRequest(email, OriginalPassword, "Reset Tester"));
        response.EnsureSuccessStatusCode();
        return email;
    }

    private async Task<(Uri Link, string Email, string Token)> RequestResetLinkAsync(string email)
    {
        var response = await factory.CreateClient()
            .PostAsJsonAsync(Url("/api/v1/auth/forgot-password"), new ForgotPasswordRequest(email));
        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);

        var message = await factory.Emails.WaitForEmailToAsync(email);
        var link = new Uri(ResetLinkPattern().Match(message.TextBody).Value);
        var query = HttpUtility.ParseQueryString(link.Query);
        return (link, query["email"]!, query["token"]!);
    }

    private Task<HttpResponseMessage> LoginAsync(string email, string password) =>
        factory.CreateClient().PostAsJsonAsync(Url("/api/v1/auth/login"), new LoginRequest(email, password));

    [Fact]
    public async Task ForgotPassword_KnownEmail_EmailsLinkToConfiguredFrontend()
    {
        var email = await RegisterAsync();

        var (link, linkEmail, token) = await RequestResetLinkAsync(email);

        Assert.Equal("http://localhost:5173/reset-password", link.GetLeftPart(UriPartial.Path));
        Assert.Equal(email, linkEmail);
        Assert.False(string.IsNullOrWhiteSpace(token));
    }

    [Fact]
    public async Task ForgotPassword_UnknownEmail_ReturnsSameResponse_AndSendsNothing()
    {
        var known = await RegisterAsync();
        var unknown = $"nobody-{Guid.NewGuid():N}@example.com";
        var client = factory.CreateClient();

        var knownResponse = await client.PostAsJsonAsync(Url("/api/v1/auth/forgot-password"), new ForgotPasswordRequest(known));
        var unknownResponse = await client.PostAsJsonAsync(Url("/api/v1/auth/forgot-password"), new ForgotPasswordRequest(unknown));

        Assert.Equal(knownResponse.StatusCode, unknownResponse.StatusCode);
        Assert.Equal(await knownResponse.Content.ReadAsStringAsync(), await unknownResponse.Content.ReadAsStringAsync());
        await factory.Emails.WaitForEmailToAsync(known);
        Assert.DoesNotContain(factory.Emails.Sent, m => m.To == unknown);
    }

    [Fact]
    public async Task ResetPassword_WithEmailedToken_ChangesPassword_AndTokenIsSingleUse()
    {
        var email = await RegisterAsync();
        var (_, linkEmail, token) = await RequestResetLinkAsync(email);
        var client = factory.CreateClient();

        var reset = await client.PostAsJsonAsync(Url("/api/v1/auth/reset-password"), new ResetPasswordRequest(linkEmail, token, NewPassword));
        Assert.Equal(HttpStatusCode.OK, reset.StatusCode);

        Assert.Equal(HttpStatusCode.Unauthorized, (await LoginAsync(email, OriginalPassword)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await LoginAsync(email, NewPassword)).StatusCode);

        var reuse = await client.PostAsJsonAsync(Url("/api/v1/auth/reset-password"), new ResetPasswordRequest(linkEmail, token, "An0ther!Passw0rd"));
        Assert.Equal(HttpStatusCode.BadRequest, reuse.StatusCode);
    }

    [Theory]
    [InlineData("not-base64-!!!")]
    [InlineData("dGFtcGVyZWQtdG9rZW4")]
    public async Task ResetPassword_InvalidToken_Returns400OnTokenField(string token)
    {
        var email = await RegisterAsync();

        var response = await factory.CreateClient()
            .PostAsJsonAsync(Url("/api/v1/auth/reset-password"), new ResetPasswordRequest(email, token, NewPassword));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();
        Assert.True(problem!.Errors.ContainsKey("token"));
    }

    [Fact]
    public async Task ResetPassword_WeakPassword_Returns400OnPasswordField()
    {
        var email = await RegisterAsync();
        var (_, linkEmail, token) = await RequestResetLinkAsync(email);

        var response = await factory.CreateClient()
            .PostAsJsonAsync(Url("/api/v1/auth/reset-password"), new ResetPasswordRequest(linkEmail, token, "alllowercase"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();
        Assert.True(problem!.Errors.ContainsKey("password"));
    }

    [Fact]
    public async Task ResetPassword_UnlocksLockedOutAccount()
    {
        var email = await RegisterAsync();
        for (var i = 0; i < 5; i++)
        {
            await LoginAsync(email, "Wr0ng!Password");
        }

        // Locked out: even the correct password is rejected.
        Assert.Equal(HttpStatusCode.Unauthorized, (await LoginAsync(email, OriginalPassword)).StatusCode);

        var (_, linkEmail, token) = await RequestResetLinkAsync(email);
        var reset = await factory.CreateClient()
            .PostAsJsonAsync(Url("/api/v1/auth/reset-password"), new ResetPasswordRequest(linkEmail, token, NewPassword));
        Assert.Equal(HttpStatusCode.OK, reset.StatusCode);

        Assert.Equal(HttpStatusCode.OK, (await LoginAsync(email, NewPassword)).StatusCode);
    }

    [GeneratedRegex(@"http://localhost:5173/reset-password\?\S+")]
    private static partial Regex ResetLinkPattern();
}
