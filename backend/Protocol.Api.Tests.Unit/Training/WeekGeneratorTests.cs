using System.Globalization;
using Protocol.Api.Training;

namespace Protocol.Api.Tests.Unit.Training;

/// <summary>
/// The generator, asserted without a database or a clock — which is the point of it being a
/// pure domain service (ADR-006).
/// </summary>
public class WeekGeneratorTests
{
    /// <summary>A Wednesday, chosen so the Monday anchoring is not accidentally satisfied.</summary>
    private static readonly DateOnly Reference = new(2026, 8, 26);

    private static readonly DateOnly ExpectedMonday = new(2026, 8, 24);

    private static TrainingProfile Profile(int daysPerWeek, int seconds = 3_600) => new()
    {
        Id = Guid.Empty,
        UserId = "user-1",
        Goal = TrainingGoal.Hypertrophy,
        DaysPerWeek = daysPerWeek,
        SessionDurationSeconds = seconds,
    };

    private static WeekPlan Generate(int daysPerWeek, int seconds = 3_600) =>
        WeekGenerator.Generate(Profile(daysPerWeek, seconds), ExerciseCatalogue.All, Reference);

    [Theory]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    [InlineData(5)]
    [InlineData(6)]
    public void A_week_is_generated_for_every_supported_frequency(int daysPerWeek)
    {
        var week = Generate(daysPerWeek);

        Assert.Equal(daysPerWeek, week.Sessions.Count);
        Assert.All(week.Sessions, session => Assert.NotEmpty(session.Slots));
        Assert.Equal(
            Enumerable.Range(1, daysPerWeek),
            week.Sessions.Select(session => session.Position));
    }

    [Fact]
    public void The_split_at_each_frequency_is_the_one_TD_003_decided()
    {
        Assert.Equal(
            [SessionKind.FullBody, SessionKind.FullBody],
            Generate(2).Sessions.Select(s => s.Kind));

        Assert.Equal(
            [SessionKind.Upper, SessionKind.Lower, SessionKind.Upper, SessionKind.Lower],
            Generate(4).Sessions.Select(s => s.Kind));

        Assert.Equal(
            [SessionKind.Push, SessionKind.Pull, SessionKind.Legs,
             SessionKind.Push, SessionKind.Pull, SessionKind.Legs],
            Generate(6).Sessions.Select(s => s.Kind));
    }

    [Fact]
    public void The_week_starts_on_Monday_and_the_first_session_is_Mondays()
    {
        var week = Generate(4);

        Assert.Equal(ExpectedMonday, week.WeekStartDate);
        Assert.Equal(DayOfWeek.Monday, week.WeekStartDate.DayOfWeek);
        Assert.Equal(DayOfWeek.Monday, week.Sessions[0].Day);
    }

