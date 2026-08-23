namespace Protocol.Api.Training;

/// <summary>
/// Turns a training profile and a catalogue into a week of prescribed sessions.
/// <para>
/// A pure domain service inside the API (ADR-006): no database, no HTTP, no clock. The same
/// profile and catalogue always produce the same week (ADR-005), which is why every ordering
/// below is total and nothing is tie-broken on an identifier or a title (TD-005, root
/// standard 9).
/// </para>
/// <para>
/// It takes no training status and has no branch keyed on one. That absence is TD-001's
/// implementation: the system has observed nothing about this user, so it assumes nothing and
/// starts everyone conservative. A parameter for experience arriving here later should be sent
/// back to that record.
/// </para>
/// </summary>
public static class WeekGenerator
{
    /// <summary>
    /// Generates the week beginning on the Monday of <paramref name="reference"/>'s week.
    /// <para>
    /// The reference date is passed in rather than read from a clock, because a service that
    /// reads the time is not deterministic and cannot be asserted whole (ADR-005).
    /// </para>
    /// </summary>
    public static WeekPlan Generate(
        TrainingProfile profile,
        IReadOnlyList<Exercise> catalogue,
        DateOnly reference,
        IReadOnlySet<EquipmentItem>? owned = null,
        TrainingPreferences? preferences = null)
    {
        ArgumentNullException.ThrowIfNull(profile);
        ArgumentNullException.ThrowIfNull(catalogue);

        preferences ??= TrainingPreferences.None;

        // An exercise is performable when everything it needs is present. Nothing owned means
        // TD-004's assumed gym, so a user who never described theirs gets M1's week (ADR-013).
        var available = owned ?? ExerciseCatalogue.AssumedGym;

        // An exclusion is honoured unconditionally, including the last exercise that trains a
        // muscle. Refusing one would turn it into an unlogged skip -- a shortfall the system can
        // count becomes one it cannot, and the history records a plan nobody executed (TD-016).
        catalogue = [.. catalogue.Where(exercise =>
            !preferences.ExcludedExerciseIds.Contains(exercise.Id)
            && exercise.Requirements.All(requirement => available.Contains(requirement.Item)))];

        var split = SplitTemplate.For(profile.DaysPerWeek);
        var weekStart = WeekStartFor(reference, split); // ADR-008

        // TD-013's ladder, in order. Each rung buys time: rest first because it is 74-79% of the
        // clock and near-free for growth, sets second because they move a muscle down the volume
        // curve. The fill is capacity-bounded, so the ladder's "drop a slot" rung has no trigger
        // here -- nothing ever overflows.
        foreach (var cut in (CutLevel[])[CutLevel.None, CutLevel.RestToFloor, CutLevel.RestToFloorAndFewerSets])
        {
            var week = Build(profile, catalogue, weekStart, split, cut, preferences);
            if (week.MeetsFloor)
            {
                return week;
            }
        }

        // Every rung exhausted and some muscle is still short. The week is returned with the
        // gap named rather than refused: a shortfall the user can see beats a week that looks
        // complete and is not (TD-013, step 5).
        return Build(profile, catalogue, weekStart, split, CutLevel.RestToFloorAndFewerSets, preferences);
    }

    /// <summary>
    /// The training week starts on Monday, always. It is a periodization convention, not a
    /// calendar one, and is never derived from locale — an <c>en-US</c> week starting on Sunday
    /// must not redraw the boundaries of an existing block (root standard 6).
    /// </summary>
    private static DateOnly MondayOf(DateOnly date) =>
        date.AddDays(-DaysFromMonday(date.DayOfWeek));

    /// <summary>Monday is zero, Sunday is six — the training week's own order, not the locale's.</summary>
    private static int DaysFromMonday(DayOfWeek day) => ((int)day + 6) % 7;

    /// <summary>
    /// The week a plan can actually be trained in.
    /// <para>
    /// Anchoring to the reference date's own Monday produces a week that is mostly in the past
    /// whenever it is generated after Monday — on a Sunday, a one-day week. That is not merely
    /// untidy: the volume target and floor are <b>weekly</b> (TD-014, TD-008), so a week whose
    /// sessions cannot all still happen fails its own floor by construction, and the shortfall
    /// it reports would be blamed on the time budget rather than on the calendar.
    /// </para>
    /// <para>
    /// So the current week is used only when every day the split assigns still lies ahead;
    /// otherwise the next one. The rule comes from TD-003's templates rather than from a
    /// threshold constant, which is what makes it defensible (ADR-008).
    /// </para>
    /// </summary>
    private static DateOnly WeekStartFor(DateOnly reference, IReadOnlyList<SplitDay> split)
    {
        var monday = MondayOf(reference);
        var everyDayStillAhead = split.All(day => monday.AddDays(DaysFromMonday(day.Day)) >= reference);

        return everyDayStillAhead ? monday : monday.AddDays(7); // ADR-008
    }

