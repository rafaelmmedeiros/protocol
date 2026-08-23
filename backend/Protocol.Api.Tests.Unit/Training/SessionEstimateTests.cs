using Protocol.Api.Training;

namespace Protocol.Api.Tests.Unit.Training;

/// <summary>
/// The duration a session is expected to take. Its point is not convenience: two of the terms
/// behind it are engineering estimates with no source (`TD-012`), and showing the number is what
/// makes them falsifiable in use.
/// </summary>
public class SessionEstimateTests
{
    private static readonly DateOnly Reference = new(2026, 8, 24);

    private static TrainingProfile Profile(int daysPerWeek, int seconds) => new()
    {
        Id = Guid.Empty,
        UserId = "user-1",
        Goal = TrainingGoal.Hypertrophy,
        DaysPerWeek = daysPerWeek,
        SessionDurationSeconds = seconds,
    };

    private static WeekPlan Generate(int days, int seconds) =>
        WeekGenerator.Generate(Profile(days, seconds), ExerciseCatalogue.All, Reference);

    /// <summary>Mirrors what the API computes from stored prescriptions.</summary>
    private static int Estimate(PlannedSession session) =>
        SessionTimeBudget.WarmUpSeconds
        + session.Slots.Sum(slot => SessionTimeBudget.SlotCostSeconds(
            slot.Prescription.MinReps,
            slot.Prescription.MaxReps,
            slot.Sets,
            slot.Prescription.RestSeconds));

    [Fact]
    public void A_slot_costs_its_sets_its_rest_and_the_transition_after_it()
    {
        // Three sets of 6-10 with three minutes between them: 3 x 24 s of work, two rests, and
        // the walk to the next exercise.
        var expected = (3 * 24) + (2 * 180) + SessionTimeBudget.TransitionSeconds;

        Assert.Equal(expected, SessionTimeBudget.SlotCostSeconds(6, 10, 3, 180));
        // The order_class overload has to agree with it, or a stored week and a planned one
        // would price differently.
        Assert.Equal(expected, SessionTimeBudget.SlotCostSeconds(OrderClass.CompoundPrimary, 3, 180));
    }

    [Fact]
    public void Rest_after_the_final_set_is_charged_once_as_the_transition_and_not_twice()
    {
        var oneSet = SessionTimeBudget.SlotCostSeconds(10, 15, 1, 90);

        // One set has no inter-set rest at all: work plus the transition, nothing else.
        Assert.Equal((15 + 10) * SessionTimeBudget.RepetitionSeconds / 2 + 60, oneSet);
    }

    [Theory]
    [InlineData(2, 1_500)]
    [InlineData(3, 2_400)]
    [InlineData(4, 3_600)]
    [InlineData(5, 3_000)]
    [InlineData(6, 5_400)]
    public void No_session_is_estimated_to_take_longer_than_the_user_said_they_have(
        int days,
        int seconds)
    {
        // The acceptance criterion. The generator fills against this budget, so a session that
        // over-runs it means the fill and the estimate disagree — which would make the number
        // on screen worse than no number at all.
        var week = Generate(days, seconds);

        Assert.All(week.Sessions, session => Assert.True(
            Estimate(session) <= seconds,
            $"{days}d/{seconds}s: session {session.Position} estimated {Estimate(session)}s"));
    }

    [Fact]
    public void The_estimate_uses_what_was_prescribed_rather_than_what_the_record_says_today()
    {
        // A week is immutable (ADR-003) and the records behind it are append-only. Pricing a
        // stored week from TD-011's *current* rest values would re-price it under rules it was
        // never generated under, so the estimate reads the numbers written down.
        var slotAtFloorRest = SessionTimeBudget.SlotCostSeconds(6, 10, 3, 90);
        var slotAtPrescribedRest = SessionTimeBudget.SlotCostSeconds(6, 10, 3, 180);

        Assert.True(slotAtFloorRest < slotAtPrescribedRest);
        Assert.Equal(2 * (180 - 90), slotAtPrescribedRest - slotAtFloorRest);
    }

    [Fact]
    public void A_longer_session_is_estimated_longer_than_a_shorter_one()
    {
        var shorter = Generate(3, 2_400);
        var longer = Generate(3, 5_400);

        Assert.True(
            longer.Sessions.Sum(Estimate) > shorter.Sessions.Sum(Estimate),
            "ninety minutes should be estimated to fill more of the week than forty");
    }
}
