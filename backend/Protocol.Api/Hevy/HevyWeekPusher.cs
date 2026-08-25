using Microsoft.EntityFrameworkCore;
using Protocol.Api.Auth;
using Protocol.Api.Training;

namespace Protocol.Api.Hevy;

/// <summary>
/// Puts a generated week into Hevy: a folder for the week, one routine per session (ADR-015).
/// <para>
/// Sequential, and safe to retry. Nothing here ever deletes, because Hevy has no endpoint for it
/// — ADR-017 is built on that fact rather than around it, and a half-created week is recovered by
/// pushing again rather than by cleaning up.
/// </para>
/// </summary>
public sealed class HevyWeekPusher(AppDbContext db, IHevyClient hevy, HevyKeyProtector protector)
{
    public async Task<PushResult> PushAsync(
        Guid weekId,
        string userId,
        string? locale,
        CancellationToken token)
    {
        var connection = await db.HevyConnections.SingleOrDefaultAsync(c => c.UserId == userId, token);

        if (connection is null)
        {
            return new PushResult(PushOutcome.NotConnected);
        }

        var week = await db.GeneratedWeeks
            .Include(w => w.Sessions.OrderBy(s => s.Position))
                .ThenInclude(s => s.Prescriptions.OrderBy(p => p.Position))
                    .ThenInclude(p => p.Exercise)
            .SingleOrDefaultAsync(w => w.Id == weekId && w.UserId == userId, token);

        if (week is null)
        {
            return new PushResult(PushOutcome.WeekNotFound);
        }

        // Anything already trained from is evidence, and rewriting it would leave a logged
        // workout pointing at a prescription that did not exist when it was performed (ADR-017).
        // A regenerated week never reaches this branch: ADR-009 makes it a new row, and a new row
        // has no folder, so it takes the create path and the old routines are left standing.
        if (await HasBeenTrainedFromAsync(week, userId, token))
        {
            return new PushResult(PushOutcome.AlreadyTrainedFrom);
        }

        var apiKey = protector.Unprotect(connection.ProtectedApiKey);

        // A folder we created can be deleted in Hevy by the user, and we cannot delete anything
        // ourselves (ADR-017). Sending routines into one that is gone is refused with a 400 whose
        // only explanation is prose; asking first turns that into a fact we can act on.
        if (week.HevyRoutineFolderId is { } stored && stored > 0)
        {
            var exists = await hevy.FolderExistsAsync(apiKey, stored, token);

            if (exists.Outcome == HevyWriteOutcome.NotFound)
            {
                // Gone. Treated exactly like never having had one: the week recovers by creating
                // a real folder instead of failing forever against a dead identifier.
                week.HevyRoutineFolderId = null;
            }
            else if (!exists.Ok)
            {
                return Failed(exists.Outcome);
            }
        }

        // Not `is null` alone: rows written before the response envelope was understood carry a
        // stored zero, which is not a folder Hevy ever had.
        if (week.HevyRoutineFolderId is null or <= 0)
        {
            var folder = await hevy.CreateFolderAsync(apiKey, FolderTitle(week), token);

            if (!folder.Ok)
            {
                return Failed(folder.Outcome);
            }

            week.HevyRoutineFolderId = folder.Value;
            // Saved before the routines are created. A push interrupted after this point resumes
            // into the same folder instead of creating a second one.
            await db.SaveChangesAsync(token);
        }

        foreach (var session in week.Sessions.OrderBy(session => session.Position))
        {
            HevyRoutinePayload routine;

            try
            {
                routine = HevyOutboundMapper.ToRoutine(
                    RoutineTitle(week, session),
                    week.HevyRoutineFolderId,
                    [.. session.Prescriptions],
                    prescription => RoutineNotes.For(prescription, locale));
            }
            catch (HevyMappingException)
            {
                // An exercise with no external key. Loud rather than substituted (ADR-016).
                return new PushResult(PushOutcome.ExerciseNotMappable);
            }

            var written = session.HevyRoutineId is { } existing
                ? await hevy.UpdateRoutineAsync(apiKey, existing, routine, token)
                : await hevy.CreateRoutineAsync(apiKey, routine, token);

            if (written.Outcome == HevyWriteOutcome.NotFound)
            {
                // The user deleted it in Hevy. Recreating is safe *here specifically*: the
                // trained-from check above already passed, so no logged workout points at the
                // identifier being replaced and there is no join to break. Refusing instead
                // would tell the user to throw a week away because they tidied their own app.
                written = await hevy.CreateRoutineAsync(apiKey, routine, token);
            }

            if (!written.Ok)
            {
                return Failed(written.Outcome);
            }

            session.HevyRoutineId = written.Value;
            // One save per session, so an interrupted push keeps every routine it managed to
            // create and the retry replaces them rather than duplicating them.
            await db.SaveChangesAsync(token);
        }

        return new PushResult(
            PushOutcome.Ok,
            week.HevyRoutineFolderId,
            [.. week.Sessions.OrderBy(s => s.Position).Select(s => new PushedSession(s.Id, s.HevyRoutineId!))]);
    }

