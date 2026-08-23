using Protocol.Api.Training;

namespace Protocol.Api.Tests.Unit.Training;

/// <summary>
/// The bounds every generated week stands on, tested without a container because
/// <see cref="TrainingProfileRules"/> takes no dependency on I/O.
/// </summary>
public class TrainingProfileTests
{
    private const int ValidDuration = 3_600;

    [Fact]
    public void A_supported_profile_is_accepted()
    {
        Assert.Null(TrainingProfileRules.Validate(TrainingGoal.Hypertrophy, 4, ValidDuration));
    }

    [Theory]
    [InlineData(TrainingGoal.Strength)]
    [InlineData(TrainingGoal.WeightLoss)]
    [InlineData(TrainingGoal.Endurance)]
    public void Any_goal_other_than_hypertrophy_is_rejected(TrainingGoal goal)
    {
        // The schema accepts these from the first migration (ADR-004); the API does not, until
        // a decision record covers them.
        var error = TrainingProfileRules.Validate(goal, 4, ValidDuration);

        Assert.NotNull(error);
        Assert.Equal(TrainingErrorCodes.GoalNotSupported, error!.Code);
    }

    [Theory]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    [InlineData(5)]
    [InlineData(6)]
    public void Every_frequency_TD_002_supports_is_accepted(int daysPerWeek)
    {
        Assert.Null(TrainingProfileRules.Validate(TrainingGoal.Hypertrophy, daysPerWeek, ValidDuration));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(7)]
    [InlineData(8)]
    public void A_frequency_outside_the_supported_range_is_rejected(int daysPerWeek)
    {
        // 1 and 7 are named explicitly rather than covered by "out of range": TD-002 rejects
        // them for different reasons, and both are values a real user would try.
        var error = TrainingProfileRules.Validate(TrainingGoal.Hypertrophy, daysPerWeek, ValidDuration);

        Assert.NotNull(error);
        Assert.Equal(TrainingErrorCodes.FrequencyOutOfRange, error!.Code);
        Assert.Equal(TrainingProfileRules.MinDaysPerWeek, error.Min);
        Assert.Equal(TrainingProfileRules.MaxDaysPerWeek, error.Max);
    }

    [Theory]
    [InlineData(1_500)] // 25 minutes, the floor
    [InlineData(2_400)] // 40 minutes
    [InlineData(7_200)] // 120 minutes, the ceiling
    public void Every_duration_TD_012_supports_is_accepted(int seconds)
    {
        Assert.Null(TrainingProfileRules.Validate(TrainingGoal.Hypertrophy, 3, seconds));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1_499)]
    [InlineData(7_201)]
    public void A_duration_outside_the_supported_range_is_rejected(int seconds)
    {
        var error = TrainingProfileRules.Validate(TrainingGoal.Hypertrophy, 3, seconds);

        Assert.NotNull(error);
        Assert.Equal(TrainingErrorCodes.DurationOutOfRange, error!.Code);
        Assert.Equal(TrainingProfileRules.MinSessionDurationSeconds, error.Min);
        Assert.Equal(TrainingProfileRules.MaxSessionDurationSeconds, error.Max);
    }

    [Fact]
    public void An_unsupported_goal_is_reported_before_any_other_problem()
    {
        // A profile for a goal we do not programme has no defensible bounds at all, so the
        // frequency and duration ranges are not the useful thing to tell the caller about.
        var error = TrainingProfileRules.Validate(TrainingGoal.Strength, 99, 99);

        Assert.Equal(TrainingErrorCodes.GoalNotSupported, error!.Code);
    }

    [Theory]
    [InlineData("Hypertrophy")]
    [InlineData("hypertrophy")]
    [InlineData("HYPERTROPHY")]
    public void A_goal_parses_regardless_of_casing(string goal)
    {
        Assert.True(TrainingProfileRules.TryParseGoal(goal, out var parsed));
        Assert.Equal(TrainingGoal.Hypertrophy, parsed);
    }

    [Theory]
    [InlineData("powerlifting")]
    [InlineData("")]
    [InlineData(null)]
    [InlineData("7")]
    public void A_goal_this_product_does_not_know_fails_to_parse(string? goal)
    {
        // Including "7": Enum.TryParse accepts numeric strings for any underlying value, which
        // would otherwise smuggle an undefined goal past the parser.
        Assert.False(TrainingProfileRules.TryParseGoal(goal, out _));
    }
}