    private static WeekPlan Build(
        TrainingProfile profile,
        IReadOnlyList<Exercise> catalogue,
        DateOnly weekStart,
        IReadOnlyList<SplitDay> split,
        CutLevel cut,
        TrainingPreferences preferences)
    {
        var setsPerSlot = cut == CutLevel.RestToFloorAndFewerSets
            ? TrainingPrescription.ReducedSetsPerSlot   // TD-013
            : TrainingPrescription.SetsPerSlot;         // TD-008

        var volumes = Enum.GetValues<MuscleGroup>().ToDictionary(muscle => muscle, _ => 0.0m);

        // A muscle no exercise trains directly can never reach the floor, however much is cut.
        // Separating the two keeps the ladder from chasing a catalogue gap (TD-004) as though it
        // were a time-budget one (TD-008).
        var trainable = catalogue.Select(PrimaryOf).ToHashSet();
        var uncovered = Enum.GetValues<MuscleGroup>()
            .Where(muscle => !trainable.Contains(muscle))
            .Order()
            .ToList();

        // Which sessions may train each muscle, so the weekly target can be spread across them
        // instead of being spent entirely in the first one. Without this a second Push day finds
        // chest and triceps already at target and generates nothing -- and per-muscle frequency
        // collapses to 1x, which is not what TD-003's templates are for.
        var schedule = new Dictionary<MuscleGroup, List<int>>();
        for (var index = 0; index < split.Count; index++)
        {
            foreach (var muscle in SplitTemplate.ScopeOf(split[index].Kind).Where(trainable.Contains))
            {
                (schedule.TryGetValue(muscle, out var days) ? days : schedule[muscle] = []).Add(index);
            }
        }

        var sessions = new List<PlannedSession>(split.Count);
        for (var index = 0; index < split.Count; index++)
        {
            var day = split[index];
            var slots = FillSession(
                day, index, schedule, catalogue, profile.SessionDurationSeconds, cut, setsPerSlot, volumes,
                preferences);

            // A session with nothing in it is not a training day, and emitting one is worse than
            // emitting fewer days. It happens when the available catalogue is small enough that
            // the earlier sessions already carried every trainable muscle to target -- a real
            // outcome once equipment filtering exists (ADR-013), and the honest answer is that
            // the week is finished, not that Friday is blank. Padding it would mean prescribing
            // volume above the target, which is the one thing the target is for.
            if (slots.Count == 0)
            {
                continue;
            }

            sessions.Add(new PlannedSession(sessions.Count + 1, day.Day, day.Kind, slots));
        }

        var shortfalls = volumes
            .Where(entry => trainable.Contains(entry.Key))
            .Where(entry => entry.Value < TrainingPrescription.WeeklyFloorFractionalSets) // TD-008
            .OrderBy(entry => entry.Key)
            .Select(entry => new MuscleShortfall(entry.Key, entry.Value))
            .ToList();

        return new WeekPlan(weekStart, sessions, shortfalls, uncovered, cut);
    }

    private static List<PlannedSlot> FillSession(
        SplitDay day,
        int sessionIndex,
        Dictionary<MuscleGroup, List<int>> schedule,
        IReadOnlyList<Exercise> catalogue,
        int sessionDurationSeconds,
        CutLevel cut,
        int setsPerSlot,
        Dictionary<MuscleGroup, decimal> volumes,
        TrainingPreferences preferences)
    {
        // How much of each muscle's weekly target this session is responsible for: an even share
        // across the sessions that can train it, accumulated so far. This is what makes
        // per-muscle frequency land at 2-3x rather than 1x (TD-003).
        var targets = new Dictionary<MuscleGroup, decimal>();
        foreach (var muscle in SplitTemplate.ScopeOf(day.Kind))
        {
            if (!schedule.TryGetValue(muscle, out var days))
            {
                continue;
            }

            var occurrence = days.IndexOf(sessionIndex) + 1;
            targets[muscle] = TrainingPrescription.WeeklyTargetFractionalSets * occurrence / days.Count;
        }

        var remaining = SessionTimeBudget.SlotSecondsAvailable(sessionDurationSeconds);
        var chosen = new List<Exercise>();

        while (true)
        {
            var next = NextExercise(catalogue, targets, chosen, volumes, preferences);
            if (next is null)
            {
                break;
            }

            var rest = RestFor(next.OrderClass, cut);
            var cost = SessionTimeBudget.SlotCostSeconds(next.OrderClass, setsPerSlot, rest);
            if (cost > remaining)
            {
                break;
            }

            remaining -= cost;
            chosen.Add(next);
            Credit(next, setsPerSlot, volumes);
        }

        // Within a session the sequence is free for growth, so it is ordered for technique
        // quality under fatigue and load preservation instead: heavy compounds first, isolation
        // last. A small muscle trailing the session is allowed and is not a benefit (TD-007).
        return
        [
            .. chosen
                .OrderBy(exercise => exercise.OrderClass)          // TD-007
                .ThenBy(exercise => exercise.PreferenceRank)       // TD-005
                .ThenBy(exercise => exercise.MovementPattern)
                .ThenBy(exercise => exercise.Equipment)
                .Select((exercise, position) => new PlannedSlot(
                    position + 1,
                    exercise,
                    setsPerSlot,
                    TrainingPrescription.For(exercise.OrderClass) with
                    {
                        RestSeconds = RestFor(exercise.OrderClass, cut),
                    })),
        ];
    }

