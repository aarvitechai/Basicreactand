using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc;
using RedStarVibe.Api.Contracts;
using static RedStarVibe.IntegrationTests.TestAccounts;

namespace RedStarVibe.IntegrationTests.Auth;

public sealed class EmailConfirmationTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    [Fact]
    public async Task Register_SendsConfirmationEmail_WithLinkToFrontend()
    {
        var email = await RegisterAsync(factory);

        var message = await factory.Emails.WaitForEmailToAsync(email, "Confirm");

        Assert.Contains("http://localhost:5173/confirm-email?userId=", message.TextBody, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Login_BeforeConfirming_CorrectPassword_Returns403WithCode()
    {
        var email = await RegisterAsync(factory);

        var response = await factory.CreateClient().PostAsJsonAsync(Url("/api/v1/auth/login"), new LoginRequest(email, Password));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        Assert.Equal("email_not_confirmed", problem!.Extensions["code"]?.ToString());
    }

    [Fact]
    public async Task Login_BeforeConfirming_WrongPassword_Returns401_WithoutRevealingState()
    {
        var email = await RegisterAsync(factory);

        var response = await factory.CreateClient()
            .PostAsJsonAsync(Url("/api/v1/auth/login"), new LoginRequest(email, "Wr0ng!Password"));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task ConfirmEmail_ThenLogin_Succeeds_AndConfirmIsIdempotent()
    {
        var email = await RegisterAsync(factory);
        var (userId, token) = await GetConfirmationLinkAsync(factory, email);
        var client = factory.CreateClient();

        var confirm = await client.PostAsJsonAsync(Url("/api/v1/auth/confirm-email"), new ConfirmEmailRequest(userId, token));
        Assert.Equal(HttpStatusCode.OK, confirm.StatusCode);

        var again = await client.PostAsJsonAsync(Url("/api/v1/auth/confirm-email"), new ConfirmEmailRequest(userId, token));
        Assert.Equal(HttpStatusCode.OK, again.StatusCode);

        var login = await client.PostAsJsonAsync(Url("/api/v1/auth/login"), new LoginRequest(email, Password));
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
    }

    [Theory]
    [InlineData("not-base64-!!!")]
    [InlineData("dGFtcGVyZWQtdG9rZW4")]
    public async Task ConfirmEmail_InvalidToken_Returns400OnTokenField(string token)
    {
        var email = await RegisterAsync(factory);
        var (userId, _) = await GetConfirmationLinkAsync(factory, email);

        var response = await factory.CreateClient()
            .PostAsJsonAsync(Url("/api/v1/auth/confirm-email"), new ConfirmEmailRequest(userId, token));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();
        Assert.True(problem!.Errors.ContainsKey("token"));
    }

    [Fact]
    public async Task ResendConfirmation_SendsNewLink_OnlyForUnconfirmedAccounts()
    {
        var unconfirmed = await RegisterAsync(factory);
        var (_, confirmed) = await RegisterConfirmedAndSignInAsync(factory);
        var unknown = UniqueEmail("nobody");
        var client = factory.CreateClient();

        var bodies = new List<string>();
        foreach (var email in new[] { unconfirmed, confirmed, unknown })
        {
            var r = await client.PostAsJsonAsync(Url("/api/v1/auth/resend-confirmation"), new ResendConfirmationRequest(email));
            Assert.Equal(HttpStatusCode.Accepted, r.StatusCode);
            bodies.Add(await r.Content.ReadAsStringAsync());
        }

        Assert.Single(bodies.Distinct()); // identical response regardless of account state
        await factory.Emails.WaitForEmailToAsync(unconfirmed, "Confirm", count: 2);
        Assert.Single(factory.Emails.Sent, m => m.To == confirmed && m.Subject.Contains("Confirm", StringComparison.Ordinal));
        Assert.DoesNotContain(factory.Emails.Sent, m => m.To == unknown);
    }
}
