using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using RenderVN.CoreApi.Data;
using RenderVN.CoreApi.Domain;

namespace RenderVN.CoreApi.Endpoints;

public static class AuthEndpoints
{
    public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost("/api/auth/register", RegisterAsync)
            .AllowAnonymous();
        endpoints.MapPost("/api/auth/login", LoginAsync)
            .AllowAnonymous();
        endpoints.MapGet("/api/me", GetMeAsync)
            .RequireAuthorization();
        endpoints.MapPost("/api/auth/logout", LogoutAsync)
            .RequireAuthorization();

        return endpoints;
    }

    private static async Task<IResult> RegisterAsync(
        RegisterRequest request,
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        AppDbContext db,
        CreditLedger ledger,
        CancellationToken cancellationToken)
    {
        var existingUser = await userManager.FindByEmailAsync(request.Email);
        if (existingUser is not null)
        {
            return Results.Conflict(new ApiError(
                "duplicate_email",
                "An account with this email already exists."));
        }

        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        var now = DateTimeOffset.UtcNow;
        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            UserName = request.Email,
            Email = request.Email,
            CreatedAt = now,
            UpdatedAt = now
        };

        var createResult = await userManager.CreateAsync(user, request.Password);
        if (!createResult.Succeeded)
        {
            var duplicateEmail = createResult.Errors.Any(error =>
                error.Code is "DuplicateEmail" or "DuplicateUserName");
            if (duplicateEmail)
            {
                return Results.Conflict(new ApiError(
                    "duplicate_email",
                    "An account with this email already exists."));
            }

            return Results.BadRequest(new ApiError(
                "registration_failed",
                string.Join(" ", createResult.Errors.Select(error => error.Description))));
        }

        var wallet = new CreditWallet
        {
            UserId = user.Id,
            User = user,
            CreatedAt = now,
            UpdatedAt = now
        };
        db.CreditWallets.Add(wallet);
        await db.SaveChangesAsync(cancellationToken);

        await ledger.GrantAsync(
            wallet.Id,
            20,
            $"registration:{user.Id:N}",
            cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        await signInManager.SignInAsync(user, isPersistent: false);
        return Results.Created("/api/me", new AuthResponse(
            user.Email!,
            wallet.AvailableCredits,
            wallet.ReservedCredits));
    }

    private static async Task<IResult> GetMeAsync(
        HttpContext httpContext,
        UserManager<ApplicationUser> userManager,
        AppDbContext db,
        CancellationToken cancellationToken)
    {
        var user = await userManager.GetUserAsync(httpContext.User);
        if (user is null)
        {
            return Results.Unauthorized();
        }

        var wallet = await db.CreditWallets
            .AsNoTracking()
            .SingleAsync(item => item.UserId == user.Id, cancellationToken);
        return Results.Ok(new AuthResponse(
            user.Email!,
            wallet.AvailableCredits,
            wallet.ReservedCredits));
    }

    private static async Task<IResult> LogoutAsync(SignInManager<ApplicationUser> signInManager)
    {
        await signInManager.SignOutAsync();
        return Results.NoContent();
    }

    private static async Task<IResult> LoginAsync(
        RegisterRequest request,
        SignInManager<ApplicationUser> signInManager)
    {
        var result = await signInManager.PasswordSignInAsync(
            request.Email,
            request.Password,
            isPersistent: false,
            lockoutOnFailure: true);
        if (!result.Succeeded)
        {
            return Results.Json(
                new ApiError("invalid_credentials", "Email or password is incorrect."),
                statusCode: StatusCodes.Status401Unauthorized);
        }

        return Results.NoContent();
    }

    private sealed record RegisterRequest(string Email, string Password);
    private sealed record AuthResponse(string Email, int AvailableCredits, int ReservedCredits);
    private sealed record ApiError(string Code, string Message);
}
