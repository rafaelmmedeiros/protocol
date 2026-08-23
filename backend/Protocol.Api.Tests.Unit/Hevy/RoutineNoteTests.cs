using Protocol.Api.Hevy;
using Protocol.Api.Training;

namespace Protocol.Api.Tests.Unit.Hevy;

/// <summary>
/// The one line of display text this backend composes (ADR-016).
/// </summary>
public class RoutineNoteTests
{
    private static GeneratedPrescription APrescription() => new()
    {
        Position = 1,
        ExerciseId = Guid.NewGuid(),
        Sets = 3,
        MinReps = 8,
        MaxReps = 12,
        RepsInReserve = 2,
        RestSeconds = 150,
    };

    [Theory]
    [InlineData("en-US")]
    [InlineData("pt-BR")]
    public void The_note_carries_the_range_and_the_reserve_in_both_locales(string locale)
    {
        var note = RoutineNotes.For(APrescription(), locale);

        Assert.Contains("8-12", note);
        Assert.Contains("2", note);
    }

    [Fact]
    public void The_two_locales_differ()
    {
        // The cheap check that pt-BR is a translation rather than a copy of the default. It has
        // caught nothing yet and exists for the day someone adds a locale by duplicating a block.
        Assert.NotEqual(
            RoutineNotes.For(APrescription(), "en-US"),
            RoutineNotes.For(APrescription(), "pt-BR"));
    }

    [Theory]
    [InlineData("en-US", "slows")]
    [InlineData("pt-BR", "velocidade")]
    public void The_note_tells_the_user_to_terminate_on_effort(string locale, string expected)
    {
        // The whole reason this sentence exists, asserted rather than trusted to survive an edit.
        // A fixed target censors the observation: a lifter who stops at twelve because the plan
        // says twelve cannot log more than twelve, and the count then carries nothing about the
        // effort behind it. This wording is what keeps the imported log readable.
        Assert.Contains(expected, RoutineNotes.For(APrescription(), locale), StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData("en-US", "chase")]
    [InlineData("pt-BR", "perseguir")]
    public void The_note_says_the_range_is_where_the_set_should_land(string locale, string expected)
    {
        Assert.Contains(expected, RoutineNotes.For(APrescription(), locale), StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("fr-FR")]
    [InlineData("klingon")]
    public void An_unsupported_locale_falls_back_rather_than_failing(string? locale)
    {
        // A push must not fail because a client sent a locale we do not translate. en-US is the
        // product's default (root standard 2), and a routine in the wrong language is recoverable
        // where a failed push is not.
        Assert.Equal(
            RoutineNotes.For(APrescription(), "en-US"),
            RoutineNotes.For(APrescription(), locale));
    }

    [Fact]
    public void The_locale_is_matched_without_regard_to_case()
    {
        Assert.Equal(
            RoutineNotes.For(APrescription(), "pt-BR"),
            RoutineNotes.For(APrescription(), "pt-br"));
    }
}
