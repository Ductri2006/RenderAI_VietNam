using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using RenderVN.CoreApi.Data;
using RenderVN.CoreApi.Domain;

namespace RenderVN.CoreApi.Tests.Integration;

public sealed class AuthEndpointTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    [Fact]
    public async Task RegistrationCreatesWalletAndExactlyTwentyCreditGrant()
    {
        var client = factory.CreateAuthenticatedClient();
        var email = $"register-{Guid.NewGuid():N}@example.com";

        using var response = await client.PostAsJsonAsync("/api/auth/register", new
        {
            email,
            password = "StrongPass123!"
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var user = await db.Users.SingleAsync(item => item.Email == email);
        var wallet = await db.CreditWallets.SingleAsync(item => item.UserId == user.Id);
        Assert.Equal(20, wallet.AvailableCredits);
        Assert.Equal(0, wallet.ReservedCredits);

        var transaction = await db.CreditTransactions.SingleAsync(
            item => item.WalletId == wallet.Id);
        Assert.Equal(CreditTransactionType.Grant, transaction.Type);
        Assert.Equal(20, transaction.AvailableDelta);
        Assert.Equal(0, transaction.ReservedDelta);
    }

    [Fact]
    public async Task DuplicateEmailIsRejectedWithStableConflictResponse()
    {
        var firstClient = factory.CreateAuthenticatedClient();
        var secondClient = factory.CreateAuthenticatedClient();
        var email = $"duplicate-{Guid.NewGuid():N}@example.com";
        var request = new
        {
            email,
            password = "StrongPass123!"
        };

        using var first = await firstClient.PostAsJsonAsync("/api/auth/register", request);
        using var duplicate = await secondClient.PostAsJsonAsync("/api/auth/register", request);

        Assert.Equal(HttpStatusCode.Created, first.StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, duplicate.StatusCode);
        var error = await duplicate.Content.ReadFromJsonAsync<ApiError>();
        Assert.NotNull(error);
        Assert.Equal("duplicate_email", error.Code);
    }

    [Fact]
    public async Task MeReturnsSignedInUserAndWalletWithoutInternalIds()
    {
        var client = factory.CreateAuthenticatedClient();
        var email = $"me-{Guid.NewGuid():N}@example.com";
        using var registration = await client.PostAsJsonAsync("/api/auth/register", new
        {
            email,
            password = "StrongPass123!"
        });
        Assert.Equal(HttpStatusCode.Created, registration.StatusCode);

        using var response = await client.GetAsync("/api/me");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<MeResponse>();
        Assert.NotNull(payload);
        Assert.Equal(email, payload.Email);
        Assert.Equal(20, payload.AvailableCredits);
        Assert.Equal(0, payload.ReservedCredits);

        var json = await response.Content.ReadAsStringAsync();
        Assert.DoesNotContain("userId", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("password", json, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task LogoutClearsAuthenticationCookie()
    {
        var client = factory.CreateAuthenticatedClient();
        using var registration = await client.PostAsJsonAsync("/api/auth/register", new
        {
            email = $"logout-{Guid.NewGuid():N}@example.com",
            password = "StrongPass123!"
        });
        Assert.Equal(HttpStatusCode.Created, registration.StatusCode);

        using var logout = await client.PostAsync("/api/auth/logout", content: null);
        using var me = await client.GetAsync("/api/me");

        Assert.Equal(HttpStatusCode.NoContent, logout.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, me.StatusCode);
    }

    [Fact]
    public async Task LoginIssuesCookieForValidCredentials()
    {
        var registrationClient = factory.CreateAuthenticatedClient();
        var loginClient = factory.CreateAuthenticatedClient();
        var email = $"login-{Guid.NewGuid():N}@example.com";
        var credentials = new
        {
            email,
            password = "StrongPass123!"
        };
        using var registration = await registrationClient.PostAsJsonAsync(
            "/api/auth/register",
            credentials);
        Assert.Equal(HttpStatusCode.Created, registration.StatusCode);

        using var login = await loginClient.PostAsJsonAsync("/api/auth/login", credentials);
        using var me = await loginClient.GetAsync("/api/me");

        Assert.Equal(HttpStatusCode.NoContent, login.StatusCode);
        Assert.Equal(HttpStatusCode.OK, me.StatusCode);
        var payload = await me.Content.ReadFromJsonAsync<MeResponse>();
        Assert.NotNull(payload);
        Assert.Equal(email, payload.Email);
    }

    [Fact]
    public async Task DevelopmentHttpLoginCookieIsHttpOnlyAndLaxWithoutSecureFlag()
    {
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            BaseAddress = new Uri("http://localhost"),
            HandleCookies = false
        });
        var email = $"http-cookie-{Guid.NewGuid():N}@example.com";
        var credentials = new
        {
            email,
            password = "StrongPass123!"
        };
        using var registration = await client.PostAsJsonAsync("/api/auth/register", credentials);
        Assert.Equal(HttpStatusCode.Created, registration.StatusCode);

        using var login = await client.PostAsJsonAsync("/api/auth/login", credentials);

        Assert.Equal(HttpStatusCode.NoContent, login.StatusCode);
        var cookie = Assert.Single(login.Headers.GetValues("Set-Cookie"));
        Assert.Contains("; httponly", cookie, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("; samesite=lax", cookie, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("; secure", cookie, StringComparison.OrdinalIgnoreCase);
    }

    private sealed record ApiError(string Code, string Message);
    private sealed record MeResponse(string Email, int AvailableCredits, int ReservedCredits);
}
