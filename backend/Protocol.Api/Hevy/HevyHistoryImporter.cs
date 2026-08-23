using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Protocol.Api.Auth;
using Protocol.Api.Training;

namespace Protocol.Api.Hevy;

/// <summary>
/// Pulls what changed in Hevy since the last sync, and appends it (ADR-018).
/// <para>
/// Nothing here updates a row and nothing deletes one. An upstream edit appends a new version, an
/// upstream deletion appends a tombstone, and reads take the highest version — which is root
/// standard 7 applied literally rather than approximately.
/// </para>
/// </summary>
public sealed class HevyHistoryImporter(
    AppDbContext db,
    IHevyClient hevy,
    HevyKeyProtector protector,
    ILogger<HevyHistoryImporter> logger)
{
    /// <summary>Hevy's own cap on the events feed. Not ours to choose.</summary>
    private const int PageSize = 10;

    /// <summary>
    /// A stop on a runaway feed. High enough that no real history reaches it — at ten per page
    /// that is thirty thousand events — and low enough that a paging bug on their side ends as a
    /// partial sync rather than as an unbounded loop.
    /// </summary>
    private const int MaxPages = 3_000;

    public async Task<SyncResult> SyncAsync(string userId, CancellationToken token)
    {
        var connection = await db.HevyConnections.SingleOrDefaultAsync(c => c.UserId == userId, token);

        if (connection is null)
        {
            return new SyncResult(SyncOutcome.NotConnected);
        }

        var apiKey = protector.Unprotect(connection.ProtectedApiKey);

        // The first sync is a backfill. The feed's own default epoch is the honest starting
        // point: we have read nothing, so nothing is too old to matter.
        var since = connection.SyncCursor ?? DateTimeOffset.UnixEpoch;

        var catalogue = await db.Exercises
            .AsNoTracking()
            .ToDictionaryAsync(exercise => exercise.ExternalTemplateId, exercise => exercise.Id, token);

        var imported = 0;
        var tombstoned = 0;
        var unmapped = 0;
        var page = 1;

        while (page <= MaxPages)
        {
            var fetched = await hevy.ListWorkoutEventsAsync(apiKey, since, page, PageSize, token);

            if (!fetched.Ok)
            {
                // A sync that gives up is a partial success, never a restart: everything already
                // committed stays, and the cursor already advanced past it (ADR-021).
                return new SyncResult(
                    fetched.Outcome == HevyWriteOutcome.RateLimited
                        ? SyncOutcome.RateLimited
                        : SyncOutcome.Unreachable,
                    imported,
                    tombstoned,
                    unmapped);
            }

            var events = fetched.Value?.Events ?? [];

            foreach (var change in events)
            {
                var applied = await ApplyAsync(change, userId, catalogue, token);

                imported += applied.Imported;
                tombstoned += applied.Tombstoned;
                unmapped += applied.Unmapped;

                // Written out rather than `at > connection.SyncCursor`: a lifted comparison against
                // a null operand is false, so the null cursor -- the first sync, every time --
                // would never advance, and every sync would re-read the whole history.
                if (applied.At is { } at
                    && (connection.SyncCursor is null || at > connection.SyncCursor.Value))
                {
                    connection.SyncCursor = at;
                }
            }

            // Committed a page at a time, cursor included. An interrupted sync resumes from where
            // it stopped rather than starting over -- which is also the behaviour least likely to
            // hit a rate limit again on the retry.
            await db.SaveChangesAsync(token);

            var pageCount = fetched.Value?.PageCount ?? page;

            if (events.Count == 0 || page >= pageCount)
            {
                break;
            }

            page++;
        }

        return new SyncResult(SyncOutcome.Ok, imported, tombstoned, unmapped);
    }

    private async Task<(int Imported, int Tombstoned, int Unmapped, DateTimeOffset? At)> ApplyAsync(
        HevyWorkoutEvent change,
        string userId,
        IReadOnlyDictionary<string, Guid> catalogue,
        CancellationToken token)
    {
        if (change.Type == "deleted" && change.Id is { } deletedId)
        {
            return await TombstoneAsync(deletedId, userId, change.DeletedAt, token);
        }

        if (change.Workout is not { } workout)
        {
            return (0, 0, 0, null);
        }

        var updatedAt = workout.UpdatedAt.ToUniversalTime();

        // The feed is asked for everything at or after a cursor, so the boundary event arrives
        // again on the next sync by design. Recognising it is what makes a re-run add nothing.
        var known = await db.PerformedWorkouts.AnyAsync(
            row => row.UserId == userId
                && row.ExternalWorkoutId == workout.Id
                && row.ExternallyUpdatedAt == updatedAt,
            token);

        if (known)
        {
            return (0, 0, 0, updatedAt);
        }

        var version = await NextVersionAsync(workout.Id, userId, token);

        // Stored before it is mapped, and this order is the point: a payload carrying something
        // we do not model is still captured, so the failure costs a report rather than the data.
        var snapshot = new HevyWorkoutSnapshot
        {
            Id = Guid.CreateVersion7(),
            UserId = userId,
            ExternalWorkoutId = workout.Id,
            Version = version,
            ExternallyUpdatedAt = updatedAt,
            RawJson = JsonSerializer.Serialize(workout),
            FetchedAt = DateTimeOffset.UtcNow,
        };

        try
        {
            var performed = HevyInboundMapper.ToPerformedWorkout(
                workout,
                userId,
                templateId => catalogue.TryGetValue(templateId, out var id) ? id : null,
                version);

            db.HevyWorkoutSnapshots.Add(snapshot);
            db.PerformedWorkouts.Add(performed);

            return (1, 0, 0, updatedAt);
        }
        catch (HevyMappingException exception)
        {
            // Reported and skipped, never fatal. One workout carrying an unmodelled set type must
            // not block every future sync -- and because the payload is kept, deciding later what
            // that type means is a recomputation rather than a re-fetch.
            logger.LogWarning(
                "Workout {WorkoutId} could not be mapped: {Reason}",
                workout.Id,
                exception.Message);

            snapshot.MappingFailure = Truncate(exception.Message);
            db.HevyWorkoutSnapshots.Add(snapshot);

            return (0, 0, 1, updatedAt);
        }
    }

    private async Task<(int Imported, int Tombstoned, int Unmapped, DateTimeOffset? At)> TombstoneAsync(
        string externalWorkoutId,
        string userId,
        DateTimeOffset? deletedAt,
        CancellationToken token)
    {
        var latest = await db.PerformedWorkouts
            .Where(row => row.UserId == userId && row.ExternalWorkoutId == externalWorkoutId)
            .OrderByDescending(row => row.Version)
            .FirstOrDefaultAsync(token);

        // Nothing to tombstone, or already tombstoned. Both are no-ops rather than errors: the
        // user may have deleted a workout we never read, and a re-delivered event must add nothing.
        if (latest is null || latest.IsDeleted)
        {
            return (0, 0, 0, deletedAt?.ToUniversalTime());
        }

        db.PerformedWorkouts.Add(new PerformedWorkout
        {
            Id = Guid.CreateVersion7(),
            UserId = userId,
            ExternalWorkoutId = externalWorkoutId,
            ExternalRoutineId = latest.ExternalRoutineId,
            ExternalTitle = latest.ExternalTitle,
            StartedAt = latest.StartedAt,
            EndedAt = latest.EndedAt,
            ExternallyUpdatedAt = deletedAt?.ToUniversalTime() ?? DateTimeOffset.UtcNow,
            Version = latest.Version + 1,
            // A tombstone carries no exercises. What it says is "this stopped counting", and the
            // sets it used to carry are still readable on the version below it.
            IsDeleted = true,
        });

        return (0, 1, 0, deletedAt?.ToUniversalTime());
    }

    private async Task<int> NextVersionAsync(string externalWorkoutId, string userId, CancellationToken token)
    {
        var highest = await db.PerformedWorkouts
            .Where(row => row.UserId == userId && row.ExternalWorkoutId == externalWorkoutId)
            .Select(row => (int?)row.Version)
            .MaxAsync(token);

        return (highest ?? 0) + 1;
    }

    private static string Truncate(string message) =>
        message.Length <= 500 ? message : message[..500];
}

/// <summary>What a sync produced. Counts rather than rows, because the caller reports, not reads.</summary>
public sealed record SyncResult(
    SyncOutcome Outcome,
    int Imported = 0,
    int Tombstoned = 0,
    int Unmapped = 0);

/// <summary>
/// How a sync ended. A partial result is not an error state: the cursor advanced over everything
/// that committed, so the next run continues rather than repeating (ADR-021).
/// </summary>
public enum SyncOutcome
{
    Ok,
    NotConnected,
    RateLimited,
    Unreachable,
}
