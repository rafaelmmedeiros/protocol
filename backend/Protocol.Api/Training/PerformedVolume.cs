namespace Protocol.Api.Training;

/// <summary>
/// Fractional weekly volume, counted from training that actually happened.
/// <para>
/// The same arithmetic the generator plans with (TD-006) — a set counts 1.0 toward a muscle it
/// loads directly and 0.5 toward one it loads indirectly — applied to the other side of the
/// loop. Planned and performed have to be counted the same way, or comparing them compares two
/// different quantities.
/// </para>
/// </summary>
public static class PerformedVolume
{
    /// <summary>
    /// The current reading of each workout: highest version wins, and anything tombstoned drops
    /// out.
    /// <para>
    /// Nothing is deleted to make this true (root standard 7). A workout the user removed
    /// upstream still has every row it ever had; it simply stops being the reading that counts,
    /// which is what lets an analysis produced last month still be explained.
    /// </para>
    /// </summary>
    public static IReadOnlyList<PerformedWorkout> Current(IEnumerable<PerformedWorkout> workouts) =>
        [.. workouts
            .GroupBy(workout => workout.ExternalWorkoutId)
            .Select(versions => versions.MaxBy(workout => workout.Version)!)
            .Where(workout => !workout.IsDeleted)];

    /// <summary>
    /// Fractional sets per muscle across the given workouts.
    /// <para>
    /// **Only working sets count.** A warm-up set is retained on import and excluded here, never
    /// dropped on the way in (ADR-018) — the record of what was performed is complete, and the
    /// arithmetic is where the exclusion belongs. Drop sets and sets taken to failure are
    /// excluded on the same principle for now: this system never prescribes them (TD-013,
    /// TD-018), so counting them as working sets would credit volume against a prescription that
    /// did not ask for it.
    /// </para>
    /// <para>
    /// An exercise outside our catalogue credits nothing, because we do not know what it loads.
    /// That is a gap in the catalogue rather than in the training, and ADR-020 is what surfaces
    /// it instead of letting it read as rest.
    /// </para>
    /// </summary>
    public static IReadOnlyDictionary<MuscleGroup, decimal> ByMuscle(
        IEnumerable<PerformedWorkout> workouts,
        IReadOnlyDictionary<Guid, Exercise> catalogue)
    {
        var volumes = new Dictionary<MuscleGroup, decimal>();

        foreach (var exercise in workouts.SelectMany(workout => workout.Exercises))
        {
            if (exercise.ExerciseId is not { } id || !catalogue.TryGetValue(id, out var ours))
            {
                continue;
            }

            var workingSets = exercise.Sets.Count(set => set.Kind == SetKind.Working);

            if (workingSets == 0)
            {
                continue;
            }

            foreach (var muscle in ours.Muscles)
            {
                var credit = muscle.Role == MuscleRole.Primary
                    ? TrainingPrescription.PrimarySetCredit    // TD-006
                    : TrainingPrescription.SecondarySetCredit; // TD-006

                volumes[muscle.MuscleGroup] = volumes.GetValueOrDefault(muscle.MuscleGroup)
                    + (workingSets * credit);
            }
        }

        return volumes;
    }
}