    /// <summary>
    /// Whether any imported workout was started from one of this week's routines.
    /// <para>
    /// The same lookup ADR-019 binds on, asked of the whole week. It returns false for every week
    /// until the import lands, which is correct rather than a placeholder: nothing has been
    /// trained from, because nothing has been read.
    /// </para>
    /// </summary>
    private async Task<bool> HasBeenTrainedFromAsync(
        GeneratedWeek week,
        string userId,
        CancellationToken token)
    {
        var routineIds = week.Sessions
            .Select(session => session.HevyRoutineId)
            .OfType<string>()
            .ToList();

        // Scoped to this user, and the omission was a real bug: a routine identifier is Hevy's,
        // not ours, so nothing guarantees it is unique across accounts -- and without this filter
        // one user's imported training could refuse another user's push. Found by the E2E suite
        // running sixteen workers against one API.
        return routineIds.Count != 0
            && await db.PerformedWorkouts.AnyAsync(
                performed => performed.UserId == userId
                    && performed.ExternalRoutineId != null
                    && routineIds.Contains(performed.ExternalRoutineId),
                token);
    }

    /// <summary>
    /// What the folder is called in Hevy. Display only, never read back — the binding is on
    /// identifiers alone (ADR-019, standard 9), so this exists for a human scrolling their app.
    /// </summary>
    /// <summary>
    /// One folder per generated plan, named for when the plan was generated (ADR-031). It used
    /// to be named for the week the plan was anchored to; ADR-027 removed that, and the
    /// generation timestamp is what identifies a plan now.
    /// </summary>
    private static string FolderTitle(GeneratedWeek week) =>
        $"Protocol · {week.GeneratedAt:yyyy-MM-dd HH:mm}";

    /// <summary>
    /// A session by its place in the queue rather than by a weekday it no longer has. Display
    /// only, on both sides: nothing reads a title, and ADR-019 matches on identifiers alone.
    /// </summary>
    private static string RoutineTitle(GeneratedWeek week, GeneratedSession session) =>
        $"{session.Position}. {session.Kind}";

    private static PushResult Failed(HevyWriteOutcome outcome) => new(outcome switch
    {
        HevyWriteOutcome.NotFound => PushOutcome.RoutineMissing,
        HevyWriteOutcome.RateLimited => PushOutcome.RateLimited,
        HevyWriteOutcome.Unreadable => PushOutcome.Unreadable,
        _ => PushOutcome.Unreachable,
    });
}

/// <summary>What a push produced, in our vocabulary rather than in status codes.</summary>
public sealed record PushResult(
    PushOutcome Outcome,
    long? FolderId = null,
    IReadOnlyList<PushedSession>? Sessions = null);

/// <summary>One session and the routine it now lives in.</summary>
public sealed record PushedSession(Guid SessionId, string RoutineId);

/// <summary>Every way a push can end. Each one is a different sentence and a different next action.</summary>
public enum PushOutcome
{
    Ok,

    /// <summary>No Hevy key saved for this user.</summary>
    NotConnected,

    /// <summary>No such week, or not this user's.</summary>
    WeekNotFound,

    /// <summary>
    /// Something has already been logged against this week's routines, so they are evidence and
    /// are not rewritten (ADR-017). Regenerating produces a new week, which pushes freely.
    /// </summary>
    AlreadyTrainedFrom,

    /// <summary>A prescribed exercise has no external key and cannot be named to Hevy.</summary>
    ExerciseNotMappable,

    /// <summary>A routine we meant to replace no longer exists — the user deleted it in Hevy.</summary>
    RoutineMissing,

    /// <summary>Refused for rate reasons after the retries were exhausted (ADR-021).</summary>
    RateLimited,

    /// <summary>Hevy did not answer.</summary>
    Unreachable,

    /// <summary>
    /// Hevy answered and the body did not carry what we needed — our reading of their shape is
    /// wrong. Kept apart from <see cref="Unreachable"/> because it is a bug of ours, and a
    /// "try again" would be a lie.
    /// </summary>
    Unreadable,
}
