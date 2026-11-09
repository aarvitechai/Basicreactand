using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc;
using RedStarVibe.Api.Contracts;

namespace RedStarVibe.IntegrationTests.Auth;

public sealed class AuthEndpointsTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    private const string ValidPassword = "Str0ng!Passw0rd";

    private static string UniqueEmail() => $"user-{Guid.NewGuid():N}@example.com";

    private static Uri Url(string path) => new(path, UriKind.Relative);

    [Fact]
    public async Task Register_CreatesUser_SignsIn_AndMeReturnsUser()
    {
        var client = factory.CreateClient();
        var email = UniqueEmail();

        var register = await client.PostAsJsonAsync(Url("/api/v1/auth/register"), new RegisterRequest(email, ValidPassword, "Jane Doe"));

        Assert.Equal(HttpStatusCode.Created, register.StatusCode);
        var created = await register.Content.ReadFromJsonAsync<UserResponse>();
        Assert.Equal(email, created!.Email);
        Assert.Equal("Jane Doe", created.DisplayName);

        var cookie = Assert.Single(register.Headers.GetValues("Set-Cookie"), c => c.StartsWith("rsv.auth=", StringComparison.Ordinal));
        Assert.Contains("httponly", cookie, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("samesite=strict", cookie, StringComparison.OrdinalIgnoreCase);

        var me = await client.GetFromJsonAsync<UserResponse>(Url("/api/v1/auth/me"));
        Assert.Equal(created.Id, me!.Id);
    }

    [Fact]
    public async Task Register_DuplicateEmail_Returns409()
    {
        var client = factory.CreateClient();
        var email = UniqueEmail();
        await client.PostAsJsonAsync(Url("/api/v1/auth/register"), new RegisterRequest(email, ValidPassword, "First"));

        var second = await factory.CreateClient()
            .PostAsJsonAsync(Url("/api/v1/auth/register"), new RegisterRequest(email.ToUpperInvariant(), ValidPassword, "Second"));

        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
    }

    [Theory]
    [InlineData("not-an-email", ValidPassword, "Jane", "Email")]
    [InlineData("a@example.com", "short", "Jane", "Password")]
    [InlineData("a@example.com", ValidPassword, "J", "DisplayName")]
    public async Task Register_InvalidInput_Returns400WithFieldError(string email, string password, string displayName, string field)
    {
        var response = await factory.CreateClient()
            .PostAsJsonAsync(Url("/api/v1/auth/register"), new RegisterRequest(email, password, displayName));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();
        Assert.Contains(problem!.Errors.Keys, k => k.Equals(field, StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task Register_WeakPassword_ReturnsPasswordPolicyErrors()
    {
        var response = await factory.CreateClient()
            .PostAsJsonAsync(Url("/api/v1/auth/register"), new RegisterRequest(UniqueEmail(), "alllowercase", "Jane"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();
        Assert.True(problem!.Errors.ContainsKey("password"));
    }

    [Fact]
    public async Task Login_WithValidCredentials_Succeeds_AndWrongPassword_Returns401()
    {
        var email = UniqueEmail();
        await factory.CreateClient().PostAsJsonAsync(Url("/api/v1/auth/register"), new RegisterRequest(email, ValidPassword, "Jane"));

        var client = factory.CreateClient();
        var bad = await client.PostAsJsonAsync(Url("/api/v1/auth/login"), new LoginRequest(email, "Wr0ng!Password"));
        Assert.Equal(HttpStatusCode.Unauthorized, bad.StatusCode);

        var good = await client.PostAsJsonAsync(Url("/api/v1/auth/login"), new LoginRequest(email, ValidPassword));
        Assert.Equal(HttpStatusCode.OK, good.StatusCode);
    }

    [Fact]
    public async Task Login_UnknownEmail_Returns401WithSameMessageAsWrongPassword()
    {
        var response = await factory.CreateClient()
            .PostAsJsonAsync(Url("/api/v1/auth/login"), new LoginRequest(UniqueEmail(), ValidPassword));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        Assert.Equal("Invalid email or password.", problem!.Title);
    }

    [Fact]
    public async Task Me_WithoutCookie_Returns401_NotRedirect()
    {
        var response = await factory.CreateClient().GetAsync(Url("/api/v1/auth/me"));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Logout_ClearsSession()
    {
        var client = factory.CreateClient();
        await client.PostAsJsonAsync(Url("/api/v1/auth/register"), new RegisterRequest(UniqueEmail(), ValidPassword, "Jane"));

        var logout = await client.PostAsync(Url("/api/v1/auth/logout"), null);
        Assert.Equal(HttpStatusCode.NoContent, logout.StatusCode);

        var me = await client.GetAsync(Url("/api/v1/auth/me"));
        Assert.Equal(HttpStatusCode.Unauthorized, me.StatusCode);
    }
}
