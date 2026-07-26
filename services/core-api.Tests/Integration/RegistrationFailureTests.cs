using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using RenderVN.CoreApi.Data;
using RenderVN.CoreApi.Domain;

namespace RenderVN.CoreApi.Tests.Integration;

public sealed class RegistrationFailureTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    [Fact]
    public async Task FailedGrantRollsBackRegistrationAndReturnsStableError()
    {
        using var failingFactory = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                services.RemoveAll<ICreditGrant>();
                services.AddScoped<ICreditGrant, FailingCreditGrant>();
            });
        });
        var client = failingFactory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            BaseAddress = new Uri("https://localhost")
        });
        var email = $"grant-failure-{Guid.NewGuid():N}@example.com";

        using var response = await client.PostAsJsonAsync("/api/auth/register", new
        {
            email,
            password = "StrongPass123!"
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var error = await response.Content.ReadFromJsonAsync<ApiError>();
        Assert.NotNull(error);
        Assert.Equal("grant_failed", error.Code);

        using var scope = failingFactory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        Assert.False(await db.Users.AnyAsync(user => user.Email == email));
        Assert.Empty(await db.CreditWallets.ToListAsync());
        Assert.Empty(await db.CreditTransactions.ToListAsync());
    }

    private sealed class FailingCreditGrant : ICreditGrant
    {
        public Task<CreditLedgerResult> GrantAsync(
            Guid walletId,
            int credits,
            string idempotencyKey,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(CreditLedgerResult.Failure("forced_failure"));
        }
    }

    private sealed record ApiError(string Code, string Message);
}
