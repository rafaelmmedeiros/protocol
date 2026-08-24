using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Protocol.Api.Auth;
using Protocol.Api.Training;

namespace Protocol.Api.Tests.Integration.Training;

/// <summary>
/// The catalogue is seeded by a hosted service and has no endpoint in M1, so these read the
/// context directly rather than going over HTTP.
/// <para>
/// That is a deliberate exception to this tier's testing rule. The rule exists so tests do not
/// couple to the schema and break on migrations that broke nothing; what is asserted here is
/// the seed contract itself — every row maps to Hevy, every row is performable in the assumed
/// gym — which is precisely the thing that <i>should</i> fail when it stops being true.
/// </para>
/// </summary>
public class ExerciseCatalogueTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    private async Task<List<Exercise>> LoadCatalogueAsync()
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        return await db.Exercises
            .Include(exercise => exercise.Muscles)
            .Include(exercise => exercise.Requirements)
            .AsNoTracking()
            .ToListAsync();
    }

    [Fact]
    public async Task The_catalogue_is_seeded_at_startup()
    {
        var catalogue = await LoadCatalogueAsync();

        Assert.NotEmpty(catalogue);
        Assert.Equal(ExerciseCatalogue.All.Count, catalogue.Count);
    }

    [Fact]
    public async Task Every_exercise_carries_a_hevy_template_id()
    {
        var catalogue = await LoadCatalogueAsync();

        Assert.All(catalogue, exercise =>
            Assert.False(string.IsNullOrWhiteSpace(exercise.ExternalTemplateId)));
    }

    [Fact]
    public async Task No_two_exercises_share_a_hevy_template_id()
    {
        var catalogue = await LoadCatalogueAsync();

        var duplicates = catalogue
            .GroupBy(exercise => exercise.ExternalTemplateId)
            .Where(group => group.Count() > 1)
            .Select(group => group.Key)
            .ToList();

        Assert.Empty(duplicates);
    }

    [Fact]
    public async Task Every_exercise_has_exactly_one_primary_muscle()
    {
        // Fractional volume counting (TD-006) is meaningless without this: a row with no primary
        // credits nobody 1.0, and a row with two silently doubles a muscle's weekly total.
        var catalogue = await LoadCatalogueAsync();

        Assert.All(catalogue, exercise =>
            Assert.Single(exercise.Muscles, muscle => muscle.Role == MuscleRole.Primary));
    }

    [Fact]
    public async Task The_assumed_gym_still_contains_no_selectorised_machine()
    {
        // This test used to assert the opposite thing about the catalogue: that no seeded row
        // needed a machine, because TD-004 scoped the catalogue to the gym it assumed. TD-019
        // withdrew that scoping and kept the assumption, so the guarantee moves rather than
        // disappears — to the equipment filter, which is where ADR-013 always intended it.
        //
        // What is left worth guarding is the half TD-019 did not change. Adding a machine to the
        // default is the invisible failure: the user cannot perform the prescription, improvises,
        // and the append-only history diverges from what was generated with nothing surfacing it.
        var machines = ExerciseCatalogue.AssumedGym
            .Where(item => item.ToString().EndsWith("Machine", StringComparison.Ordinal)
                || item is EquipmentItem.BackExtensionBench)
            .ToList();

        Assert.Empty(machines);

        // And the catalogue has genuinely outgrown it, which is the change itself.
        var catalogue = await LoadCatalogueAsync();
        Assert.Contains(catalogue, exercise => exercise.Equipment is Equipment.Machine);
    }

    [Fact]
    public async Task No_row_duplicates_another_in_everything_the_model_represents()
    {
        // The rule M4 curated against, written down where it can fail. A movement earns a row
        // when it differs from every existing one in its movement pattern, its implement, the
        // equipment it requires, or what it loads — never in its title (standard 9), and never
        // in an attribute TD-005 deliberately omitted.
        //
        // The fourth term is the one that is easy to leave out and wrong to: a squat and a sumo
        // squat share pattern, implement and requirements, and are separated only by the muscles
        // they load. A three-term version of this test would demand deleting one of them.
        var catalogue = await LoadCatalogueAsync();

        var duplicates = catalogue
            .GroupBy(exercise => (
                exercise.MovementPattern,
                exercise.Equipment,
                Requirements: string.Join(",", exercise.Requirements.Select(r => r.Item.ToString()).Order()),
                Muscles: string.Join(",", exercise.Muscles.Select(m => $"{m.Role}:{m.MuscleGroup}").Order())))
            .Where(group => group.Count() > 1)
            .Select(group => string.Join(" / ", group.Select(exercise => exercise.Title)))
            .ToList();

        Assert.Empty(duplicates);
    }

    [Fact]
    public async Task Every_seeded_exercise_declares_what_it_needs_to_be_performed()
    {
        // Catalogue integrity for ADR-013. A row with no requirements is unperformable by the
        // subset rule, so a forgotten curation would silently remove an exercise from every
        // week rather than failing anywhere.
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var catalogue = await db.Exercises
            .Include(exercise => exercise.Requirements)
            .AsNoTracking()
            .ToListAsync();

        Assert.All(catalogue, exercise => Assert.NotEmpty(exercise.Requirements));
        Assert.All(
            catalogue.SelectMany(exercise => exercise.Requirements),
            requirement => Assert.True(Enum.IsDefined(requirement.Item)));
    }

    [Fact]
    public async Task Seeding_again_neither_duplicates_nor_rewrites_the_catalogue()
    {
        // Identifiers a generated week references must survive a restart (root standard 7).
        var before = await LoadCatalogueAsync();

        var seeder = new ExerciseCatalogueSeeder(factory.Services, NullLogger<ExerciseCatalogueSeeder>.Instance);
        await seeder.StartAsync(CancellationToken.None);

        var after = await LoadCatalogueAsync();

        Assert.Equal(before.Count, after.Count);
        Assert.Equal(
            before.Select(exercise => exercise.Id).OrderBy(id => id),
            after.Select(exercise => exercise.Id).OrderBy(id => id));
    }
}
