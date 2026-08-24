using Protocol.Api.Training;

namespace Protocol.Api.Tests.Unit.Training;

/// <summary>
/// An exercise is performable when its requirements are a subset of what the user owns
/// (ADR-013). These are the cases that model exists for.
/// </summary>
public class EquipmentFilterTests
{
    private static readonly DateOnly Reference = new(2026, 8, 24);

    private static TrainingProfile Profile(int daysPerWeek = 4, int seconds = 3_600) => new()
    {
        Id = Guid.Empty,
        UserId = "user-1",
        Goal = TrainingGoal.Hypertrophy,
        DaysPerWeek = daysPerWeek,
        SessionDurationSeconds = seconds,
    };

    private static WeekPlan Generate(IReadOnlySet<EquipmentItem>? owned, int days = 4, int seconds = 3_600) =>
        WeekGenerator.Generate(Profile(days, seconds), ExerciseCatalogue.All, Reference, owned);

    private static IEnumerable<Exercise> ExercisesIn(WeekPlan week) =>
        week.Sessions.SelectMany(session => session.Slots).Select(slot => slot.Exercise);

    /// <summary>An exercise that exists only for this test, requiring items the catalogue does not yet use.</summary>
    private static Exercise Requiring(params EquipmentItem[] items) => new()
    {
        Id = Guid.CreateVersion7(),
        ExternalTemplateId = "TEST0001",
        Title = "A machine movement",
        MovementPattern = MovementPattern.KneeFlexion,
        Mechanic = Mechanic.Isolation,
        Equipment = Equipment.Machine,
        OrderClass = OrderClass.Isolation,
        Laterality = Laterality.Bilateral,
        PreferenceRank = 1,
        Requirements = [.. items.Select(item => new ExerciseRequirement { Item = item })],
        Muscles = [new ExerciseMuscle { MuscleGroup = MuscleGroup.Hamstrings, Role = MuscleRole.Primary }],
    };

    [Fact]
    public void A_machine_item_filters_exactly_like_any_other()
    {
        // The vocabulary grew by eighteen machines (ADR-022) before any catalogue row requires one,
        // so this proves the new values are usable rather than merely declared. It builds its own
        // exercise: waiting for S4.3 to add rows would leave the vocabulary untested in the step
        // that introduced it.
        var movement = Requiring(EquipmentItem.SeatedLegCurlMachine);

        Assert.False(ExerciseCatalogue.AssumedGym.Contains(EquipmentItem.SeatedLegCurlMachine));

        var withoutIt = ExerciseCatalogue.AssumedGym;
        var withIt = new HashSet<EquipmentItem>(ExerciseCatalogue.AssumedGym)
        {
            EquipmentItem.SeatedLegCurlMachine,
        };

        Assert.False(movement.Requirements.All(r => withoutIt.Contains(r.Item)));
        Assert.True(movement.Requirements.All(r => withIt.Contains(r.Item)));
    }

    [Fact]
    public void Owning_one_machine_does_not_imply_owning_another()
    {
        // The whole reason ADR-022 named machines individually instead of grouping them. A gym with
        // a leg press and no leg curl is ordinary, and a coarser vocabulary would have asserted the
        // second -- the invisible failure TD-019 is built around.
        var legPress = Requiring(EquipmentItem.LegPressMachine);
        var legCurl = Requiring(EquipmentItem.SeatedLegCurlMachine);

        var gym = new HashSet<EquipmentItem>(ExerciseCatalogue.AssumedGym) { EquipmentItem.LegPressMachine };

        Assert.True(legPress.Requirements.All(r => gym.Contains(r.Item)));
        Assert.False(legCurl.Requirements.All(r => gym.Contains(r.Item)));
    }

    [Fact]
    public void The_assumed_gym_gained_no_machine()
    {
        // TD-019 keeps the default lean and says so at the line: machines reach a user by
        // derivation or description, never by assumption. This is that sentence, enforced.
        Assert.DoesNotContain(
            ExerciseCatalogue.AssumedGym,
            item => item.ToString().EndsWith("Machine", StringComparison.Ordinal));
    }

    [Fact]
    public void Every_catalogue_row_declares_what_it_needs()
    {
        // An empty requirement set cannot be told apart from a row nobody curated, which is why
        // a bodyweight movement carries EquipmentItem.Bodyweight explicitly (ADR-013).
        Assert.All(ExerciseCatalogue.All, exercise => Assert.NotEmpty(exercise.Requirements));
    }

    [Fact]
    public void The_assumed_gym_reproduces_the_week_M1_would_have_generated()
    {
        // The milestone's own criterion: describing a gym that matches TD-004 must add nothing
        // and remove nothing.
        var withDefault = Generate(null);
        var withAssumedGym = Generate(ExerciseCatalogue.AssumedGym);

        Assert.Equal(
            ExercisesIn(withDefault).Select(exercise => exercise.ExternalTemplateId),
            ExercisesIn(withAssumedGym).Select(exercise => exercise.ExternalTemplateId));
    }

