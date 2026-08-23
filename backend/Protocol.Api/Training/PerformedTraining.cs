namespace Protocol.Api.Training;

/// <summary>
/// Training that actually happened, in our vocabulary.
/// <para>
/// These types are the domain's side of the import boundary (root standard 17). Nothing here is
/// shaped by Hevy: the set kinds are ours, the units are canonical, the identifiers are ours with
/// theirs beside them (root standard 8), and every one of these records can be constructed with
/// no Hevy payload in sight. The day the logging surface is ours, what gets deleted is the
/// mapper — not this file.
/// </para>
/// </summary>
public sealed class PerformedWorkout
{
    public required Guid Id { get; init; }

    public required string UserId { get; init; }

    /// <summary>Hevy's workout identifier, in its own column and never a key (standard 8).</summary>
    public required string ExternalWorkoutId { get; init; }

    /// <summary>
    /// The routine this workout was started from, when there was one. **The only join between a
    /// prescribed session and what was performed** (ADR-019) — null for training that started
    /// from nothing, which is ordinary and stays first-class rather than being discarded.
    /// </summary>
    public string? ExternalRoutineId { get; init; }

    /// <summary>Display only, never matched or compared on (standard 9).</summary>
    public string? ExternalTitle { get; init; }

    /// <summary>UTC, both of them (root standard 5).</summary>
    public required DateTimeOffset StartedAt { get; init; }

    public required DateTimeOffset EndedAt { get; init; }

    /// <summary>
    /// When Hevy last changed this workout. The reconciliation key: a workout that changed
    /// upstream arrives again with a later value, and history is appended rather than rewritten
    /// (root standard 7, ADR-018).
    /// </summary>
    public required DateTimeOffset ExternallyUpdatedAt { get; init; }

    public ICollection<PerformedExercise> Exercises { get; init; } = [];
}

/// <summary>One exercise inside a performed workout.</summary>
public sealed class PerformedExercise
{
    public Guid Id { get; init; }

    public Guid PerformedWorkoutId { get; init; }

    /// <summary>Order within the workout, as performed.</summary>
    public required int Position { get; init; }

    /// <summary>
    /// Our exercise, when the logged one is in our catalogue. **Null is expected and meaningful:**
    /// it is a movement the user trained that we do not model, which is the loud signal TD-004
    /// chose over a silent one and the input ADR-020 reads for a catalogue gap.
    /// </summary>
    public Guid? ExerciseId { get; init; }

    /// <summary>Hevy's template identifier, beside ours rather than instead of it (standard 8).</summary>
    public required string ExternalTemplateId { get; init; }

    /// <summary>Display only (standard 9). For an exercise outside our catalogue it is the only human handle.</summary>
    public string? ExternalTitle { get; init; }

    public ICollection<PerformedSet> Sets { get; init; } = [];
}

/// <summary>
/// One performed set.
/// <para>
/// The sequence these belong to is ordered and is never reduced to a total: 11/9/8 and 8/9/11 are
/// different facts about the same session, and the shape of the fall is what a progression rule
/// would have to read.
/// </para>
/// </summary>
public sealed class PerformedSet
{
    public Guid Id { get; init; }

    public Guid PerformedExerciseId { get; init; }

    /// <summary>Order within the exercise, as performed.</summary>
    public required int Position { get; init; }

    public required SetKind Kind { get; init; }

    /// <summary>Kilograms, always — the unit is in the name (root standard 4). Null for bodyweight work.</summary>
    public double? WeightKg { get; init; }

    public double? Reps { get; init; }

    /// <summary>
    /// What the user reported having left, converted from their scale on the way in (TD-017).
    /// <para>
    /// **Null means they reported nothing, and never means they had nothing left.** In every
    /// workout read from a real account so far this is null on every set, so any rule that
    /// consumes it has to say what it does when it is absent — which is always.
    /// </para>
    /// </summary>
    public int? RepsInReserve { get; init; }
}

/// <summary>
/// What a set was, in our words.
/// <para>
/// Ours rather than theirs, so that the volume arithmetic keys on a meaning instead of on a
/// third party's string. Only <see cref="Working"/> counts toward weekly volume (TD-006); a
/// warm-up set is retained on import and excluded where volume is counted, never dropped
/// (ADR-018).
/// </para>
/// </summary>
public enum SetKind
{
    /// <summary>A working set. The only kind that counts toward volume.</summary>
    Working,

    /// <summary>Ramping toward the working load. Retained, never counted.</summary>
    WarmUp,

    /// <summary>A continuation at a reduced load. Never prescribed by this system.</summary>
    DropSet,

    /// <summary>Taken to momentary failure. Never prescribed by this system (TD-018).</summary>
    ToFailure,
}
