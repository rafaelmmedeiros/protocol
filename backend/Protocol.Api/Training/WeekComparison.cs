namespace Protocol.Api.Training;

/// <summary>
/// What was prescribed, beside what was performed.
/// <para>
/// The product's reason to exist, and domain rather than boundary: both sides are ours. That
/// performed training arrives through Hevy today is an import detail, and this file does not know
/// it — the join is on a column of ours that happens to hold their identifier (root standard 8).
/// </para>
/// </summary>
public static class WeekComparisonBuilder
{
    /// <summary>
    /// Builds the comparison for one stored week.
    /// </summary>
    /// <param name="week">The week as it was prescribed, immutable since it was generated (ADR-003).</param>
    /// <param name="performed">
    /// The user's imported training — every version. Only the current reading of each workout is
    /// used, so a session the user deleted upstream reads as not performed rather than as history.
    /// </param>
    public static WeekComparison Build(GeneratedWeek week, IReadOnlyList<PerformedWorkout> performed)
    {
        var current = PerformedVolume.Current(performed);

        // The one and only join (ADR-019). No title is read anywhere in this method, because a
        // title is display and a rename would silently re-bind history (standard 9).
        var byRoutine = current
            .Where(workout => workout.ExternalRoutineId is not null)
            .GroupBy(workout => workout.ExternalRoutineId!)
            .ToDictionary(
                group => group.Key,
                // If the same routine was trained more than once, the latest is the one this week
                // is being compared against. The earlier runs stay in the unbound list rather than
                // disappearing.
                group => group.OrderByDescending(workout => workout.StartedAt).First());

        var sessions = new List<SessionComparison>();
        var boundWorkoutIds = new HashSet<Guid>();

        foreach (var session in week.Sessions.OrderBy(session => session.Position))
        {
            PerformedWorkout? match = null;

            if (session.HevyRoutineId is { } routineId && byRoutine.TryGetValue(routineId, out var found))
            {
                match = found;
                boundWorkoutIds.Add(found.Id);
            }

            sessions.Add(Compare(session, match));
        }

        // Scoped to the week being compared. Listing every unbound workout ever imported turned
        // this into a dump of years of history under a single week -- 757 rows against one week's
        // three sessions. A comparison of one week answers for that week.
        var from = week.WeekStartDate.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        var until = from.AddDays(7);

        var unbound = current
            .Where(workout => !boundWorkoutIds.Contains(workout.Id))
            .Where(workout => workout.StartedAt.UtcDateTime >= from
                && workout.StartedAt.UtcDateTime < until)
            .OrderByDescending(workout => workout.StartedAt)
            .Select(workout => new UnboundWorkout(
                workout.ExternalWorkoutId,
                workout.StartedAt,
                workout.Exercises.Count))
            .ToList();

        return new WeekComparison(
            week.Id,
            week.WeekStartDate,
            sessions,
            unbound,
            // The evidence ADR-019 named as what would justify revisiting it. Reported rather than
            // assumed: if the binding rate is low in practice, that is a measurement to argue
            // with, not a hunch. Counted over the whole history on purpose -- the rate is about
            // the join, not about one week.
            new BindingCoverage(current.Count, boundWorkoutIds.Count));
    }

    private static SessionComparison Compare(GeneratedSession session, PerformedWorkout? performed)
    {
        // Consumed as slots claim them, so two slots prescribing the same exercise each take their
        // own performed entry rather than both reading the first one.
        var available = performed is null
            ? []
            : performed.Exercises.OrderBy(exercise => exercise.Position).ToList();

        var slots = new List<SlotComparison>();

        foreach (var prescription in session.Prescriptions.OrderBy(prescription => prescription.Position))
        {
            var match = available.FirstOrDefault(exercise => exercise.ExerciseId == prescription.ExerciseId);

            if (match is not null)
            {
                available.Remove(match);
            }

            slots.Add(Compare(prescription, match));
        }

        // Everything the user did that this session did not ask for. Carried rather than
        // discarded: it is training that happened, and hiding it would make the screen a claim
        // about the plan rather than a record of the day.
        var extras = available
            .Select(exercise => new ExtraExercise(
                exercise.ExternalTemplateId,
                exercise.ExternalTitle,
                exercise.ExerciseId,
                [.. exercise.Sets.OrderBy(set => set.Position).Select(ToView)]))
            .ToList();

        return new SessionComparison(
            session.Position,
            session.Day.ToString(),
            session.Kind.ToString(),
            performed is not null,
            performed?.StartedAt,
            slots,
            extras);
    }