    [Fact]
    public void An_exercise_is_withheld_when_a_second_thing_it_needs_is_missing()
    {
        // The failure ADR-010 could not express: the barbell is owned, the bench is not, and a
        // barbell bench press is still impossible.
        var noBench = new HashSet<EquipmentItem>
        {
            EquipmentItem.Bodyweight,
            EquipmentItem.Barbell,
            EquipmentItem.WeightPlates,
            EquipmentItem.SquatRack,
        };

        var week = Generate(noBench);

        Assert.DoesNotContain(
            ExercisesIn(week),
            exercise => exercise.Requirements.Any(r => r.Item == EquipmentItem.Bench));
        // ...and the barbell work that needs no bench is still there.
        Assert.Contains(ExercisesIn(week), exercise => exercise.Equipment == Equipment.Barbell);
    }

    [Fact]
    public void An_incline_movement_needs_more_than_a_bench()
    {
        // AdjustableBench is held separately rather than implying Bench, so that an inclined
        // movement requires both and no implication rule has to exist (ADR-013).
        var flatBenchOnly = new HashSet<EquipmentItem>
        {
            EquipmentItem.Bodyweight,
            EquipmentItem.Barbell,
            EquipmentItem.WeightPlates,
            EquipmentItem.Bench,
        };

        var week = Generate(flatBenchOnly);

        Assert.DoesNotContain(
            ExercisesIn(week),
            exercise => exercise.Requirements.Any(r => r.Item == EquipmentItem.AdjustableBench));
        Assert.Contains(ExercisesIn(week), exercise => exercise.ExternalTemplateId == "79D0BB3A");
    }

    [Fact]
    public void A_small_home_gym_still_produces_a_week_and_names_what_it_cannot_cover()
    {
        // One barbell, plates, a rack and a bench. The point of ADR-013: this has to produce a
        // real week rather than nothing, and say plainly what it cannot reach.
        var homeGym = new HashSet<EquipmentItem>
        {
            EquipmentItem.Bodyweight,
            EquipmentItem.Barbell,
            EquipmentItem.WeightPlates,
            EquipmentItem.SquatRack,
            EquipmentItem.Bench,
        };

        var week = Generate(homeGym, days: 3);

        Assert.All(week.Sessions, session => Assert.NotEmpty(session.Slots));
        Assert.All(
            ExercisesIn(week),
            exercise => Assert.All(
                exercise.Requirements,
                requirement => Assert.Contains(requirement.Item, homeGym)));

        // Side delts, rear delts and calves are the muscles that only direct slots reach, and a
        // barbell-and-bench gym has few of those. Whatever it cannot cover is reported, not
        // silently absent (TD-008).
        Assert.True(
            week.UncoveredMuscles.Count > 0 || week.Shortfalls.Count > 0 || week.MeetsFloor,
            "a week must either meet the floor or say which muscles it could not");
    }

    [Fact]
    public void Owning_almost_nothing_yields_a_week_that_says_so_rather_than_throwing()
    {
        var bodyweightOnly = new HashSet<EquipmentItem> { EquipmentItem.Bodyweight };

        var week = Generate(bodyweightOnly, days: 3);

        Assert.NotEmpty(week.Sessions);
        Assert.NotEmpty(week.UncoveredMuscles);
        Assert.All(
            ExercisesIn(week),
            exercise => Assert.All(
                exercise.Requirements,
                requirement => Assert.Equal(EquipmentItem.Bodyweight, requirement.Item)));
    }

    [Fact]
    public void A_generated_week_never_contains_an_empty_training_day()
    {
        // Found by the home-gym case: with a small catalogue the early sessions can carry every
        // trainable muscle to target, leaving a later day with nothing to do. Fewer days is an
        // honest answer; a blank Friday is not.
        foreach (var owned in (IReadOnlySet<EquipmentItem>[])
        [
            ExerciseCatalogue.AssumedGym,
            new HashSet<EquipmentItem>
            {
                EquipmentItem.Bodyweight, EquipmentItem.Barbell,
                EquipmentItem.WeightPlates, EquipmentItem.SquatRack, EquipmentItem.Bench,
            },
            new HashSet<EquipmentItem> { EquipmentItem.Bodyweight },
        ])
        {
            foreach (var days in (int[])[2, 3, 4, 5, 6])
            {
                var week = Generate(owned, days);
                Assert.All(week.Sessions, session => Assert.NotEmpty(session.Slots));
                Assert.Equal(
                    Enumerable.Range(1, week.Sessions.Count),
                    week.Sessions.Select(session => session.Position));
            }
        }
    }

    [Fact]
    public void The_vocabulary_holds_nothing_the_catalogue_does_not_ask_for()
    {
        // A value with no exercise behind it is a checkbox that does nothing, which is worse
        // than an absent one (ADR-013).
        var required = ExerciseCatalogue.All
            .SelectMany(exercise => exercise.Requirements)
            .Select(requirement => requirement.Item)
            .ToHashSet();

        Assert.Equal(Enum.GetValues<EquipmentItem>().ToHashSet(), required);
    }
}
