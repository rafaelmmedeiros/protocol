using Protocol.Api.Hevy;

namespace Protocol.Api.Tests.Unit.Hevy;

/// <summary>
/// TD-017, both directions. The table lives in the record; this is what holds the code to it.
/// </summary>
public class EffortConversionTests
{
    [Theory]
    // Discard the "maybe": every hedge in Hevy's wording sits on the upper value, so a half
    // point always resolves to the lower repetition count.
    [InlineData(10, 0)]     // couldn't have done more reps
    [InlineData(9.5, 0)]    // could have MAYBE done 1 more
    [InlineData(9, 1)]      // could have done 1 more
    [InlineData(8.5, 1)]    // could have done 1 more, MAYBE 2
    [InlineData(8, 2)]      // could have done 2 more
    [InlineData(7.5, 2)]    // could have done 2 more, MAYBE even 3
    [InlineData(7, 3)]      // could have done 3 more
    [InlineData(6, 4)]      // could have done 4+ more -- a floor, not a value
    public void Every_anchor_maps_to_the_reserve_TD_017_decided(double rpe, int expected)
    {
        Assert.Equal(expected, EffortConversion.ToRepsInReserve(rpe));
    }

    [Fact]
    public void A_half_point_maps_to_the_same_reserve_as_the_whole_point_below_it()
    {
        // The consequence worth stating on its own: 9.5 is not "half a reserve more than 9", it
        // is the same integer as 10. A reserve is a count of repetitions and there is no half of
        // one, which is the objection this record exists to survive.
        Assert.Equal(EffortConversion.ToRepsInReserve(10), EffortConversion.ToRepsInReserve(9.5));
        Assert.Equal(EffortConversion.ToRepsInReserve(9), EffortConversion.ToRepsInReserve(8.5));
        Assert.Equal(EffortConversion.ToRepsInReserve(8), EffortConversion.ToRepsInReserve(7.5));
    }

    [Fact]
    public void An_absent_value_yields_an_absent_reserve_and_never_zero()
    {
        // "Reported nothing" and "reported nothing left" are opposite claims about how hard the
        // user worked. Every workout read from a real account so far has rpe null on every set,
        // so this is the ordinary case rather than the edge one.
        Assert.Null(EffortConversion.ToRepsInReserve(null));
    }

    [Theory]
    [InlineData(9.25)]
    [InlineData(5)]
    [InlineData(5.5)]
    [InlineData(11)]
    [InlineData(0)]
    [InlineData(-1)]
    public void A_value_outside_Hevys_anchors_is_refused_rather_than_rounded(double rpe)
    {
        // A new anchor is a decision, not an input. Rounding an unexpected value into range
        // would let Hevy change its scale and have this system silently agree.
        Assert.Throws<ArgumentOutOfRangeException>(() => EffortConversion.ToRepsInReserve(rpe));
    }

    [Fact]
    public void No_input_can_produce_a_fractional_reserve()
    {
        // The return type forecloses it, and this asserts the intent so that widening the type
        // later has to break a test rather than a promise.
        foreach (var anchor in EffortConversion.Anchors)
        {
            var reserve = EffortConversion.ToRepsInReserve(anchor);
            Assert.NotNull(reserve);
            Assert.InRange(reserve.Value, 0, 4);
        }
    }

    [Theory]
    [InlineData(3, 7)]
    [InlineData(2, 8)]
    [InlineData(1, 9)]
    [InlineData(0, 10)]
    public void Outbound_is_exact_integer_to_integer(int repsInReserve, double expectedRpe)
    {
        // TD-018's uniform 2 writes as RPE 8. Exact in both directions at the whole points, which
        // is the only part of the table that round-trips.
        Assert.Equal(expectedRpe, EffortConversion.ToRpe(repsInReserve));
    }

    [Theory]
    [InlineData(10)]
    [InlineData(9)]
    [InlineData(8)]
    [InlineData(7)]
    public void The_whole_points_round_trip_and_only_the_whole_points_do(double rpe)
    {
        var reserve = EffortConversion.ToRepsInReserve(rpe);
        Assert.NotNull(reserve);
        Assert.Equal(rpe, EffortConversion.ToRpe(reserve.Value));
    }

    [Theory]
    [InlineData(9.5)]
    [InlineData(8.5)]
    [InlineData(7.5)]
    public void The_half_points_do_not_round_trip_and_that_is_the_cost(double rpe)
    {
        // Written as an assertion rather than left implicit: the inbound conversion is lossy on
        // purpose, and the loss is exactly here. A future session tempted to "fix" the asymmetry
        // has to delete this test to do it.
        var reserve = EffortConversion.ToRepsInReserve(rpe);
        Assert.NotNull(reserve);
        Assert.NotEqual(rpe, EffortConversion.ToRpe(reserve.Value));
    }

    [Fact]
    public void The_floor_round_trips_numerically_and_loses_its_meaning_anyway()
    {
        // 6 is a whole point, so 6 -> 4 -> 6 closes arithmetically. The loss at this anchor is
        // semantic rather than numeric: Hevy words it "4+ more reps", so it is a floor with no
        // ceiling -- a lifter with eight in reserve reports the same 6. Every other anchor is
        // wrong by at most one repetition; this one is unbounded, and it sits where the accuracy
        // literature is worst (TD-017).
        var reserve = EffortConversion.ToRepsInReserve(6);

        Assert.Equal(4, reserve);
        Assert.Equal(6, EffortConversion.ToRpe(reserve!.Value));
    }
}