    private static SlotComparison Compare(GeneratedPrescription prescription, PerformedExercise? performed)
    {
        var sets = performed is null
            ? []
            : performed.Sets
                .Where(set => set.Kind == SetKind.Working)   // TD-006: only working sets compare
                .OrderBy(set => set.Position)                // the sequence is the signal
                .Select(ToView)
                .ToList();

        return new SlotComparison(
            prescription.Id,
            prescription.Exercise?.Title,
            prescription.Exercise?.ExternalTemplateId,
            prescription.Sets,
            prescription.MinReps,
            prescription.MaxReps,
            prescription.RepsInReserve,
            prescription.RestSeconds,
            sets,
            Outcome(prescription, sets));
    }

    private static PerformedSetView ToView(PerformedSet set) => new(
        set.Position,
        set.WeightKg,
        set.Reps,
        // Absent stays absent (TD-017). The screen must be able to tell "reported nothing" from
        // "had nothing left", which is why this is nullable all the way to the wire.
        set.RepsInReserve);

    /// <summary>
    /// How the slot went, as a code the frontend turns into a sentence (root standard 3).
    /// <para>
    /// Deliberately not a judgement about progress. It says where the repetitions landed against
    /// the range and nothing more — what a sequence *means* is a training decision M4 has to make
    /// with a record behind it, and this read model must not pre-empt it.
    /// </para>
    /// </summary>
    private static string Outcome(GeneratedPrescription prescription, IReadOnlyList<PerformedSetView> sets)
    {
        if (sets.Count == 0)
        {
            return SlotOutcomes.NotPerformed;
        }

        var above = sets.Any(set => set.Reps > prescription.MaxReps);
        var below = sets.Any(set => set.Reps < prescription.MinReps);

        return (above, below) switch
        {
            (true, true) => SlotOutcomes.Mixed,
            (true, false) => SlotOutcomes.AboveRange,
            (false, true) => SlotOutcomes.BelowRange,
            _ => SlotOutcomes.InRange,
        };
    }
}

/// <summary>The codes a slot's outcome can take. Codes, never display text (root standard 3).</summary>
public static class SlotOutcomes
{
    public const string NotPerformed = "NotPerformed";
    public const string InRange = "InRange";
    public const string AboveRange = "AboveRange";
    public const string BelowRange = "BelowRange";
    public const string Mixed = "Mixed";
}

/// <summary>One week, prescribed and performed.</summary>
public sealed record WeekComparison(
    Guid WeekId,
    DateOnly WeekStartDate,
    IReadOnlyList<SessionComparison> Sessions,
    IReadOnlyList<UnboundWorkout> UnboundWorkouts,
    BindingCoverage Coverage);

/// <summary>One prescribed session, and the workout it was trained from if there was one.</summary>
public sealed record SessionComparison(
    int Position,
    string Day,
    string Kind,
    bool Performed,
    DateTimeOffset? PerformedAt,
    IReadOnlyList<SlotComparison> Slots,
    IReadOnlyList<ExtraExercise> Extras);

/// <summary>
/// One prescribed slot beside what was done in it.
/// <para>
/// <see cref="PerformedSets"/> is ordered and is never reduced to a total: 11/9/8 and 8/9/11 are
/// different facts about the same session, and the shape of the fall is what a progression rule
/// would have to read.
/// </para>
/// </summary>
public sealed record SlotComparison(
    Guid PrescriptionId,
    string? ExerciseTitle,
    string? ExternalTemplateId,
    int PrescribedSets,
    int MinReps,
    int MaxReps,
    int RepsInReserve,
    int RestSeconds,
    IReadOnlyList<PerformedSetView> PerformedSets,
    string Outcome);

/// <summary>One performed set. <see cref="RepsInReserve"/> is null when the user reported nothing.</summary>
public sealed record PerformedSetView(int Position, double? WeightKg, double? Reps, int? RepsInReserve);

/// <summary>An exercise the user trained that the session did not prescribe.</summary>
public sealed record ExtraExercise(
    string ExternalTemplateId,
    string? Title,
    Guid? ExerciseId,
    IReadOnlyList<PerformedSetView> Sets);

/// <summary>
/// A workout trained during this week that belongs to none of its sessions — ordinary, and
/// first-class (ADR-019). <see cref="ExerciseCount"/> counts exercises, not sets.
/// </summary>
public sealed record UnboundWorkout(string ExternalWorkoutId, DateTimeOffset StartedAt, int ExerciseCount);

/// <summary>
/// How much imported history the join actually reaches.
/// <para>
/// ADR-019 chose the narrow join and named this number as the evidence that would justify
/// revisiting it. Reporting it is what makes that an argument from data rather than a hunch.
/// </para>
/// </summary>
public sealed record BindingCoverage(int ImportedWorkouts, int BoundWorkouts);
