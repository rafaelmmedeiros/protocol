namespace Protocol.Api.Hevy;

/// <summary>
/// One workout exactly as Hevy sent it, kept beside the rows it was mapped into (ADR-018).
/// <para>
/// It lives in <c>Hevy/</c> rather than in <c>Training/</c> deliberately: this is a third
/// party's payload, and the domain does not hold one (root standard 17). Nothing reasons about
/// this table — it is an archive.
/// </para>
/// <para>
/// **Why it exists.** `TD-017` converts effort by discarding information on purpose and names
/// the conditions under which that conversion should be revisited. A conversion that may change
/// is a conversion whose inputs have to survive, and Hevy is not a guaranteed archive of its
/// own. With the payload kept, a changed mapping is a recomputation; without it, a re-fetch of a
/// history the vendor may no longer hold.
/// </para>
/// <para>
/// **And it is what makes a mapping failure survivable.** The snapshot is written *before* the
/// payload is mapped, so a workout carrying something we do not model — an unknown set type, an
/// RPE outside their own anchors — is still captured. The sync reports it and moves on, and the
/// row can be mapped later once a record decides what the unknown thing means.
/// </para>
/// </summary>
public sealed class HevyWorkoutSnapshot
{
    public required Guid Id { get; init; }

    public required string UserId { get; init; }

    /// <summary>Hevy's workout identifier (root standard 8).</summary>
    public required string ExternalWorkoutId { get; init; }

    /// <summary>Matches the version of the <c>PerformedWorkout</c> it was mapped into, when it was.</summary>
    public required int Version { get; init; }

    /// <summary>
    /// When Hevy last changed the workout this payload describes. Carried here as well as on the
    /// mapped row, because a snapshot that failed to map has no mapped row to carry it.
    /// </summary>
    public required DateTimeOffset ExternallyUpdatedAt { get; init; }

    /// <summary>The payload, verbatim.</summary>
    public required string RawJson { get; init; }

    /// <summary>When we read it. UTC (root standard 5).</summary>
    public required DateTimeOffset FetchedAt { get; init; }

    /// <summary>
    /// Why this payload has no mapped rows, when it has none. Null on the ordinary path.
    /// <para>
    /// Kept as text rather than a code because nothing branches on it: it is a note for a human
    /// deciding what an unmodelled value should mean, and that decision is a record, not a
    /// runtime path.
    /// </para>
    /// </summary>
    public string? MappingFailure { get; set; }
}
