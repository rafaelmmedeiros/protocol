using Protocol.Api.Training;

namespace Protocol.Api.Tests.Unit.Hevy;

/// <summary>
/// Equipment inferred from what was actually trained (ADR-020).
/// </summary>
public class DerivedEquipmentTests
{
    private static IReadOnlyDictionary<Guid, Exercise> Catalogue() =>
        ExerciseCatalogue.All.ToDictionary(exercise => exercise.Id);

    /// <summary>
    /// A gym narrower than the assumed one — a barbell, plates, a rack and a bench, which is the
    /// engineer's own "someone with little doing a lot" example.
    /// <para>
    /// The suggestions path needs this, and the reason is worth knowing: **every exercise in the
    /// catalogue is performable in the assumed gym**, because the catalogue was built for it
    /// (TD-004). So a user on the default set can never receive a suggestion — only a user who
    /// narrowed their gym can, and the real account's `Iso-Lateral Row (Machine)` arrives as a
    /// catalogue gap rather than as a suggestion.
    /// </para>
    /// </summary>
    private static IReadOnlySet<EquipmentItem> ASmallGym() =>
        new HashSet<EquipmentItem>
        {
            EquipmentItem.Bodyweight,
            EquipmentItem.Barbell,
            EquipmentItem.WeightPlates,
            EquipmentItem.Bench,
            EquipmentItem.SquatRack,
        };

    /// <summary>An exercise that small gym cannot perform.</summary>
    private static Exercise SomethingTheSmallGymCannotDo() =>
        ExerciseCatalogue.All.First(exercise =>
            exercise.Requirements.Any(requirement => !ASmallGym().Contains(requirement.Item)));

    private static PerformedWorkout AWorkout(
        Exercise? exercise,
        DateTimeOffset? at = null,
        string? externalTemplateId = null,
        string? title = null) => new()
    {
        Id = Guid.CreateVersion7(),
        UserId = "user-1",
        ExternalWorkoutId = $"w-{Guid.NewGuid():N}",
        StartedAt = at ?? new DateTimeOffset(2026, 8, 20, 15, 0, 0, TimeSpan.Zero),
        EndedAt = at ?? new DateTimeOffset(2026, 8, 20, 16, 0, 0, TimeSpan.Zero),
        ExternallyUpdatedAt = at ?? new DateTimeOffset(2026, 8, 20, 16, 0, 0, TimeSpan.Zero),
        Version = 1,
        Exercises =
        [
            new PerformedExercise
            {
                Position = 0,
                ExerciseId = exercise?.Id,
                ExternalTemplateId = externalTemplateId ?? exercise?.ExternalTemplateId ?? "UNKNOWN",
                ExternalTitle = title ?? exercise?.Title,
                Sets = [new PerformedSet { Position = 0, Kind = SetKind.Working, WeightKg = 50, Reps = 10 }],
            },
        ],
    };

    [Fact]
    public void An_exercise_the_gym_cannot_perform_produces_a_suggestion()
    {
        // The acceptance criterion. Note what it takes to reach it: the user must have a gym
        // narrower than the assumed one, because every catalogue exercise fits the assumed gym.
        var exercise = SomethingTheSmallGymCannotDo();

        var result = DerivedEquipment.From(
            [AWorkout(exercise)],
            Catalogue(),
            ASmallGym(),
            new HashSet<EquipmentItem>());

        Assert.NotEmpty(result.Suggestions);
        Assert.All(result.Suggestions, suggestion =>
            Assert.False(ASmallGym().Contains(Enum.Parse<EquipmentItem>(suggestion.Item))));
    }

    [Fact]
    public void A_suggestion_cites_the_exercise_that_implied_it_and_when()
    {
        // A suggestion the user cannot audit is an assertion.
        var exercise = SomethingTheSmallGymCannotDo();
        var at = new DateTimeOffset(2026, 8, 18, 14, 0, 0, TimeSpan.Zero);

        var suggestion = DerivedEquipment
            .From([AWorkout(exercise, at)], Catalogue(), ASmallGym(), new HashSet<EquipmentItem>())
            .Suggestions[0];

        Assert.Equal(exercise.Title, suggestion.ImpliedByTitle);
        Assert.Equal(exercise.ExternalTemplateId, suggestion.ImpliedByExternalTemplateId);
        Assert.Equal(at, suggestion.LastTrainedAt);
    }

