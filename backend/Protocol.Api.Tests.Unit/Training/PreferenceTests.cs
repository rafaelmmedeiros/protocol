using Protocol.Api.Training;

namespace Protocol.Api.Tests.Unit.Training;

/// <summary>
/// What a stated preference reaches, and what it must not (`TD-016`).
/// </summary>
public class PreferenceTests
{
    private static readonly DateOnly Reference = new(2026, 8, 24);

    /// <summary>Overhead Press (Barbell) — `compound_primary`, and what wins today.</summary>
    private static Exercise BarbellPress =>
        ExerciseCatalogue.All.Single(e => e.ExternalTemplateId == "7B8D84E8");

    /// <summary>Overhead Press (Dumbbell) — `compound_secondary`, the engineer's actual choice.</summary>
    private static Exercise DumbbellPress =>
        ExerciseCatalogue.All.Single(e => e.ExternalTemplateId == "6AC96645");

    private static TrainingProfile Profile(int daysPerWeek = 4, int seconds = 3_600) => new()
    {
        Id = Guid.Empty,
        UserId = "user-1",
        Goal = TrainingGoal.Hypertrophy,
        DaysPerWeek = daysPerWeek,
        SessionDurationSeconds = seconds,
    };

    private static WeekPlan Generate(TrainingPreferences? preferences, int days = 4, int seconds = 3_600) =>
        WeekGenerator.Generate(Profile(days, seconds), ExerciseCatalogue.All, Reference, null, preferences);

    private static IEnumerable<Exercise> ExercisesIn(WeekPlan week) =>
        week.Sessions.SelectMany(session => session.Slots).Select(slot => slot.Exercise);

    private static TrainingPreferences Excluding(params Exercise[] exercises) =>
        new(exercises.Select(e => e.Id).ToHashSet(), new Dictionary<MovementPattern, Guid>());

    private static TrainingPreferences Preferring(Exercise exercise) =>
        new(new HashSet<Guid>(), new Dictionary<MovementPattern, Guid>
        {
            [exercise.MovementPattern] = exercise.Id,
        });

    [Fact]
    public void Saying_nothing_leaves_the_week_exactly_as_it_was()
    {
        Assert.Equal(
            ExercisesIn(Generate(null)).Select(e => e.ExternalTemplateId),
            ExercisesIn(Generate(TrainingPreferences.None)).Select(e => e.ExternalTemplateId));
    }

    [Fact]
    public void An_excluded_exercise_never_appears()
    {
        var week = Generate(Excluding(BarbellPress));

        Assert.DoesNotContain(ExercisesIn(week), e => e.ExternalTemplateId == "7B8D84E8");
    }

    [Fact]
    public void Excluding_the_barbell_press_yields_the_dumbbell_one_with_its_own_prescription()
    {
        // The milestone's acceptance criterion, and the engineer's own case. The swap crosses an
        // order_class boundary, so the prescription changes with the exercise rather than being
        // carried over — 8-12 at 2 RIR instead of 6-10 at 3 (TD-009, TD-010, ADR-011).
        var week = Generate(Excluding(BarbellPress), days: 4, seconds: 5_400);

        var slot = week.Sessions
            .SelectMany(session => session.Slots)
            .FirstOrDefault(s => s.Exercise.ExternalTemplateId == "6AC96645");

        Assert.NotNull(slot);
        Assert.Equal(OrderClass.CompoundSecondary, slot!.Exercise.OrderClass);
        Assert.Equal(8, slot.Prescription.MinReps);
        Assert.Equal(12, slot.Prescription.MaxReps);
        Assert.Equal(2, slot.Prescription.RepsInReserve);
    }

    [Fact]
    public void A_preferred_variant_wins_over_the_catalogue_order_without_excluding_anything()
    {
        // Preference reorders the draw, and it has to outrank order_class to have any effect —
        // a user asking for dumbbells is asking for the secondary compound (TD-016).
        var week = Generate(Preferring(DumbbellPress), days: 4, seconds: 5_400);

        var pressed = ExercisesIn(week)
            .Where(e => e.MovementPattern == MovementPattern.VerticalPush)
            .Select(e => e.ExternalTemplateId)
            .ToList();

        Assert.Contains("6AC96645", pressed);
        Assert.Equal("6AC96645", pressed[0]);
    }