    /// <summary>
    /// Selection is arithmetic, not judgement. The question is never "does this session need an
    /// isolation exercise" but "which muscle is furthest from its weekly target and what trains
    /// it" — every selection variable tested (compound versus isolation, machine versus free
    /// weight, unilateral versus bilateral, varied versus fixed) is null for whole-muscle growth
    /// once volume is equated.
    /// </summary>
    private static Exercise? NextExercise(
        IReadOnlyList<Exercise> catalogue,
        Dictionary<MuscleGroup, decimal> targets,
        List<Exercise> chosen,
        Dictionary<MuscleGroup, decimal> volumes,
        TrainingPreferences preferences)
    {
        var neediest = targets
            .Select(entry => (Muscle: entry.Key, Deficit: entry.Value - volumes[entry.Key]))
            .Where(candidate => candidate.Deficit > 0)
            .OrderByDescending(candidate => candidate.Deficit)
            .ThenBy(candidate => candidate.Muscle) // total order, so the same profile repeats
            .ToList();

        foreach (var (muscle, _) in neediest)
        {
            var exercise = catalogue
                .Where(candidate => PrimaryOf(candidate) == muscle)
                .Where(candidate => !chosen.Contains(candidate))
                // A stated preference outranks the catalogue's curated order without exception
                // (TD-016). It reorders the *draw*, which is why it sits above OrderClass here --
                // a user who wants dumbbells over a barbell is asking for the secondary compound.
                // It does not reorder the session: the slot is still placed by its own
                // OrderClass when the session is sorted (TD-007).
                .OrderByDescending(candidate => preferences.IsPreferred(candidate)) // TD-016
                .ThenBy(candidate => candidate.OrderClass)
                .ThenBy(candidate => candidate.PreferenceRank)
                .ThenBy(candidate => candidate.MovementPattern)
                .ThenBy(candidate => candidate.Equipment)
                .FirstOrDefault();

            if (exercise is not null)
            {
                return exercise;
            }
        }

        return null;
    }

    private static MuscleGroup PrimaryOf(Exercise exercise) =>
        exercise.Muscles.Single(muscle => muscle.Role == MuscleRole.Primary).MuscleGroup;

    /// <summary>
    /// Credits a slot's sets to every muscle it loads. An indirect set counts half, which is why
    /// secondary musculature has to be modelled at all: counting only direct sets would
    /// systematically under-read arm and shoulder volume on any push/pull or upper/lower
    /// template (TD-006).
    /// </summary>
    private static void Credit(Exercise exercise, int sets, Dictionary<MuscleGroup, decimal> volumes)
    {
        foreach (var muscle in exercise.Muscles)
        {
            var credit = muscle.Role == MuscleRole.Primary
                ? TrainingPrescription.PrimarySetCredit    // TD-006
                : TrainingPrescription.SecondarySetCredit; // TD-006

            volumes[muscle.MuscleGroup] += sets * credit;
        }
    }

    /// <summary>
    /// Rest is the first thing the ladder cuts and a set is the last, because between ninety and
    /// a hundred and eighty seconds the evidence is flat while cutting a set moves a muscle down
    /// a curve whose steepest region is exactly where the weekly target sits (TD-011, TD-013).
    /// </summary>
    private static int RestFor(OrderClass orderClass, CutLevel cut) => cut == CutLevel.None
        ? TrainingPrescription.For(orderClass).RestSeconds // TD-011
        : TrainingPrescription.RestFloorSeconds;          // TD-011, never below
}
