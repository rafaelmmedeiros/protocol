namespace Protocol.Api.Training;

/// <summary>"Never prescribe me this exercise."</summary>
public sealed class ExerciseExclusion
{
    public Guid Id { get; init; }

    public required string UserId { get; init; }

    public required Guid ExerciseId { get; init; }
}

/// <summary>
/// "When you need this movement, use this one." One per movement pattern.
/// </summary>
public sealed class PreferredVariant
{
    public Guid Id { get; init; }

    public required string UserId { get; init; }

    public required MovementPattern MovementPattern { get; init; }

    public required Guid ExerciseId { get; init; }
}

/// <summary>
/// What a user has said about exercises, as the generator consumes it.
/// <para>
/// Two lists and no numbers, deliberately. A per-user score blended with the catalogue's rank
/// would let an invented weight sit on top of a variable the evidence nulls, which is the
/// composition `ranking-exercise-variants` argues against and `ADR-011` rejected. This is filter
/// first, then order.
/// </para>
/// <para>
/// What these may and may not reach is `TD-016`: the draw pool and its order, never the volume
/// arithmetic, and never the repetition range, proximity to failure or rest — the one place
/// self-selection has a measured price.
/// </para>
/// </summary>
public sealed record TrainingPreferences(
    IReadOnlySet<Guid> ExcludedExerciseIds,
    IReadOnlyDictionary<MovementPattern, Guid> PreferredByPattern)
{
    /// <summary>A user who has said nothing.</summary>
    public static TrainingPreferences None { get; } =
        new(new HashSet<Guid>(), new Dictionary<MovementPattern, Guid>());

    /// <summary>Whether this exercise is the one the user asked for in its movement pattern.</summary>
    public bool IsPreferred(Exercise exercise) =>
        PreferredByPattern.TryGetValue(exercise.MovementPattern, out var preferred)
        && preferred == exercise.Id;
}