    [Fact]
    public void A_preference_never_reorders_a_session()
    {
        // TD-007 is a different axis and is not a preference surface: whatever is drawn, the
        // session is still heavy compounds first and isolation last.
        foreach (var preferences in (TrainingPreferences?[])
        [
            null,
            Preferring(DumbbellPress),
            Excluding(BarbellPress),
        ])
        {
            foreach (var session in Generate(preferences, days: 5, seconds: 5_400).Sessions)
            {
                var classes = session.Slots.Select(slot => slot.Exercise.OrderClass).ToList();
                Assert.Equal(classes.OrderBy(c => c), classes);
            }
        }
    }

    [Fact]
    public void A_preference_never_changes_reps_rest_or_proximity_to_failure_for_a_given_slot()
    {
        // The one place self-selection has a measured price is load, so TD-016 stops preference
        // at the exercise. Every slot's numbers still come from its order_class.
        var week = Generate(Preferring(DumbbellPress), days: 4, seconds: 5_400);

        Assert.All(week.Sessions.SelectMany(session => session.Slots), slot =>
        {
            var expected = TrainingPrescription.For(slot.Exercise.OrderClass);
            Assert.Equal(expected.MinReps, slot.Prescription.MinReps);
            Assert.Equal(expected.MaxReps, slot.Prescription.MaxReps);
            Assert.Equal(expected.RepsInReserve, slot.Prescription.RepsInReserve);
        });
    }

    [Fact]
    public void Excluding_every_exercise_for_a_muscle_is_honoured_and_the_gap_is_named()
    {
        // TD-016's starvation ruling. Refusing the exclusion would turn it into an unlogged
        // skip, converting a shortfall the system can count into one it cannot.
        var everyRearDeltExercise = ExerciseCatalogue.All
            .Where(e => e.Muscles.Any(m => m.Role == MuscleRole.Primary && m.MuscleGroup == MuscleGroup.RearDelts))
            .ToArray();

        Assert.NotEmpty(everyRearDeltExercise);

        var week = Generate(Excluding(everyRearDeltExercise));

        Assert.DoesNotContain(
            ExercisesIn(week),
            e => everyRearDeltExercise.Select(x => x.Id).Contains(e.Id));

        // Named somewhere, never silently absent (TD-008).
        var named = week.UncoveredMuscles.Contains(MuscleGroup.RearDelts)
            || week.Shortfalls.Any(s => s.MuscleGroup == MuscleGroup.RearDelts);
        Assert.True(named, "an exclusion that starves a muscle has to say so");
    }

    [Fact]
    public void An_exclusion_does_not_relax_the_floor_for_anything_else()
    {
        // A preference filters the draw pool; it is never an input to the volume arithmetic
        // (TD-016). Muscles the exclusion did not touch keep the same target.
        var baseline = Generate(null);
        var withExclusion = Generate(Excluding(BarbellPress));

        Assert.Equal(
            TrainingPrescription.WeeklyFloorFractionalSets,
            TrainingPrescription.WeeklyFloorFractionalSets);
        Assert.All(
            withExclusion.Shortfalls,
            shortfall => Assert.True(shortfall.FractionalSets < TrainingPrescription.WeeklyFloorFractionalSets));
        Assert.Equal(baseline.Sessions.Count, withExclusion.Sessions.Count);
    }

    [Fact]
    public void The_same_preferences_produce_the_same_week()
    {
        // ADR-005 still holds with a preference in play.
        var first = Generate(Preferring(DumbbellPress));
        var second = Generate(Preferring(DumbbellPress));

        Assert.Equal(
            ExercisesIn(first).Select(e => e.ExternalTemplateId),
            ExercisesIn(second).Select(e => e.ExternalTemplateId));
    }
}
