namespace Protocol.Api.Training;

/// <summary>
/// A week that was generated for a user, stored as it was generated and never edited.
/// <para>
/// Immutable is about the record, not the interface (ADR-003): regenerating is expected and
/// writes a new row. What the user read yesterday has to stay readable, and it has to stay
/// explainable against the decisions in force when it was produced — a superseded <c>TD</c>
/// would otherwise rewrite history that was never recorded as history (root standard 7).
/// </para>
/// <para>
/// The profile's values are <b>snapshotted</b> here rather than referenced. A profile is current
/// state and a generated week is not; a week whose frequency changed when the user edited their
/// profile would be a week nobody ever trained.
/// </para>
/// </summary>
public sealed class GeneratedWeek
{
    public Guid Id { get; init; }

    public required string UserId { get; init; }

    /// <summary>
    /// The Monday the week begins on. The training week starts on Monday, always, and never
    /// derives that from locale (root standard 6).
    /// </summary>
    /// <summary>
    /// The Monday a plan was anchored to, for plans generated before `ADR-027`.
    /// <para>
    /// <b>Null on everything generated since, and kept rather than dropped.</b> A plan is now an
    /// ordered queue with no dates, but a week that *was* anchored still means what it meant, and
    /// deleting the column would make those rows unexplainable — which is what root standard 7
    /// protects and what `ADR-003` says about a stored week specifically.
    /// </para>
    /// </summary>
    public DateOnly? WeekStartDate { get; init; }

    /// <summary>When this week was generated, in UTC (root standard 5).</summary>
    public required DateTimeOffset GeneratedAt { get; init; }

    /// <summary>The goal in force when this week was generated (ADR-003).</summary>
    public required TrainingGoal Goal { get; init; }

    /// <summary>The frequency in force when this week was generated (ADR-003).</summary>
    public required int DaysPerWeek { get; init; }

    /// <summary>The session duration in force when this week was generated (ADR-003).</summary>
    public required int SessionDurationSeconds { get; init; }

    /// <summary>
    /// The Hevy routine folder this week was pushed into, if it has been pushed (ADR-015).
    /// <para>
    /// Their identifier, in its own column, never a key of ours (root standard 8) — and a
    /// number, where a routine's is a string, because that is what their API returns. Null means
    /// never pushed, which is the ordinary state of a week that is still being regenerated.
    /// </para>
    /// </summary>
    /// <summary>
    /// The volume band this week was generated under, snapshotted for the same reason
    /// <see cref="Goal"/> and <see cref="DaysPerWeek"/> are (ADR-003).
    /// <para>
    /// <b>Stored rather than derived, and it is the only thing about a week's explanation that
    /// is.</b> What a slot trains is catalogue data and is recomputed on read (ADR-029); the
    /// target is not recoverable from the plan at all — a week holding six fractional sets of
    /// quadriceps is indistinguishable from one that aimed at eight and ran out of minutes.
    /// Judging an old week against today's constant is exactly what ADR-003 exists to prevent.
    /// </para>
    /// <para>
    /// The window is a cycle, not a calendar week (TD-024). Rows that predate this column carry
    /// 6.0 and 6.0 — they were generated under TD-014's target and before TD-022 created a
    /// ceiling, so a ceiling equal to the target is the faithful statement that they were built
    /// to stop there.
    /// </para>
    /// </summary>
    public required decimal WeeklyTargetFractionalSets { get; init; }

    /// <summary>The upper edge of the same band (TD-022).</summary>
    public required decimal WeeklyCeilingFractionalSets { get; init; }

    public long? HevyRoutineFolderId { get; set; }

    public ICollection<GeneratedSession> Sessions { get; init; } = [];
}

/// <summary>One day of a stored week.</summary>
public sealed class GeneratedSession
{
    public Guid Id { get; init; }

    public Guid GeneratedWeekId { get; init; }

    /// <summary>Ordered position within the week, starting at one.</summary>
    public required int Position { get; init; }

    /// <summary>
    /// The weekday this session was assigned, for plans generated before `ADR-027`. Null since:
    /// a session has a position and the queue decides what is next (see
    /// <see cref="GeneratedWeek.WeekStartDate"/> for why it is kept).
    /// </summary>
    public DayOfWeek? Day { get; init; }

    public required SessionKind Kind { get; init; }

    /// <summary>
    /// The Hevy routine this session was pushed as (ADR-015). **The join** — a workout started
    /// from this routine comes back carrying this identifier, and that is the only association
    /// Hevy provides (ADR-019). External, and never a key of ours (root standard 8).
    /// </summary>
    public string? HevyRoutineId { get; set; }

    public ICollection<GeneratedPrescription> Prescriptions { get; init; } = [];
}

/// <summary>
/// A slot as it was prescribed: one exercise, and what to do with it.
/// <para>
/// There is no load column. M1 prescribes sets, repetitions, proximity to failure and rest, and
/// nothing about weight — so a <c>weight_kg</c> column would be a field nothing writes. When
/// load arrives it carries its unit in the name (root standard 4).
/// </para>
/// </summary>
public sealed class GeneratedPrescription
{
    public Guid Id { get; init; }

    public Guid GeneratedSessionId { get; init; }

    /// <summary>Ordered position within the session, starting at one (TD-007).</summary>
    public required int Position { get; init; }

    /// <summary>
    /// Our exercise, by our own key. The catalogue row is referenced rather than copied: an
    /// exercise's attributes are reference data, not something the week decided.
    /// </summary>
    public required Guid ExerciseId { get; init; }

    public Exercise? Exercise { get; init; }

    public required int Sets { get; init; }

    public required int MinReps { get; init; }

    public required int MaxReps { get; init; }

    /// <summary>Repetitions in reserve. Two for every exercise, and never failure (TD-018).</summary>
    public required int RepsInReserve { get; init; }

    /// <summary>Rest between sets, in seconds — the unit is in the name (root standard 4).</summary>
    public required int RestSeconds { get; init; }
}
