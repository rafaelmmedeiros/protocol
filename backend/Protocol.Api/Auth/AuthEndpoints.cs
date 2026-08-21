using System.Security.Claims;
using Microsoft.AspNetCore.Identity;

namespace Protocol.Api.Auth;

/// <summary>
/// Endpoints that complete what <c>MapIdentityApi</c> leaves out: reading the current session
/// and ending it.
/// </summary>
public static class AuthEndpoints
{
    public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/auth").WithTags("Auth");

        group.MapGet("/me", (ClaimsPrincipal user) => Results.Ok(CurrentUser.From(user)))
            .RequireAuthorization()
            .WithName("GetCurrentUser");

        group.MapPost("/logout", async (SignInManager<AppUser> signInManager) =>
            {
                await signInManager.SignOutAsync();
                return Results.NoContent();
            })
            .RequireAuthorization()
            .WithName("Logout");

        return app;
    }
}

/// <summary>The authenticated caller, as the frontend needs it.</summary>
public sealed record CurrentUser(string Id, string Email)
{
    /// <summary>
    /// Reads the session's principal. Identity issues the email as <c>Name</c> and only
    /// sometimes as an explicit email claim, so both are consulted.
    /// </summary>
    public static CurrentUser From(ClaimsPrincipal principal)
    {
        var id = principal.FindFirstValue(ClaimTypes.NameIdentifier)
                 ?? throw new InvalidOperationException("Authenticated principal has no identifier.");
        var email = principal.FindFirstValue(ClaimTypes.Email)
                    ?? principal.FindFirstValue(ClaimTypes.Name)
                    ?? throw new InvalidOperationException("Authenticated principal has no email.");
        return new CurrentUser(id, email);
    }
}
