using Microsoft.EntityFrameworkCore;
using Protocol.Api.Auth;

namespace Protocol.Api.Training;

/// <summary>
/// Erasing everything one user owns, so the whole loop can be exercised from nothing (ADR-025).
/// <para>
/// <b>This contradicts root standard 7, and that is said out loud rather than glossed.</b> Training
/// history is append-only: a correction arrives as a new record and nothing is mutated or deleted.
/// What makes this acceptable <i>today</i> is that almost nothing here is irrecoverable — a
/// generated week regenerates from the same profile (ADR-005), imported history comes back from
/// Hevy on the next sync (ADR-018), and equipment is a few minutes of typing.
/// </para>
/// <para>
/// <b>None of that survives M6</b>, the milestone that starts storing judgements this system made —
/// a chosen load, a progression step — which Hevy cannot return and no regeneration reproduces.
/// ADR-025 carries the expiry; this comment carries it to the place someone would actually read it.
/// That record is append-only and still calls the milestone M5, its number before a milestone was
/// inserted ahead of it — same milestone, former number (docs/ROADMAP.md).
/// </para>
/// <para>
/// The alternative it replaces is the reason it exists: "reset by hand" means opening the
/// development database, and root standard 14 says that is the moment to stop and ask rather than
/// the moment to type. A missing affordance does not remove the need, it moves it somewhere
/// unguarded.
/// </para>
/// </summary>
public static class EraseUserData
{
    /// <summary>
    /// The switch that makes the endpoint exist at all.
    /// <para>
    /// Same shape as <see cref="Hevy.FakeHevyClient.EnabledKey"/> and for the same reason: a
    /// feature justified by "we are still iterating" must be <b>absent</b> where that is untrue,
    /// not present and politely refusing. A published deployment never sets this, so the route is
    /// never mapped and the answer is a 404 from the router rather than a 403 from a check someone
    /// could later relax.
    /// </para>
    /// </summary>
    public const string EnabledKey = "Development:AllowErase";

    /// <summary>
    /// Deletes every row belonging to <paramref name="userId"/> and returns what went, per table.
    /// <para>
    /// <b>The account itself survives</b>, so the user stays signed in and lands on a product that
    /// looks exactly as it does to someone who just registered — which is the state being
    /// reproduced.
    /// </para>
    /// <para>
    /// <b>Two tables are never touched, and the reasons are different.</b> <c>exercises</c> is a
    /// global seed shared with every other user: it is not "mine" in any sense, and erasing it
    /// would break every other account's stored weeks, which reference those identifiers (root
    /// standard 7). The Data Protection key ring is what makes every <i>other</i> user's stored
    /// Hevy key decryptable (ADR-014); dropping it would silently destroy credentials belonging to
    /// people who did not ask for anything.
    /// </para>
    /// <para>
    /// <c>PerformedExercises</c> and <c>PerformedSets</c> are absent below because they cascade
    /// from <c>PerformedWorkouts</c> in the schema. They are counted through their workouts rather
    /// than deleted separately, so the log reports rows the database actually removed.
    /// </para>
    /// </summary>
    public static async Task<ErasedCounts> EraseAsync(
        AppDbContext db,
        string userId,
        CancellationToken token)
    {
        // ExecuteDeleteAsync issues one DELETE per table rather than loading every row to delete
        // it. On an account with 757 workouts and 19,138 sets, the difference is not cosmetic.
        var profiles = await db.TrainingProfiles.Where(row => row.UserId == userId)
            .ExecuteDeleteAsync(token);
        var equipment = await db.UserEquipment.Where(row => row.UserId == userId)
            .ExecuteDeleteAsync(token);
        var exclusions = await db.ExerciseExclusions.Where(row => row.UserId == userId)
            .ExecuteDeleteAsync(token);
        var preferredVariants = await db.PreferredVariants.Where(row => row.UserId == userId)
            .ExecuteDeleteAsync(token);
        var declined = await db.DeclinedEquipmentSuggestions.Where(row => row.UserId == userId)
            .ExecuteDeleteAsync(token);
        var weeks = await db.GeneratedWeeks.Where(row => row.UserId == userId)
            .ExecuteDeleteAsync(token);
        var snapshots = await db.HevyWorkoutSnapshots.Where(row => row.UserId == userId)
            .ExecuteDeleteAsync(token);
        var workouts = await db.PerformedWorkouts.Where(row => row.UserId == userId)
            .ExecuteDeleteAsync(token);

        // Last, and losing it is correct rather than incidental: re-entering the key is part of
        // exercising the loop from its start, which is the whole point of the affordance.
        var connections = await db.HevyConnections.Where(row => row.UserId == userId)
            .ExecuteDeleteAsync(token);

        return new ErasedCounts(
            profiles,
            equipment,
            exclusions,
            preferredVariants,
            declined,
            weeks,
            snapshots,
            workouts,
            connections);
    }
}

/// <summary>
/// What an erase removed, per table.
/// <para>
/// Worth a log line with its counts, because afterwards "the data was erased" and "the import never
/// ran" look identical from every screen (ADR-025, root standard 12).
/// </para>
/// </summary>
public sealed record ErasedCounts(
    int Profiles,
    int Equipment,
    int Exclusions,
    int PreferredVariants,
    int DeclinedSuggestions,
    int GeneratedWeeks,
    int Snapshots,
    int PerformedWorkouts,
    int HevyConnections);