    [Theory]
    [InlineData("en-US")] // starts its calendar week on Sunday
    [InlineData("pt-BR")]
    public void The_week_starts_on_Monday_regardless_of_locale(string culture)
    {
        // Root standard 6: the training week is a periodization convention, never a calendar
        // one. An en-US week starting on Sunday must not redraw a block's boundaries.
        var original = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = new CultureInfo(culture);
            Assert.Equal(ExpectedMonday, Generate(3).WeekStartDate);
        }
        finally
        {
            CultureInfo.CurrentCulture = original;
        }
    }

    [Fact]
    public void Every_day_of_a_generated_week_falls_on_or_after_its_Monday()
    {
        var week = Generate(6);

        Assert.All(week.Sessions, session =>
            Assert.True(session.Day >= DayOfWeek.Monday || session.Day == DayOfWeek.Sunday));
        Assert.Equal(
            [DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday,
             DayOfWeek.Thursday, DayOfWeek.Friday, DayOfWeek.Saturday],
            week.Sessions.Select(session => session.Day));
    }

    [Fact]
    public void The_same_profile_produces_an_identical_week()
    {
        // ADR-005. Asserted on the whole structure rather than a count, because a generator
        // that varied only in exercise choice would still pass a count.
        var first = Generate(4);
        var second = Generate(4);

        Assert.Equal(first.WeekStartDate, second.WeekStartDate);
        Assert.Equal(first.CutApplied, second.CutApplied);
        Assert.Equal(
            first.Sessions.SelectMany(s => s.Slots).Select(Describe),
            second.Sessions.SelectMany(s => s.Slots).Select(Describe));
    }

    [Fact]
    public void Forty_minutes_and_ninety_minutes_produce_different_weekly_volumes()
    {
        // The plan's criterion: different availability must produce different volume, not the
        // same volume redistributed.
        var shorter = Generate(3, 2_400);
        var longer = Generate(3, 5_400);

        Assert.True(
            TotalSets(longer) > TotalSets(shorter),
            $"expected more sets at 90 minutes; got {TotalSets(longer)} against {TotalSets(shorter)}");
    }

    [Fact]
    public void Three_sessions_of_forty_minutes_and_five_of_fifty_differ_in_volume()
    {
        // The configurations the milestone plan names by hand.
        var three = Generate(3, 2_400);
        var five = Generate(5, 3_000);

        Assert.True(
            TotalSets(five) > TotalSets(three),
            $"expected more sets at 5x50; got {TotalSets(five)} against {TotalSets(three)}");
    }

    [Fact]
    public void Rest_differs_between_slots_within_one_session()
    {
        // TD-011 makes rest a property of the slot, not of the session or of the user (ADR-007).
        var session = Generate(4, 5_400).Sessions[0];

        var restIntervals = session.Slots.Select(slot => slot.Prescription.RestSeconds).Distinct();

        Assert.True(restIntervals.Count() > 1, "expected more than one rest interval in a session");
    }

    [Fact]
    public void No_slot_ever_rests_below_the_floor()
    {
        // Sixty seconds is the one interval the acute evidence argues against, so the ladder
        // stops at ninety and takes a set instead (TD-011).
        foreach (var days in (int[])[2, 3, 4, 5, 6])
        {
            var week = Generate(days, 1_500);
            Assert.All(
                week.Sessions.SelectMany(session => session.Slots),
                slot => Assert.True(
                    slot.Prescription.RestSeconds >= TrainingPrescription.RestFloorSeconds,
                    $"{slot.Exercise.ExternalTemplateId} rested {slot.Prescription.RestSeconds}s"));
        }
    }

    [Fact]
    public void No_slot_is_ever_prescribed_to_failure()
    {
        // TD-010: never to failure, never 0 RIR. Reaching failure adds nothing detectable and
        // costs a day of fatigue.
        foreach (var days in (int[])[2, 3, 4, 5, 6])
        {
            Assert.All(
                Generate(days).Sessions.SelectMany(session => session.Slots),
                slot => Assert.True(slot.Prescription.RepsInReserve >= 2));
        }
    }

    [Fact]
    public void Slots_are_ordered_heavy_compound_first_and_isolation_last()
    {
        // TD-007. Order is free for growth, so this is a technique-and-safety convention -- but
        // it is a total order and the generator must apply it.
        foreach (var session in Generate(5, 5_400).Sessions)
        {
            var classes = session.Slots.Select(slot => slot.Exercise.OrderClass).ToList();
            Assert.Equal(classes.OrderBy(c => c), classes);
        }
    }

    [Fact]
    public void No_session_repeats_an_exercise()
    {
        foreach (var session in Generate(3, 5_400).Sessions)
        {
            var ids = session.Slots.Select(slot => slot.Exercise.ExternalTemplateId).ToList();
            Assert.Equal(ids.Count, ids.Distinct().Count());
        }
    }

    [Fact]
    public void A_tight_budget_climbs_the_cut_ladder_before_reporting_a_shortfall()
    {
        // TD-013's ladder is not an edge case: at modest availability it runs in full.
        var tight = Generate(2, 1_500);
        var generous = Generate(6, 5_400);

        Assert.True(tight.CutApplied >= generous.CutApplied);
        Assert.Equal(CutLevel.None, generous.CutApplied);
    }

    [Fact]
    public void A_shortfall_names_the_muscle_rather_than_being_swallowed()
    {
        // TD-008: a muscle below the floor is a surfaced coverage failure, never a silent one.
        var week = Generate(2, 1_500);

        Assert.All(week.Shortfalls, shortfall =>
            Assert.True(shortfall.FractionalSets < TrainingPrescription.WeeklyFloorFractionalSets));
        Assert.Equal(week.Shortfalls.Count == 0, week.MeetsFloor);
    }

    [Fact]
    public void No_session_exceeds_the_per_session_volume_cap()
    {
        // TD-008's cap does not bind at supported volumes. Asserted as a guard so that a future
        // volume rise cannot cross Remmert's ceiling silently.
        foreach (var session in Generate(2, 7_200).Sessions)
        {
            var perMuscle = new Dictionary<MuscleGroup, decimal>();
            foreach (var slot in session.Slots)
            {
                foreach (var muscle in slot.Exercise.Muscles)
                {
                    var credit = muscle.Role == MuscleRole.Primary
                        ? TrainingPrescription.PrimarySetCredit
                        : TrainingPrescription.SecondarySetCredit;
                    perMuscle[muscle.MuscleGroup] =
                        perMuscle.GetValueOrDefault(muscle.MuscleGroup) + (slot.Sets * credit);
                }
            }

            Assert.All(perMuscle, entry =>
                Assert.True(entry.Value <= TrainingPrescription.PerSessionCapFractionalSets));
        }
    }

    [Fact]
    public void The_catalogue_still_supports_the_arithmetic_TD_014_rests_on()
    {
        // TD-014 chose 6.0 using an estimate of ~4.5 fractional credits per slot, made before
        // the catalogue existed, and says the conclusion holds across 4.0-6.0. This recomputes
        // it from the real catalogue so that a change pushing it out of range fails here rather
        // than silently invalidating the record.
        var totalCredits = ExerciseCatalogue.All.Sum(exercise =>
            TrainingPrescription.SetsPerSlot * exercise.Muscles.Sum(muscle =>
                muscle.Role == MuscleRole.Primary
                    ? TrainingPrescription.PrimarySetCredit
                    : TrainingPrescription.SecondarySetCredit));

        var creditsPerSlot = totalCredits / ExerciseCatalogue.All.Count;

        Assert.InRange(creditsPerSlot, 4.0m, 6.0m);
    }

    [Fact]
    public void An_ordinary_profile_reaches_the_floor_without_cutting_anything()
    {
        // Four sessions of an hour is the configuration the product should serve best. If this
        // ever needs the ladder, either the time model or the weekly target is wrong.
        var week = Generate(4, 3_600);

        Assert.Equal(CutLevel.None, week.CutApplied);
        Assert.True(week.MeetsFloor, $"short: {string.Join(", ", week.Shortfalls)}");
    }

    [Fact]
    public void The_muscles_the_assumed_gym_cannot_train_are_named_and_do_not_drive_the_ladder()
    {
        // TD-004 assumes no selectorised machines, so three groups have no direct exercise.
        // They are a catalogue failure, reported separately from a time-budget one, because no
        // amount of cutting closes them.
        var week = Generate(4, 3_600);

        Assert.Equal(
            [MuscleGroup.Forearms, MuscleGroup.SpinalErectors, MuscleGroup.Adductors],
            week.UncoveredMuscles.Order());
        Assert.DoesNotContain(
            week.Shortfalls.Select(shortfall => shortfall.MuscleGroup),
            muscle => week.UncoveredMuscles.Contains(muscle));
    }

    [Fact]
    public void Every_muscle_the_catalogue_trains_is_worked_at_least_twice_a_week()
    {
        // TD-003's templates exist to land per-muscle frequency at 2-3x. A generator that spent
        // a muscle's whole weekly target in one session would satisfy the volume target and
        // still be wrong.
        //
        // Counted over any credited role, not just primary: under TD-006 an indirect set is
        // stimulus worth half, and front delts and triceps are expected to reach target largely
        // through it. Counting only direct slots would assert something the records do not ask
        // for -- and would fail on exactly the muscles the fractional scheme is designed around.
        var week = Generate(4, 3_600);

        var sessionsPerMuscle = new Dictionary<MuscleGroup, int>();
        foreach (var session in week.Sessions)
        {
            foreach (var muscle in session.Slots
                .SelectMany(slot => slot.Exercise.Muscles)
                .Select(muscle => muscle.MuscleGroup)
                .Distinct())
            {
                sessionsPerMuscle[muscle] = sessionsPerMuscle.GetValueOrDefault(muscle) + 1;
            }
        }

        Assert.All(sessionsPerMuscle, entry =>
            Assert.True(entry.Value >= 2, $"{entry.Key} was trained {entry.Value}x"));
    }

    private static int TotalSets(WeekPlan week) =>
        week.Sessions.SelectMany(session => session.Slots).Sum(slot => slot.Sets);

    private static string Describe(PlannedSlot slot) =>
        $"{slot.Position}:{slot.Exercise.ExternalTemplateId}:{slot.Sets}:" +
        $"{slot.Prescription.MinReps}-{slot.Prescription.MaxReps}:" +
        $"{slot.Prescription.RepsInReserve}:{slot.Prescription.RestSeconds}";
}
