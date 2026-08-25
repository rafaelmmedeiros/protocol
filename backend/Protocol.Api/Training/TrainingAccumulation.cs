namespace Protocol.Api.Training;

/// <summary>
/// What each muscle group has actually accumulated, and what it is owed or has lost.
/// <para>
/// Three numbers that are easy to conflate and must not be. <b>Performed</b> is training that
/// happened. <b>Deferred</b> is volume sitting in sessions still ahead in the current queue — it
/// arrives when they do. <b>Skipped</b> is volume in sessions the user passed over, and it never
/// arrives (`ADR-032`). Adding the last two together would flatter the system in the one
/// direction that matters.
/// </para>
/// <para>
/// None of it is repaid. `TD-025` decides that and the reason survives the skip path: a catch-up
/// above target for someone who has just demonstrated less capacity than they declared is the
/// over-prescription failure `cold-start-first-block` establishes.
/// </para>
/// </summary>
public static class TrainingAccumulation
{
    /// <summary>
    /// Builds the report from every plan the user has and every workout they have imported.
    /// <para>
    /// <b>A superseded plan's untouched sessions are not skips.</b> Regenerating writes a new
    /// week and leaves the old one standing (`ADR-009`); its pending sessions were never declared
    /// anything, and counting them as lost volume would turn pressing the button into a deficit.
    /// Only a declaration counts.
    /// </para>
    /// </summary>
    public static AccumulationReport Build(
        IReadOnlyList<GeneratedWeek> plans,
        GeneratedWeek? current,
        IReadOnlyList<PerformedWorkout> performed,
        IReadOnlyDictionary<Guid, Exercise> catalogue)
    {
        var currentWorkouts = PerformedVolume.Current(performed);

        // Warm-up sets contribute nothing, and an exercise outside the catalogue credits nothing
        // because we do not know what it loads (TD-006, ADR-020). Both live in ByMuscle already,
        // which is what keeps planned and performed counted the same way.
        var performedByMuscle = PerformedVolume.ByMuscle(currentWorkouts, catalogue);

        // Monday-anchored, always, and never derived from a locale (root standard 6). The count
        // of distinct weeks with training in them is what turns a total into a rate.
        var weeksMeasured = currentWorkouts
            .Select(workout => TrainingWeek.MondayOf(workout.StartedAt))
            .Distinct()
            .Count();

        var deferred = VolumeIn(
            current?.Sessions.Where(session => session.Declared is null) ?? [],
            catalogue);

        var skipped = VolumeIn(
            plans.SelectMany(plan => plan.Sessions)
                .Where(session => session.Declared == SessionDeclaration.Skipped),
            catalogue);

        var muscles = Enum.GetValues<MuscleGroup>()
            .Select(muscle => new MuscleAccumulation(
                muscle.ToString(),
                performedByMuscle.GetValueOrDefault(muscle),
                deferred.GetValueOrDefault(muscle),
                skipped.GetValueOrDefault(muscle)))
            .Where(entry => entry.Performed > 0 || entry.Deferred > 0 || entry.Skipped > 0)
            .OrderBy(entry => entry.MuscleGroup, StringComparer.Ordinal)
            .ToList();

        return new AccumulationReport(
            weeksMeasured,
            // The target this plan was generated under, never today's constant (ADR-029). It is a
            // per-cycle figure (TD-024), which is why the rate beside it is reported separately
            // rather than folded in.
            current?.WeeklyTargetFractionalSets ?? TrainingPrescription.WeeklyTargetFractionalSets,
            muscles);
    }

    /// <summary>
    /// Fractional sets the given sessions carry, counted exactly as the generator counted them
    /// when it filled them (TD-006).
    /// </summary>
    private static Dictionary<MuscleGroup, decimal> VolumeIn(
        IEnumerable<GeneratedSession> sessions,
        IReadOnlyDictionary<Guid, Exercise> catalogue)
    {
        var slots = sessions
            .SelectMany(session => session.Prescriptions)
            .Select(prescription => (
                Exercise: catalogue.GetValueOrDefault(prescription.ExerciseId),
                prescription.Sets))
            .Where(slot => slot.Exercise is not null)
            .Select(slot => (slot.Exercise!, slot.Sets));

        return PrescribedVolume.ByMuscle(slots)
            .ToDictionary(entry => entry.Key, entry => entry.Value.Total);
    }
}

/// <summary>
/// The report as the API returns it. Every figure is a number against a target and none of them
/// is a verdict — the pattern `TD-016` sets for a shortfall and for the same reason: "rear delts
/// reached 2.0 of 6.0" is arithmetic, "your programme is inadequate" is a growth claim with
/// nothing behind it.
/// </summary>
public sealed record AccumulationReport(
    int WeeksMeasured,
    decimal TargetPerCycle,
    IReadOnlyList<MuscleAccumulation> Muscles);

/// <summary>One muscle group's standing. Enum name, never display text (root standard 3).</summary>
public sealed record MuscleAccumulation(
    string MuscleGroup,
    decimal Performed,
    decimal Deferred,
    decimal Skipped);