    [Fact]
    public void Nothing_the_user_already_has_is_suggested()
    {
        // Which, on the default set, is every exercise in the catalogue -- so the suggestions
        // path is silent for a user who never narrowed their gym, by construction rather than by
        // accident.
        var exercise = ExerciseCatalogue.All.First(e =>
            e.Requirements.All(r => ExerciseCatalogue.AssumedGym.Contains(r.Item)));

        var result = DerivedEquipment.From(
            [AWorkout(exercise)],
            Catalogue(),
            ExerciseCatalogue.AssumedGym,
            new HashSet<EquipmentItem>());

        Assert.Empty(result.Suggestions);
    }

    [Fact]
    public void A_declined_item_is_not_offered_again()
    {
        // A suggestion that keeps returning is one the user learns to dismiss without reading,
        // which costs the feature its only job (ADR-020).
        var exercise = SomethingTheSmallGymCannotDo();

        var offered = DerivedEquipment
            .From([AWorkout(exercise)], Catalogue(), ASmallGym(), new HashSet<EquipmentItem>())
            .Suggestions
            .Select(suggestion => Enum.Parse<EquipmentItem>(suggestion.Item))
            .ToHashSet();

        var afterDeclining = DerivedEquipment.From(
            [AWorkout(exercise)], Catalogue(), ASmallGym(), offered);

        Assert.Empty(afterDeclining.Suggestions);
    }

    [Fact]
    public void Absence_from_the_history_never_removes_anything()
    {
        // The rule that kills the worst failure outright: not training something is far more often
        // evidence it was never programmed than evidence the equipment is gone. This function can
        // only ever return additions, and there is no removal path to test because there is none.
        var result = DerivedEquipment.From(
            [],
            Catalogue(),
            ExerciseCatalogue.AssumedGym,
            new HashSet<EquipmentItem>());

        Assert.Empty(result.Suggestions);
        Assert.Empty(result.CatalogueGaps);
    }

    [Fact]
    public void An_exercise_outside_our_catalogue_implies_no_equipment_and_is_reported_as_a_gap()
    {
        // There is no requirement set to read, so it implies nothing. A gap in the catalogue
        // rather than in the gym, and surfaced rather than silently ignored (TD-004).
        var result = DerivedEquipment.From(
            [AWorkout(exercise: null, externalTemplateId: "AA1EB7D8", title: "Iso-Lateral Row (Machine)")],
            Catalogue(),
            ExerciseCatalogue.AssumedGym,
            new HashSet<EquipmentItem>());

        Assert.Empty(result.Suggestions);

        var gap = Assert.Single(result.CatalogueGaps);
        Assert.Equal("AA1EB7D8", gap.ExternalTemplateId);
        Assert.Equal("Iso-Lateral Row (Machine)", gap.Title);
    }

    [Fact]
    public void The_same_item_implied_twice_is_one_suggestion_carrying_the_later_date()
    {
        var exercise = SomethingTheSmallGymCannotDo();
        var early = new DateTimeOffset(2026, 8, 10, 12, 0, 0, TimeSpan.Zero);
        var late = new DateTimeOffset(2026, 8, 20, 12, 0, 0, TimeSpan.Zero);

        var result = DerivedEquipment.From(
            [AWorkout(exercise, early), AWorkout(exercise, late)],
            Catalogue(),
            ASmallGym(),
            new HashSet<EquipmentItem>());

        Assert.All(result.Suggestions, suggestion => Assert.Equal(late, suggestion.LastTrainedAt));
        Assert.Equal(result.Suggestions.Select(s => s.Item).Distinct().Count(), result.Suggestions.Count);
    }

    [Fact]
    public void Suggestions_lead_with_what_was_trained_most_recently()
    {
        var result = DerivedEquipment.From(
            [.. ExerciseCatalogue.All
                .Where(e => e.Requirements.Any(r => !ASmallGym().Contains(r.Item)))
                .Select((e, index) => AWorkout(e, new DateTimeOffset(2026, 8, 1, 0, 0, 0, TimeSpan.Zero).AddDays(index)))],
            Catalogue(),
            ASmallGym(),
            new HashSet<EquipmentItem>());

        Assert.Equal(
            result.Suggestions.OrderByDescending(s => s.LastTrainedAt).Select(s => s.Item),
            result.Suggestions.Select(s => s.Item));
    }
}
