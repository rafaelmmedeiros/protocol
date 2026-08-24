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
    /// The suggestions path needs this, and the reason has changed since it was written. Under
    /// TD-004 every catalogue exercise was performable in the assumed gym, so only a user who
    /// *narrowed* their gym could ever receive a suggestion. TD-019 withdrew that scoping in M4 and
    /// the catalogue now holds machines the assumed gym does not, so the default user can receive
    /// suggestions too. A gym narrower than the default is still the sharpest way to test the path,
    /// which is why it stays.
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
        // The acceptance criterion, tested from a gym narrower than the default so the
        // suggestion is unambiguous rather than incidental to M4's machine rows (TD-019).
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

    // ---- Coverage as a proportion (S4.5) -----------------------------------------------------

    private static PerformedWorkout ATombstone(string externalId) => new()
    {
        Id = Guid.CreateVersion7(),
        UserId = "user-1",
        ExternalWorkoutId = externalId,
        StartedAt = new DateTimeOffset(2026, 8, 20, 15, 0, 0, TimeSpan.Zero),
        EndedAt = new DateTimeOffset(2026, 8, 20, 16, 0, 0, TimeSpan.Zero),
        ExternallyUpdatedAt = new DateTimeOffset(2026, 8, 21, 16, 0, 0, TimeSpan.Zero),
        Version = 2,
        IsDeleted = true,
        Exercises = [],
    };

    [Fact]
    public void The_report_says_how_much_of_the_training_the_catalogue_explains()
    {
        // A list of twenty names reads the same whether it covers 3% of someone's training or 73%.
        // The counts are what tell those apart, and they count logged entries rather than distinct
        // movements — one movement trained 162 times weighs 162 here and once in
        // TotalCatalogueGaps.
        var known = ExerciseCatalogue.All.First();

        var result = DerivedEquipment.From(
            [
                AWorkout(known),
                AWorkout(known),
                AWorkout(null, externalTemplateId: "GHOST-1", title: "Something we do not model"),
            ],
            Catalogue(),
            ASmallGym(),
            new HashSet<EquipmentItem>());

        Assert.Equal(2, result.ExplainedExercises);
        Assert.Equal(1, result.UnexplainedExercises);
        Assert.Equal(1, result.TotalCatalogueGaps);
    }

    [Fact]
    public void The_proportion_is_computed_from_current_readings_only()
    {
        // Root standard 7: nothing is deleted to make this true. A workout the user removed
        // upstream keeps every row it ever had and simply stops being the reading that counts.
        // PerformedVolume.Current is what applies that, and the caller applies it — so this test
        // runs the pair together, because the failure it guards against is a caller that forgot.
        var known = ExerciseCatalogue.All.First();

        var live = AWorkout(known);
        var removed = AWorkout(null, externalTemplateId: "GHOST-2", title: "Trained, then deleted");

        var readings = PerformedVolume.Current([live, removed, ATombstone(removed.ExternalWorkoutId)]);

        var result = DerivedEquipment.From(
            readings,
            Catalogue(),
            ASmallGym(),
            new HashSet<EquipmentItem>());

        Assert.Equal(1, result.ExplainedExercises);
        Assert.Equal(0, result.UnexplainedExercises);
        Assert.Empty(result.CatalogueGaps);
    }

    [Fact]
    public void A_movement_that_earned_a_row_in_M4_is_no_longer_a_gap()
    {
        // The milestone's own criterion, asserted against a real logged movement rather than a
        // fixture: the seated leg curl was the most-trained gap on the first import (162 times)
        // and TD-019 is what let it into the catalogue.
        var legCurl = ExerciseCatalogue.All.Single(exercise => exercise.ExternalTemplateId == "11A123F3");

        var result = DerivedEquipment.From(
            [AWorkout(legCurl)],
            Catalogue(),
            ASmallGym(),
            new HashSet<EquipmentItem>());

        Assert.Empty(result.CatalogueGaps);
        Assert.Equal(1, result.ExplainedExercises);

        // And it arrives as a suggestion instead, which is the circle TD-019 broke: before M4 a
        // logged machine implied nothing, because no machine existed to carry a requirement.
        Assert.Contains(
            result.Suggestions,
            suggestion => suggestion.Item == EquipmentItem.SeatedLegCurlMachine.ToString());
    }
}
