using System.Globalization;
using Protocol.Api.Training;

namespace Protocol.Api.Tests.Unit.Training;

/// <summary>
/// The generator, asserted without a database or a clock — which is the point of it being a
/// pure domain service (ADR-006).
/// </summary>
public class WeekGeneratorTests
{
    private static TrainingProfile Profile(
        int daysPerWeek,
        int seconds = 3_600,
        SplitTemplateId? split = null) => new()
    {
        Id = Guid.Empty,
        UserId = "user-1",
        Goal = TrainingGoal.Hypertrophy,
        DaysPerWeek = daysPerWeek,
        SessionDurationSeconds = seconds,
        Split = split,
    };

    private static WeekPlan Generate(int daysPerWeek, int seconds = 3_600, SplitTemplateId? split = null) =>
        WeekGenerator.Generate(Profile(daysPerWeek, seconds, split), ExerciseCatalogue.All);

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

            // Asserted on the measurement window rather than on the plan: ADR-027 took the date
            // off the prescription, and root standard 6 moved with the question it answers.
            // Sunday 2026-08-30 belongs to the week beginning Monday 2026-08-24 in every locale,
            // including the ones whose calendar week starts on Sunday.
            Assert.Equal(
                new DateOnly(2026, 8, 24),
                TrainingWeek.MondayOf(new DateOnly(2026, 8, 30)));
        }
        finally
        {
            CultureInfo.CurrentCulture = original;
        }
    }

    [Fact]
    public void A_plan_is_an_ordered_queue_with_no_dates()
    {
        // What ADR-027 replaced three anchoring tests with. A session has a position and the
        // queue decides what is next; nothing here carries a weekday, and the generator no
        // longer takes a reference date at all.
        foreach (var days in (int[])[2, 3, 4, 5, 6])
        {
            var plan = Generate(days);

            Assert.Equal(
                Enumerable.Range(1, plan.Sessions.Count),
                plan.Sessions.Select(session => session.Position));
        }
    }

    [Fact]
    public void The_same_profile_still_produces_the_same_plan()
    {
        // Determinism survived losing the date, and is now stronger: the plan cannot differ by
        // when it was asked for, which is what a reference parameter always risked (ADR-005).
        var first = Generate(5);
        var second = Generate(5);

        Assert.Equal(
            first.Sessions.SelectMany(session => session.Slots).Select(Describe),
            second.Sessions.SelectMany(session => session.Slots).Select(Describe));
    }

    [Theory]
    // A Monday, a Sunday, and the Sunday's own week -- the three cases the deleted anchoring
    // tests worried about, now asked of the measurement window instead of the plan.
    [InlineData("2026-08-24", "2026-08-24")]
    [InlineData("2026-08-30", "2026-08-24")]
    [InlineData("2026-08-31", "2026-08-31")]
    public void Performed_training_still_buckets_into_Monday_anchored_weeks(string date, string monday)
    {
        // Root standard 6 outlived ADR-008. The prescription stopped being a calendar week; what
        // was performed is still measured over one, and never over a locale's idea of one.
        var parsed = DateOnly.Parse(date, CultureInfo.InvariantCulture);

        Assert.Equal(DateOnly.Parse(monday, CultureInfo.InvariantCulture), TrainingWeek.MondayOf(parsed));
        Assert.Equal(DayOfWeek.Monday, TrainingWeek.MondayOf(parsed).DayOfWeek);
    }

    [Fact]
    public void Bucketing_reads_an_instant_in_UTC_rather_than_locally()
    {
        // Sunday 23:30 in UTC-03:00 is Monday 02:30 UTC, and the two answers are different
        // weeks. Training is stored in UTC (root standard 5), so the bucket is read there --
        // otherwise a session moves between weeks depending on where it was logged.
        var lateSunday = new DateTimeOffset(2026, 8, 30, 23, 30, 0, TimeSpan.FromHours(-3));

        Assert.Equal(new DateOnly(2026, 8, 31), TrainingWeek.MondayOf(lateSunday));
    }

    [Fact]
    public void A_six_session_plan_runs_its_template_in_order()
    {
        // What replaced the weekday assertion: the shape a template declares is the order the
        // queue hands out, and nothing about it is a date (ADR-027, TD-023).
        var plan = Generate(6);

        Assert.Equal(
            [SessionKind.Push, SessionKind.Pull, SessionKind.Legs,
             SessionKind.Push, SessionKind.Pull, SessionKind.Legs],
            plan.Sessions.Select(session => session.Kind));
    }

    [Fact]
    public void The_same_profile_produces_an_identical_week()
    {
        // ADR-005. Asserted on the whole structure rather than a count, because a generator
        // that varied only in exercise choice would still pass a count.
        var first = Generate(4);
        var second = Generate(4);

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

    [Theory]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    [InlineData(5)]
    [InlineData(6)]
    public void The_same_profile_produces_the_same_week_whatever_order_the_catalogue_arrives_in(int days)
    {
        // ADR-005 says generation is deterministic for a given profile. This asserts it against
        // the input order, which is cheap and worth having.
        //
        // **It does not reproduce the failure that prompted it**, and that is worth knowing: in
        // production the same profile alternated between two plans, so ADR-009's guard -- which
        // only refuses a week identical to the current one -- never fired, and five weeks were
        // written in fifteen seconds. Reversing an in-memory catalogue does not provoke it,
        // because the comparator already separates the two exercises involved. The guard that
        // matches the failure is the integration test that generates repeatedly through the API,
        // where the catalogue comes from a real query.
        var forwards = Generate(days);
        var backwards = WeekGenerator.Generate(
            Profile(days),
            [.. ExerciseCatalogue.All.Reverse()],
            ExerciseCatalogue.AssumedGym);

        Assert.Equal(
            Shape(forwards),
            Shape(backwards));
    }

    /// <summary>Everything about a week that a regeneration is expected to reproduce exactly.</summary>
    private static string Shape(WeekPlan plan) =>
        string.Join(
            "|",
            plan.Sessions.SelectMany(session => session.Slots.Select(slot =>
                $"{session.Position}:{session.Kind}:{slot.Exercise.ExternalTemplateId}"
                    + $":{slot.Sets}:{slot.Prescription.MinReps}-{slot.Prescription.MaxReps}")));

    [Fact]
    public void Every_slot_is_prescribed_at_two_reps_in_reserve()
    {
        // TD-018: two everywhere, never to failure, never 0 RIR. Asserting equality rather than
        // a floor is the point — a floor of two would still pass if TD-010's withdrawn gradient
        // crept back in on the primary compound.
        foreach (var days in (int[])[2, 3, 4, 5, 6])
        {
            Assert.All(
                Generate(days).Sessions.SelectMany(session => session.Slots),
                slot => Assert.Equal(2, slot.Prescription.RepsInReserve));
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
        // TD-004 assumes no selectorised machines, so some groups have no direct exercise. They
        // are a catalogue failure, reported separately from a time-budget one, because no amount
        // of cutting closes them.
        //
        // Forearms used to be the third name here and left in M4: TD-020 seeded a wrist curl,
        // which needs only a barbell and a bench and so is reachable in the assumed gym. The
        // remaining two are what TD-019 knowingly left open for a user who has neither synced
        // their history nor described their gym.
        var week = Generate(4, 3_600);

        Assert.Equal(
            [MuscleGroup.SpinalErectors, MuscleGroup.Adductors],
            week.UncoveredMuscles.Order());
        Assert.DoesNotContain(
            week.Shortfalls.Select(shortfall => shortfall.MuscleGroup),
            muscle => week.UncoveredMuscles.Contains(muscle));
    }

    [Fact]
    public void The_assumed_gym_week_actually_prescribes_a_direct_forearm_exercise()
    {
        // The behavioural half of TD-020. Forearms leaving UncoveredMuscles only proves the
        // catalogue can train it; this proves the generator spends a slot on it, which is the
        // change the engineer agreed to and the cost the record predicts.
        var week = Generate(4, 3_600);

        var direct = week.Sessions
            .SelectMany(session => session.Slots)
            .Where(slot => slot.Exercise.Muscles.Any(muscle =>
                muscle.Role == MuscleRole.Primary && muscle.MuscleGroup == MuscleGroup.Forearms))
            .ToList();

        Assert.NotEmpty(direct);
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

    [Fact]
    public void No_muscle_group_ever_exceeds_the_ceiling()
    {
        // The assertion TD-022 exists because of. Three implementations of the band passed a
        // reading of the record and failed this: a three-set phase-2 slot landed muscles at 10.5,
        // bounding only the primary muscle left them at 10.5 anyway because indirect credit
        // piled up, and topping each session up before the later sessions had taken their
        // guaranteed volume left ten of fifteen groups at 9.0. Only a week-wide second pass with
        // the ceiling bounding every credited muscle holds. Before TD-021 the worst case across
        // this whole grid was 7.5, so a regression here reads as "the band leaks", not as noise.
        foreach (var days in (int[])[2, 3, 4, 5, 6])
        {
            foreach (var minutes in (int[])[25, 30, 40, 45, 60, 75, 90, 120])
            {
                var week = Generate(days, minutes * 60);

                var over = VolumeByMuscle(week)
                    .Where(entry => entry.Value > TrainingPrescription.WeeklyCeilingFractionalSets)
                    .ToList();

                Assert.True(
                    over.Count == 0,
                    $"{days}x{minutes} exceeded the ceiling: "
                    + string.Join(", ", over.Select(entry => $"{entry.Key}:{entry.Value}")));
            }
        }
    }

    [Fact]
    public void Ninety_minutes_buys_more_volume_than_forty_at_five_days()
    {
        // The blind spot that let session duration ship inert. Its sibling above runs at three
        // days a week, the one frequency where forty minutes genuinely binds -- so it passed
        // green while fifty through a hundred and twenty minutes produced identical weeks at
        // every other frequency.
        var shorter = Generate(5, 2_400);
        var longer = Generate(5, 5_400);

        Assert.True(
            TotalSets(longer) > TotalSets(shorter),
            $"expected more sets at 5x90; got {TotalSets(longer)} against {TotalSets(shorter)}");
    }

    [Fact]
    public void Three_sessions_of_forty_minutes_stay_below_the_ceiling()
    {
        // TD-014's protected case, pinned by a test rather than by argument. At 3x40 the clock
        // binds before the ceiling does, so the week reaches the guaranteed target and no more --
        // which is the whole reason TD-021 raised a ceiling instead of raising the target.
        var week = Generate(3, 2_400);

        Assert.True(week.MeetsFloor, "3x40 should still reach TD-008's floor");
        Assert.True(
            VolumeByMuscle(week).Values.Max() < TrainingPrescription.WeeklyCeilingFractionalSets,
            "3x40 has no minutes to spare and must not reach the ceiling");
    }

    [Fact]
    public void A_week_with_time_to_spare_mixes_full_and_ceiling_slots()
    {
        // The visible consequence of TD-022: a session holds three-set slots taken for the
        // guaranteed target beside two-set slots bought above it. Asserted because a reader who
        // does not know that reads the two-set slot as a cut week (TD-013) instead.
        var slots = Generate(5, 3_600).Sessions.SelectMany(session => session.Slots).ToList();

        Assert.Contains(slots, slot => slot.Sets == TrainingPrescription.SetsPerSlot);
        Assert.Contains(slots, slot => slot.Sets == TrainingPrescription.CeilingSetsPerSlot);
    }

    /// <summary>
    /// What each muscle group finishes the week holding, primary sets whole and secondary sets
    /// half (TD-006). Recomputed here from the slots rather than read off the plan, so the
    /// assertion does not trust the same arithmetic it is checking.
    /// </summary>
    private static Dictionary<MuscleGroup, decimal> VolumeByMuscle(WeekPlan week)
    {
        var volumes = Enum.GetValues<MuscleGroup>().ToDictionary(muscle => muscle, _ => 0.0m);

        foreach (var slot in week.Sessions.SelectMany(session => session.Slots))
        {
            foreach (var muscle in slot.Exercise.Muscles)
            {
                volumes[muscle.MuscleGroup] += slot.Sets * (muscle.Role == MuscleRole.Primary
                    ? TrainingPrescription.PrimarySetCredit
                    : TrainingPrescription.SecondarySetCredit);
            }
        }

        return volumes;
    }

    [Fact]
    public void A_slot_credits_its_primary_whole_and_its_secondaries_half()
    {
        // Exact numbers from one slot rather than a recomputation of the same sum, which would
        // assert nothing. Three sets: 3.0 to the primary, 1.5 to every secondary (TD-006).
        var exercise = ExerciseCatalogue.All.First(e => e.Muscles.Any(m => m.Role == MuscleRole.Secondary));
        var primary = exercise.Muscles.Single(m => m.Role == MuscleRole.Primary).MuscleGroup;

        var volumes = PrescribedVolume.ByMuscle([(exercise, 3)]);

        Assert.Equal(3.0m, volumes[primary].Direct);
        Assert.Equal(0m, volumes[primary].Indirect);

        foreach (var secondary in exercise.Muscles.Where(m => m.Role == MuscleRole.Secondary))
        {
            Assert.Equal(0m, volumes[secondary.MuscleGroup].Direct);
            Assert.Equal(1.5m, volumes[secondary.MuscleGroup].Indirect);
        }
    }

    [Fact]
    public void The_two_halves_accumulate_separately_across_slots()
    {
        var exercise = ExerciseCatalogue.All.First(e => e.Muscles.Any(m => m.Role == MuscleRole.Secondary));
        var primary = exercise.Muscles.Single(m => m.Role == MuscleRole.Primary).MuscleGroup;
        var secondary = exercise.Muscles.First(m => m.Role == MuscleRole.Secondary).MuscleGroup;

        var volumes = PrescribedVolume.ByMuscle([(exercise, 3), (exercise, 2)]);

        Assert.Equal(5.0m, volumes[primary].Direct);
        Assert.Equal(2.5m, volumes[secondary].Indirect);
        Assert.Equal(5.0m, volumes[primary].Total);
    }

    [Fact]
    public void A_generated_week_loads_at_least_one_muscle_both_ways()
    {
        // The split would be pointless if no muscle ever received both halves -- and a bug that
        // folded them together would still pass the exact-number tests above.
        var week = Generate(4);

        var volumes = PrescribedVolume.ByMuscle(
            week.Sessions.SelectMany(s => s.Slots).Select(slot => (slot.Exercise, slot.Sets)));

        Assert.Contains(volumes.Values, v => v.Direct > 0 && v.Indirect > 0);
    }

    [Fact]
    public void A_profile_that_never_chose_a_split_generates_what_it_always_did()
    {
        // The property that makes ADR-030's nullable column safe: null is not an unset field, it
        // is "whatever this frequency maps to", and that mapping is TD-003's answer unchanged.
        foreach (var days in (int[])[2, 3, 4, 5, 6])
        {
            var unchosen = Generate(days);
            var explicitDefault = Generate(days, split: SplitTemplate.Default(days));

            Assert.Equal(
                unchosen.Sessions.Select(session => session.Kind),
                explicitDefault.Sessions.Select(session => session.Kind));
        }
    }

    [Fact]
    public void A_chosen_split_overrides_the_default()
    {
        var standard = Generate(5);
        var chosen = Generate(5, split: SplitTemplateId.UpperLowerPushPullLegs);

        // U/L/U/L/Full against U/L/Push/Pull/Legs -- same frequency, different shape.
        Assert.Equal(
            [SessionKind.Upper, SessionKind.Lower, SessionKind.Upper, SessionKind.Lower, SessionKind.FullBody],
            standard.Sessions.Select(session => session.Kind));

        Assert.Equal(
            [SessionKind.Upper, SessionKind.Lower, SessionKind.Push, SessionKind.Pull, SessionKind.Legs],
            chosen.Sessions.Select(session => session.Kind));
    }

    [Fact]
    public void A_stored_split_the_frequency_no_longer_admits_falls_back_rather_than_throwing()
    {
        // Reachable only through a row whose frequency changed without its split -- the endpoint
        // rejects that combination. Falling back beats generating from a template whose session
        // count no longer matches the frequency, which is the property TD-024 leans on (ADR-030).
        var week = Generate(3, split: SplitTemplateId.UpperLowerPushPullLegs);

        Assert.Equal(3, week.Sessions.Count);
        Assert.Equal(
            SplitTemplate.For(SplitTemplate.Default(3)).Select(day => day.Kind),
            week.Sessions.Select(session => session.Kind));
    }

    [Fact]
    public void Every_admitted_template_holds_as_many_sessions_as_the_frequency_declares()
    {
        // TD-024's central claim is that a cycle *is* the declared week, and it rests entirely on
        // this. Nothing else enforces it: a template with a different session count would break
        // the dose window silently.
        foreach (var days in (int[])[2, 3, 4, 5, 6])
        {
            foreach (var template in SplitTemplate.Admitted(days))
            {
                Assert.Equal(days, SplitTemplate.For(template).Count);
            }
        }
    }

    private static int TotalSets(WeekPlan week) =>
        week.Sessions.SelectMany(session => session.Slots).Sum(slot => slot.Sets);

    private static string Describe(PlannedSlot slot) =>
        $"{slot.Position}:{slot.Exercise.ExternalTemplateId}:{slot.Sets}:" +
        $"{slot.Prescription.MinReps}-{slot.Prescription.MaxReps}:" +
        $"{slot.Prescription.RepsInReserve}:{slot.Prescription.RestSeconds}";
}
