using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using Protocol.Api.Auth;
using Protocol.Api.Training;

namespace Protocol.Api.Hevy;

/// <summary>
/// Connecting a Hevy account (ADR-014).
/// <para>
/// Its own group rather than a corner of <c>/training</c>: Hevy is a boundary this system maps
/// across, not part of the domain it reasons about (root standard 17). The day the logging
/// surface is ours, what gets deleted is this folder.
/// </para>
/// </summary>
public static class HevyEndpoints
{
    public static IEndpointRouteBuilder MapHevyEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/hevy").WithTags("Hevy").RequireAuthorization();

        group.MapGet("/connection", async (ClaimsPrincipal user, AppDbContext db, CancellationToken token) =>
            {
                var connection = await db.HevyConnections
                    .AsNoTracking()
                    .SingleOrDefaultAsync(c => c.UserId == UserIdOf(user), token);

                // Having no connection is not an error: a user who never connected is the
                // ordinary case, and the frontend renders it as the empty state rather than as
                // a failure.
                return Results.Ok(new HevyConnectionResponse(
                    Connected: connection is not null,
                    ConnectedAt: connection?.ConnectedAt));
            })
            .WithName("GetHevyConnection");

        group.MapPut("/connection", async (
                HevyConnectionRequest request,
                ClaimsPrincipal user,
                AppDbContext db,
                IHevyClient hevy,
                HevyKeyProtector protector,
                CancellationToken token) =>
            {
                var apiKey = request.ApiKey?.Trim();

                if (string.IsNullOrEmpty(apiKey))
                {
                    return Results.BadRequest(new ApiError(HevyErrorCodes.HevyKeyInvalid));
                }

                // Validated before it is stored (ADR-014): a typo has to fail while the user is
                // still looking at the field, not silently at the first sync days later.
                var check = await hevy.CheckKeyAsync(apiKey, token);

                if (check == HevyKeyCheck.Invalid)
                {
                    return Results.BadRequest(new ApiError(HevyErrorCodes.HevyKeyInvalid));
                }

                if (check == HevyKeyCheck.Unreachable)
                {
                    // Deliberately not stored. A key we could not check is a key we cannot claim
                    // is connected, and a screen reading "connected" over an unverified key is
                    // worse than one reading "try again".
                    return Results.Json(
                        new ApiError(HevyErrorCodes.HevyUnreachable),
                        statusCode: StatusCodes.Status502BadGateway);
                }

                var userId = UserIdOf(user);
                var connection = await db.HevyConnections.SingleOrDefaultAsync(c => c.UserId == userId, token);

                // Truncated to the microsecond Postgres timestamptz actually stores, so that the
                // value read back equals the value returned here. .NET ticks are finer, and the
                // difference surfaced as a flaky assertion in M1.
                var now = DateTimeOffset.UtcNow;
                now = new DateTimeOffset(now.Ticks - (now.Ticks % 10), now.Offset);

                if (connection is null)
                {
                    db.HevyConnections.Add(new HevyConnection
                    {
                        Id = Guid.CreateVersion7(),
                        UserId = userId,
                        ProtectedApiKey = protector.Protect(apiKey),
                        ConnectedAt = now,
                    });
                }
                else
                {
                    connection.ProtectedApiKey = protector.Protect(apiKey);
                    connection.ConnectedAt = now;
                    // SyncCursor is deliberately left alone. Replacing a key is re-authenticating
                    // the same account, not starting a new history, and resetting the cursor here
                    // would re-import everything already read (ADR-018).
                }

                await db.SaveChangesAsync(token);

                return Results.Ok(new HevyConnectionResponse(Connected: true, ConnectedAt: now));
            })
            .WithName("SaveHevyConnection");

        group.MapPost("/weeks/{weekId:guid}/push", async (
                Guid weekId,
                HevyPushRequest? request,
                ClaimsPrincipal user,
                HevyWeekPusher pusher,
                CancellationToken token) =>
            {
                // Explicit, never automatic. Pushing writes into a surface this system cannot
                // clean up afterwards, so it is always something the user asked for.
                var result = await pusher.PushAsync(weekId, UserIdOf(user), request?.Locale, token);

                return result.Outcome switch
                {
                    PushOutcome.Ok => Results.Ok(new HevyPushResponse(result.FolderId, result.Sessions ?? [])),
                    PushOutcome.WeekNotFound =>
                        Results.NotFound(new ApiError(TrainingErrorCodes.WeekNotFound)),
                    PushOutcome.NotConnected =>
                        Results.BadRequest(new ApiError(HevyErrorCodes.HevyNotConnected)),
                    PushOutcome.AlreadyTrainedFrom =>
                        Results.Conflict(new ApiError(HevyErrorCodes.WeekAlreadyTrainedFrom)),
                    PushOutcome.ExerciseNotMappable =>
                        Results.BadRequest(new ApiError(HevyErrorCodes.ExerciseNotMappable)),
                    PushOutcome.RoutineMissing =>
                        Results.Conflict(new ApiError(HevyErrorCodes.PushedRoutineMissing)),
                    PushOutcome.RateLimited => Results.Json(
                        new ApiError(HevyErrorCodes.HevyRateLimited),
                        statusCode: StatusCodes.Status503ServiceUnavailable),
                    _ => Results.Json(
                        new ApiError(HevyErrorCodes.HevyUnreachable),
                        statusCode: StatusCodes.Status502BadGateway),
                };
            })
            .WithName("PushWeekToHevy");

        return app;
    }

    private static string UserIdOf(ClaimsPrincipal user) =>
        user.FindFirstValue(ClaimTypes.NameIdentifier)
        ?? throw new InvalidOperationException("An authorised request has no subject claim.");
}

/// <summary>The key, on its way in. This is the only direction it ever travels.</summary>
public sealed record HevyConnectionRequest(string? ApiKey);

/// <summary>
/// What a push needs beyond the week itself: the user's language.
/// <para>
/// Carried explicitly rather than sniffed from a header, because the routine note is the one
/// piece of display text this backend composes (ADR-016) and the locale that decides it should
/// be as visible as the text is.
/// </para>
/// </summary>
public sealed record HevyPushRequest(string? Locale);

/// <summary>Where the week now lives in Hevy. Identifiers, so the caller can confirm what happened.</summary>
public sealed record HevyPushResponse(long? FolderId, IReadOnlyList<PushedSession> Sessions);

/// <summary>
/// Whether an account is connected — and nothing else.
/// <para>
/// There is deliberately no field for the key, not even a masked one. A masked key is still key
/// material on the wire and in a browser cache, and ADR-014 decided the API never returns it.
/// The absence is the enforcement.
/// </para>
/// </summary>
public sealed record HevyConnectionResponse(bool Connected, DateTimeOffset? ConnectedAt);
