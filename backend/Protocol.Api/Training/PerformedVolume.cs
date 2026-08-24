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

    /// <summary>
    /// Kilograms lifted per muscle across the given workouts — weight times repetitions, credited
    /// on the same fractional rule as <see cref="ByMuscle"/> (TD-006).
    /// <para>
    /// **<c>weight_kg</c> is the total lifted, whatever the implement (ADR-024).** A 30 kg barbell
    /// curl and a 30 kg dumbbell curl are the same load and count the same here. That meaning
    /// cannot live in the field name — root standard 4 puts the *unit* there and has nowhere to put
    /// the convention — so it lives in the record and is cited at the arithmetic that depends on
    /// it. Nothing below inspects <see cref="Exercise.Equipment"/>, and nothing may start to:
    /// halving a dumbbell load would apply a factor-of-two error to every dumbbell movement, which
    /// is precisely the option ADR-024 rejected against this account's own history.
    /// </para>
    /// <para>
    /// **A unilateral set is knowingly left alone.** ADR-024 named it rather than solved it: a
    /// single-arm entry records the implement, because "total" and "per hand" are the same number
    /// there, so a single dumbbell preacher curl reads as heavier than a two-handed barbell one.
    /// <see cref="Exercise.Laterality"/> makes it expressible, and the right correction depends on
    /// what M5 does with load — deciding it here would be modelling a preference before its
    /// consumer exists.
    /// </para>
    /// <para>
    /// Working sets only, on the same rule <see cref="ByMuscle"/> applies. A set with no weight or
    /// no repetitions contributes nothing: bodyweight work is stored with a null load because the
    /// load is the body and Hevy does not report it, and treating that as zero kilograms would be a
    /// claim rather than an absence.
    /// </para>
    /// </summary>
    public static IReadOnlyDictionary<MuscleGroup, decimal> VolumeLoadByMuscle(
        IEnumerable<PerformedWorkout> workouts,
        IReadOnlyDictionary<Guid, Exercise> catalogue)
    {
        var loads = new Dictionary<MuscleGroup, decimal>();

        foreach (var exercise in workouts.SelectMany(workout => workout.Exercises))
        {
            if (exercise.ExerciseId is not { } id || !catalogue.TryGetValue(id, out var ours))
            {
                continue;
            }

            var kilograms = VolumeLoadOf(exercise);

            if (kilograms == 0m)
            {
                continue;
            }

            foreach (var muscle in ours.Muscles)
            {
                var credit = muscle.Role == MuscleRole.Primary
                    ? TrainingPrescription.PrimarySetCredit    // TD-006
                    : TrainingPrescription.SecondarySetCredit; // TD-006

                loads[muscle.MuscleGroup] = loads.GetValueOrDefault(muscle.MuscleGroup)
                    + (kilograms * credit);
            }
        }

        return loads;
    }

    /// <summary>
    /// Kilograms lifted in one exercise: the sum of weight times repetitions over its working sets,
    /// with no reference to what the weight was held in (ADR-024).
    /// </summary>
    public static decimal VolumeLoadOf(PerformedExercise exercise) =>
        exercise.Sets
            .Where(set => set.Kind == SetKind.Working)
            .Where(set => set is { WeightKg: > 0, Reps: > 0 })
            .Sum(set => (decimal)set.WeightKg!.Value * (decimal)set.Reps!.Value);
}
