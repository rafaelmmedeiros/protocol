namespace Protocol.Api.Training;

/// <summary>
/// One muscle an exercise loads, and how directly.
/// <para>
/// This is a relation rather than a pair of columns because a muscle's weekly volume is counted
/// fractionally — a set where the muscle is <see cref="MuscleRole.Primary"/> counts 1.0, a set
/// where it is <see cref="MuscleRole.Secondary"/> counts 0.5 (TD-006). A generator that counted
/// only direct sets would systematically under-read arm and shoulder volume on any push/pull or
/// upper/lower template, so secondary musculature has to be modelled to be counted.
/// </para>
/// </summary>
public sealed class ExerciseMuscle
{
    public Guid ExerciseId { get; init; }

    public required MuscleGroup MuscleGroup { get; init; }

    public required MuscleRole Role { get; init; }
}
