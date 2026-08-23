using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using Protocol.Api.Auth;

namespace Protocol.Api.Training;

/// <summary>
/// The training profile: what the user trains for and what they have available.
/// <para>
/// Every response is codes and data, never display text (root standard 3). JSON is camelCase,
/// which is the ASP.NET Core default the frontend depends on.
/// </para>
/// </summary>
public static class TrainingEndpoints
{
    public static IEndpointRouteBuilder MapTrainingEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/training").WithTags("Training").RequireAuthorization();

        group.MapGet("/profile", async (ClaimsPrincipal user, AppDbContext db, CancellationToken token) =>
            {
                var profile = await db.TrainingProfiles
                    .AsNoTracking()
                    .SingleOrDefaultAsync(p => p.UserId == UserIdOf(user), token);

                return profile is null
                    ? Results.NotFound(new ApiError(TrainingErrorCodes.ProfileNotFound))
                    : Results.Ok(TrainingProfileResponse.From(profile));
            })
            .WithName("GetTrainingProfile");

        group.MapPut("/profile", async (
                TrainingProfileRequest request,
                ClaimsPrincipal user,
                AppDbContext db,
                CancellationToken token) =>
            {
                if (!TrainingProfileRules.TryParseGoal(request.Goal, out var goal))
                {
                    // An unrecognised goal and a recognised-but-unsupported one are the same
                    // answer to the caller: this product does not programme for it yet.
                    return Results.BadRequest(new ApiError(TrainingErrorCodes.GoalNotSupported));
                }

                var error = TrainingProfileRules.Validate(
                    goal, request.DaysPerWeek, request.SessionDurationSeconds);

                if (error is not null)
                {
                    return Results.BadRequest(error);
                }

                var userId = UserIdOf(user);
                var profile = await db.TrainingProfiles.SingleOrDefaultAsync(p => p.UserId == userId, token);

                if (profile is null)
                {
                    profile = new TrainingProfile
                    {
                        Id = Guid.CreateVersion7(),
                        UserId = userId,
                        Goal = goal,
                        DaysPerWeek = request.DaysPerWeek,
                        SessionDurationSeconds = request.SessionDurationSeconds,
                    };
                    db.TrainingProfiles.Add(profile);
                }
                else
                {
                    // A profile is current state, not history: replacing it is correct here.
                    // What must never change is a week already generated from it, which is why
                    // ADR-003 snapshots these values onto the week rather than referencing them.
                    profile.Goal = goal;
                    profile.DaysPerWeek = request.DaysPerWeek;
                    profile.SessionDurationSeconds = request.SessionDurationSeconds;
                }

                await db.SaveChangesAsync(token);
                return Results.Ok(TrainingProfileResponse.From(profile));
            })
            .WithName("PutTrainingProfile");

        return app;
    }

    private static string UserIdOf(ClaimsPrincipal user) =>
        user.FindFirstValue(ClaimTypes.NameIdentifier)
        ?? throw new InvalidOperationException("Authenticated principal has no identifier.");
}

/// <summary>
/// A profile as the client sends it. The goal arrives as a string rather than an enum so that
/// an unrecognised value becomes <see cref="TrainingErrorCodes.GoalNotSupported"/> — a code the
/// frontend can translate — instead of a deserialization failure it cannot.
/// </summary>
public sealed record TrainingProfileRequest(string? Goal, int DaysPerWeek, int SessionDurationSeconds);

/// <summary>
/// A profile as the API returns it. Duration is seconds, always: minutes are a rendering
/// concern and are converted at the edge (root standard 4).
/// </summary>
public sealed record TrainingProfileResponse(string Goal, int DaysPerWeek, int SessionDurationSeconds)
{
    public static TrainingProfileResponse From(TrainingProfile profile) =>
        new(profile.Goal.ToString(), profile.DaysPerWeek, profile.SessionDurationSeconds);
}
