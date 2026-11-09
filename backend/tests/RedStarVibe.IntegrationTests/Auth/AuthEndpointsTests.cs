using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc;
using RedStarVibe.Api.Contracts;
using static RedStarVibe.IntegrationTests.TestAccounts;

namespace RedStarVibe.IntegrationTests.Auth;

public sealed class AuthEndpointsTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    [Fact]
    public async Task Register_CreatesUnconfirmedUser_WithoutSigningIn()
    {
        var client = factory.CreateClient();
        var email = UniqueEmail();

        var register = await client.PostAsJsonAsync(Url("/api/v1/auth/register"), new RegisterRequest(email, Password, "Jane Doe"));

        Assert.Equal(HttpStatusCode.Created, register.StatusCode);
        var created = await register.Content.ReadFromJsonAsync<UserResponse>();
        Assert.Equal(email, created!.Email);
        Assert.False(created.EmailConfirmed);
        Assert.False(register.Headers.Contains("Set-Cookie"));
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync(Url("/api/v1/auth/me"))).StatusCode);
    }

    [Fact]
    public async Task ConfirmedUser_CanSignIn_WithSecureCookie_AndMeReturnsUser()
    {
        var email = await RegisterAsync(factory);
        var (userId, token) = await GetConfirmationLinkAsync(factory, email);
        var client = factory.CreateClient();
        await client.PostAsJsonAsync(Url("/api/v1/auth/confirm-email"), new ConfirmEmailRequest(userId, token));

        var login = await client.PostAsJsonAsync(Url("/api/v1/auth/login"), new LoginRequest(email, Password));

        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
        var cookie = Assert.Single(login.Headers.GetValues("Set-Cookie"), c => c.StartsWith("rsv.auth=", StringComparison.Ordinal));
        Assert.Contains("httponly", cookie, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("samesite=strict", cookie, StringComparison.OrdinalIgnoreCase);

        var me = await client.GetFromJsonAsync<UserResponse>(Url("/api/v1/auth/me"));
        Assert.Equal(userId, me!.Id);
        Assert.True(me.EmailConfirmed);
    }

    [Fact]
    public async Task Register_DuplicateEmail_Returns409()
    {
        var email = await RegisterAsync(factory);

        var second = await factory.CreateClient()
            .PostAsJsonAsync(Url("/api/v1/auth/register"), new RegisterRequest(email.ToUpperInvariant(), Password, "Second"));

        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
    }

    [Theory]
    [InlineData("not-an-email", Password, "Jane", null, "Email")]
    [InlineData("a@example.com", "short", "Jane", null, "Password")]
    [InlineData("a@example.com", Password, "J", null, "DisplayName")]
    [InlineData("phone-bad@example.com", Password, "Jane", "12345", "phoneNumber")]
    public async Task Register_InvalidInput_Returns400WithFieldError(
        string email, string password, string displayName, string? phone, string field)
    {
        var response = await factory.CreateClient()
            .PostAsJsonAsync(Url("/api/v1/auth/register"), new RegisterRequest(email, password, displayName, phone));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();
        Assert.Contains(problem!.Errors.Keys, k => k.Equals(field, StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task Register_WithPhone_StoresNormalizedUnverifiedNumber()
    {
        var response = await factory.CreateClient().PostAsJsonAsync(
            Url("/api/v1/auth/register"), new RegisterRequest(UniqueEmail(), Password, "Jane", "+91 98765-43210"));

        var user = await response.Content.ReadFromJsonAsync<UserResponse>();
        Assert.Equal("+919876543210", user!.PhoneNumber);
        Assert.False(user.PhoneNumberConfirmed);
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
    public async Task Login_WrongPassword_Returns401()
    {
        var (_, email) = await RegisterConfirmedAndSignInAsync(factory);

        var bad = await factory.CreateClient().PostAsJsonAsync(Url("/api/v1/auth/login"), new LoginRequest(email, "Wr0ng!Password"));

        Assert.Equal(HttpStatusCode.Unauthorized, bad.StatusCode);
    }

    [Fact]
    public async Task Login_UnknownEmail_Returns401WithGenericMessage()
    {
        var response = await factory.CreateClient()
            .PostAsJsonAsync(Url("/api/v1/auth/login"), new LoginRequest(UniqueEmail(), Password));

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
        var (client, _) = await RegisterConfirmedAndSignInAsync(factory);

        var logout = await client.PostAsync(Url("/api/v1/auth/logout"), null);
        Assert.Equal(HttpStatusCode.NoContent, logout.StatusCode);

        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync(Url("/api/v1/auth/me"))).StatusCode);
    }
}
