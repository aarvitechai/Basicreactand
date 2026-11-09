using System.Net;
using System.Net.Http.Json;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc;
using RedStarVibe.Api.Contracts;
using static RedStarVibe.IntegrationTests.TestAccounts;

namespace RedStarVibe.IntegrationTests.Auth;

public sealed partial class PhoneVerificationTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    private static string UniquePhone() => "+1555" + Random.Shared.Next(1000000, 9999999).ToString(System.Globalization.CultureInfo.InvariantCulture);

    private string CodeSentTo(string phone) => SixDigits().Match(factory.Sms.LastTo(phone).Body).Value;

    [Fact]
    public async Task SendCode_RequiresSignIn()
    {
        var response = await factory.CreateClient()
            .PostAsJsonAsync(Url("/api/v1/account/phone/send-code"), new SendPhoneCodeRequest(UniquePhone()));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task SendCode_ThenVerify_MarksPhoneConfirmed_AndKeepsSession()
    {
        var (client, _) = await RegisterConfirmedAndSignInAsync(factory);
        var phone = UniquePhone();

        var send = await client.PostAsJsonAsync(Url("/api/v1/account/phone/send-code"), new SendPhoneCodeRequest(phone));
        Assert.Equal(HttpStatusCode.Accepted, send.StatusCode);

        var verify = await client.PostAsJsonAsync(Url("/api/v1/account/phone/verify"), new VerifyPhoneRequest(phone, CodeSentTo(phone)));
        Assert.Equal(HttpStatusCode.OK, verify.StatusCode);
        var user = await verify.Content.ReadFromJsonAsync<UserResponse>();
        Assert.Equal(phone, user!.PhoneNumber);
        Assert.True(user.PhoneNumberConfirmed);

        // Verifying rotates the security stamp; the session must still be valid afterwards.
        var me = await client.GetFromJsonAsync<UserResponse>(Url("/api/v1/auth/me"));
        Assert.True(me!.PhoneNumberConfirmed);
    }

    [Fact]
    public async Task Verify_WrongCode_Returns400OnCodeField()
    {
        var (client, _) = await RegisterConfirmedAndSignInAsync(factory);
        var phone = UniquePhone();
        await client.PostAsJsonAsync(Url("/api/v1/account/phone/send-code"), new SendPhoneCodeRequest(phone));
        var wrong = CodeSentTo(phone) == "000000" ? "111111" : "000000";

        var response = await client.PostAsJsonAsync(Url("/api/v1/account/phone/verify"), new VerifyPhoneRequest(phone, wrong));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();
        Assert.True(problem!.Errors.ContainsKey("code"));
    }

    [Fact]
    public async Task Verify_CodeForADifferentNumber_IsRejected()
    {
        var (client, _) = await RegisterConfirmedAndSignInAsync(factory);
        var phone = UniquePhone();
        await client.PostAsJsonAsync(Url("/api/v1/account/phone/send-code"), new SendPhoneCodeRequest(phone));

        var response = await client.PostAsJsonAsync(
            Url("/api/v1/account/phone/verify"), new VerifyPhoneRequest(UniquePhone(), CodeSentTo(phone)));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task SendCode_InvalidNumber_Returns400OnPhoneField()
    {
        var (client, _) = await RegisterConfirmedAndSignInAsync(factory);

        var response = await client.PostAsJsonAsync(Url("/api/v1/account/phone/send-code"), new SendPhoneCodeRequest("98765"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();
        Assert.True(problem!.Errors.ContainsKey("phoneNumber"));
    }

    [GeneratedRegex(@"\b\d{6}\b")]
    private static partial Regex SixDigits();
}
