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
    public required DateOnly WeekStartDate { get; init; }

    /// <summary>When this week was generated, in UTC (root standard 5).</summary>
    public required DateTimeOffset GeneratedAt { get; init; }

    /// <summary>The goal in force when this week was generated (ADR-003).</summary>
    public required TrainingGoal Goal { get; init; }

    /// <summary>The frequency in force when this week was generated (ADR-003).</summary>
    public required int DaysPerWeek { get; init; }

    /// <summary>The session duration in force when this week was generated (ADR-003).</summary>
    public required int SessionDurationSeconds { get; init; }

    public ICollection<GeneratedSession> Sessions { get; init; } = [];
}

/// <summary>One day of a stored week.</summary>
public sealed class GeneratedSession
{
    public Guid Id { get; init; }

    public Guid GeneratedWeekId { get; init; }

    /// <summary>Ordered position within the week, starting at one.</summary>
    public required int Position { get; init; }

    public required DayOfWeek Day { get; init; }

    public required SessionKind Kind { get; init; }

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

    /// <summary>Repetitions in reserve. Never below two, and never failure (TD-010).</summary>
    public required int RepsInReserve { get; init; }

    /// <summary>Rest between sets, in seconds — the unit is in the name (root standard 4).</summary>
    public required int RestSeconds { get; init; }
}
