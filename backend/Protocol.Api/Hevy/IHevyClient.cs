namespace Protocol.Api.Hevy;

/// <summary>
/// Everything this system asks of Hevy, behind one seam.
/// <para>
/// The seam exists for two reasons. It keeps Hevy's shape out of the domain (root standard 17),
/// and it is what lets the suites run without ever reaching the real service — an integration
/// test that validated a key against api.hevyapp.com would be testing their uptime and spending
/// a real account's rate budget.
/// </para>
/// <para>
/// It grows one method at a time, as each step of M3 needs one. There is deliberately no delete:
/// Hevy has no endpoint for one, and ADR-017 is built on that fact rather than around it.
/// </para>
/// </summary>
public interface IHevyClient
{
    /// <summary>
    /// Asks Hevy whether a key is real, by fetching the account it belongs to.
    /// <para>
    /// Validation happens before a key is stored (ADR-014) so that a typo fails at the moment
    /// the user can still fix it, rather than silently at the first sync.
    /// </para>
    /// </summary>
    Task<HevyKeyCheck> CheckKeyAsync(string apiKey, CancellationToken token);

    /// <summary>Creates the folder a week's routines live in (ADR-015). Its identifier is a number.</summary>
    Task<HevyWrite<long>> CreateFolderAsync(string apiKey, string title, CancellationToken token);

    /// <summary>
    /// Whether a folder we created is still there.
    /// <para>
    /// Asked because the user can delete it in Hevy and we cannot: sending routines into a folder
    /// that no longer exists is refused with a 400 whose only explanation is prose, and branching
    /// on a third party's error sentence is exactly what standard 3 refuses to do in our own API.
    /// A 404 here is a fact; the sentence beside it is not.
    /// </para>
    /// </summary>
    Task<HevyWrite<bool>> FolderExistsAsync(string apiKey, long folderId, CancellationToken token);

    /// <summary>Creates one session's routine. Its identifier is a string, and it is the join (ADR-019).</summary>
    Task<HevyWrite<string>> CreateRoutineAsync(string apiKey, HevyRoutinePayload routine, CancellationToken token);

    /// <summary>
    /// Replaces a routine that already exists, which is how a week nothing has trained from is
    /// re-pushed without leaving litter Hevy gives us no way to remove (ADR-017).
    /// </summary>
    Task<HevyWrite<string>> UpdateRoutineAsync(
        string apiKey,
        string routineId,
        HevyRoutinePayload routine,
        CancellationToken token);

    /// <summary>
    /// One page of what changed at or after <paramref name="since"/> (ADR-018).
    /// <para>
    /// The events feed rather than the workouts list, because it is the only surface that reports
    /// **deletions**: on the plain list a removed workout simply stops appearing, which is
    /// indistinguishable from a paging bug. Its page is capped at ten items by Hevy, so the
    /// caller pages rather than assuming a size.
    /// </para>
    /// </summary>
    Task<HevyWrite<HevyWorkoutEventPage>> ListWorkoutEventsAsync(
        string apiKey,
        DateTimeOffset since,
        int page,
        int pageSize,
        CancellationToken token);
}

/// <summary>
/// The outcome of offering a key to Hevy. Three states, because "wrong key" and "Hevy is down"
/// are different answers to the user and must not collapse into one.
/// </summary>
public enum HevyKeyCheck
{
    /// <summary>Hevy accepted the key.</summary>
    Valid,

    /// <summary>Hevy answered, and rejected the key.</summary>
    Invalid,

    /// <summary>Hevy did not answer, or answered with a server error.</summary>
    Unreachable,
}

/// <summary>What happened to a write, and what it produced when it worked.</summary>
public sealed record HevyWrite<T>(HevyWriteOutcome Outcome, T? Value = default)
{
    public bool Ok => Outcome == HevyWriteOutcome.Ok;
}

/// <summary>
/// Why a write ended as it did. Kept apart rather than collapsed into "failed", because each one
/// is a different sentence to the user and a different next action.
/// </summary>
public enum HevyWriteOutcome
{
    Ok,

    /// <summary>
    /// The routine being replaced no longer exists — the user deleted it in Hevy. Handled as a
    /// push failure rather than as corruption (ADR-017).
    /// </summary>
    NotFound,

    /// <summary>Refused for rate reasons, after the retries were already exhausted (ADR-021).</summary>
    RateLimited,

    /// <summary>Hevy did not answer, or answered with something we cannot act on.</summary>
    Unreachable,

    /// <summary>
    /// Hevy answered successfully and the body did not carry what we needed.
    /// <para>
    /// Its own outcome rather than folded into <see cref="Unreachable"/>, because it means our
    /// reading of their shape is wrong — which is a bug of ours, not an outage of theirs, and the
    /// two want different reactions.
    /// </para>
    /// </summary>
    Unreadable,
}
