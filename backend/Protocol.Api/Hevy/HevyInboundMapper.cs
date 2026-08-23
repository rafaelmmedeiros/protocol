using Protocol.Api.Training;

namespace Protocol.Api.Hevy;

/// <summary>
/// A logged Hevy workout, translated into training we can reason about. The inward half of the
/// boundary, and the only place that direction is spelled (root standard 17).
/// <para>
/// This is where every one of Hevy's concepts stops. Their set type becomes our
/// <see cref="SetKind"/>, their RPE becomes our repetitions in reserve (TD-017), their template
/// identifier is carried beside our exercise rather than as it (standard 8), and their title is
/// carried for display and never read (standard 9).
/// </para>
/// </summary>
public static class HevyInboundMapper
{
    /// <summary>
    /// Translates a workout.
    /// </summary>
    /// <param name="workout">The payload, as Hevy returned it.</param>
    /// <param name="userId">Whose training this is.</param>
    /// <param name="exerciseIdFor">
    /// Our exercise for one of their template identifiers, or null when we do not model it. A
    /// lookup rather than a match: the catalogue is keyed to Hevy by an external identifier
    /// (ADR-002), so this is never a comparison of titles.
    /// </param>
    /// <exception cref="HevyMappingException">
    /// A set carries a type we do not model, or an RPE outside Hevy's own anchors. Loud, because
    /// silently counting an unknown set as a working one would inflate every volume number the
    /// system produces (TD-006).
    /// </exception>
    public static PerformedWorkout ToPerformedWorkout(
        HevyWorkout workout,
        string userId,
        Func<string, Guid?> exerciseIdFor)
    {
        var exercises = (workout.Exercises ?? [])
            .OrderBy(exercise => exercise.Index)
            .Select(exercise => ToExercise(exercise, exerciseIdFor))
            .ToList();

        return new PerformedWorkout
        {
            Id = Guid.CreateVersion7(),
            UserId = userId,
            ExternalWorkoutId = workout.Id,
            // The join, and the only one (ADR-019). Null is ordinary: it means the user trained
            // without starting from a routine, and that history still counts at the exercise
            // level, which is where progression reads anyway.
            ExternalRoutineId = workout.RoutineId,
            ExternalTitle = workout.Title,
            StartedAt = workout.StartTime.ToUniversalTime(),
            EndedAt = workout.EndTime.ToUniversalTime(),
            ExternallyUpdatedAt = workout.UpdatedAt.ToUniversalTime(),
            // Ordered on the way in and kept ordered by Position on the way out. The sequence is
            // never reduced to a total: 11/9/8 and 8/9/11 are different facts.
            Exercises = exercises,
        };
    }

    private static PerformedExercise ToExercise(
        HevyWorkoutExercise exercise,
        Func<string, Guid?> exerciseIdFor)
    {
        var templateId = exercise.ExerciseTemplateId;

        if (string.IsNullOrWhiteSpace(templateId))
        {
            throw new HevyMappingException(
                $"A logged exercise at index {exercise.Index} carries no template identifier.");
        }

        var sets = (exercise.Sets ?? [])
            .OrderBy(set => set.Index)
            .Select(ToSet)
            .ToList();

        return new PerformedExercise
        {
            Position = exercise.Index,
            // Null when the movement is outside our catalogue. That is a gap in the catalogue,
            // not a gap in the training, and it is surfaced rather than dropped (ADR-020).
            ExerciseId = exerciseIdFor(templateId),
            ExternalTemplateId = templateId,
            ExternalTitle = exercise.Title,
            Sets = sets,
        };
    }

    private static PerformedSet ToSet(HevyWorkoutSet set) => new()
    {
        Position = set.Index,
        Kind = ToSetKind(set.Type),
        WeightKg = set.WeightKg,
        Reps = set.Reps,
        // Absent stays absent (TD-017). A set with no reported effort yields no reserve, never a
        // default, because "reported nothing" and "had nothing left" are opposite claims.
        RepsInReserve = EffortConversion.ToRepsInReserve(set.Rpe),
    };

    /// <summary>
    /// Their set type, in our words.
    /// <para>
    /// Refuses anything it does not know rather than falling back to a working set. A new type
    /// counted as working would inflate every fractional volume figure the system produces
    /// (TD-006), and it would do it silently — the raw payload is retained (ADR-018), so failing
    /// loudly loses nothing and a wrong guess would.
    /// </para>
    /// </summary>
    private static SetKind ToSetKind(string? type) => type switch
    {
        "normal" => SetKind.Working,
        "warmup" => SetKind.WarmUp,
        "dropset" => SetKind.DropSet,
        "failure" => SetKind.ToFailure,
        _ => throw new HevyMappingException($"Unmodelled Hevy set type '{type}'."),
    };
}
