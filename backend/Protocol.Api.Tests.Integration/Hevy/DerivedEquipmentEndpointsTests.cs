using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using Protocol.Api.Hevy;
using Protocol.Api.Training;

namespace Protocol.Api.Tests.Integration.Hevy;

/// <summary>
/// Equipment the history reveals, offered and answered (ADR-020).
/// </summary>
public class DerivedEquipmentEndpointsTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    private const string Password = "Passw0rd!";

    private StubHevyClient Hevy => (StubHevyClient)factory.Services.GetRequiredService<IHevyClient>();

    /// <summary>A barbell, plates, a rack and a bench — the "little doing a lot" gym.</summary>
    private static readonly string[] SmallGym =
        ["Bodyweight", "Barbell", "WeightPlates", "Bench", "SquatRack"];

    private async Task<HttpClient> ConnectedAsync(string[]? equipment = null)
    {
        var email = $"{Guid.NewGuid():N}@protocol.test";
        var client = factory.CreateClient();
        await client.PostAsJsonAsync("/auth/register", new { email, password = Password });
        await client.PostAsJsonAsync("/auth/login?useCookies=true", new { email, password = Password });
        await client.PutAsJsonAsync("/hevy/connection", new { apiKey = StubHevyClient.ValidKey });

        if (equipment is not null)
        {
            await client.PutAsJsonAsync("/training/equipment", new { items = equipment });
        }

        return client;
    }

    /// <summary>A logged workout of one of our exercises, by its external key.</summary>
    private static HevyWorkoutEvent Logged(string externalTemplateId, string title, DateTimeOffset at) =>
        new(
            "updated",
            new HevyWorkout($"w-{Guid.NewGuid():N}", "Session", null, null, at, at.AddHours(1),
                at.AddHours(1), at,
                [
                    new HevyWorkoutExercise(0, title, null, externalTemplateId, null,
                    [
                        new HevyWorkoutSet(0, "normal", 50, 10, null, null, null, null),
                    ]),
                ]),
            null,
            null);

    private static Exercise OutsideTheSmallGym()
    {
        var small = SmallGym.Select(Enum.Parse<EquipmentItem>).ToHashSet();
        return ExerciseCatalogue.All.First(exercise =>
            exercise.Requirements.Any(requirement => !small.Contains(requirement.Item)));
    }

    private static async Task<EquipmentSuggestions?> SuggestionsAsync(HttpClient client) =>
        await client.GetFromJsonAsync<EquipmentSuggestions>("/training/equipment/suggestions");

    private static async Task<EquipmentResponse?> EquipmentAsync(HttpClient client) =>
        await client.GetFromJsonAsync<EquipmentResponse>("/training/equipment");

    [Fact]
    public async Task Suggestions_are_unreachable_without_a_session()
    {
        Assert.Equal(
            HttpStatusCode.Unauthorized,
            (await factory.CreateClient().GetAsync("/training/equipment/suggestions")).StatusCode);
    }

    [Fact]
    public async Task A_user_with_no_history_is_offered_nothing()
    {
        var client = await ConnectedAsync();

        var suggestions = await SuggestionsAsync(client);

        Assert.NotNull(suggestions);
        Assert.Empty(suggestions.Suggestions);
        Assert.Empty(suggestions.CatalogueGaps);
    }

    [Fact]
    public async Task An_exercise_the_gym_cannot_perform_is_suggested_with_its_evidence()
    {
        Hevy.Forget();
        var client = await ConnectedAsync(SmallGym);
        var exercise = OutsideTheSmallGym();

        Hevy.Events.Add(Logged(
            exercise.ExternalTemplateId,
            exercise.Title,
            new DateTimeOffset(2026, 8, 20, 15, 0, 0, TimeSpan.Zero)));

        await client.PostAsync("/hevy/sync", null);

        var suggestions = await SuggestionsAsync(client);

        Assert.NotEmpty(suggestions!.Suggestions);
        Assert.All(suggestions.Suggestions, suggestion =>
            Assert.Equal(exercise.Title, suggestion.ImpliedByTitle));
    }

    [Fact]
    public async Task Confirming_widens_the_set_and_never_narrows_it()
    {
        // The trap this endpoint exists to avoid: a user who never opened the equipment screen
        // has no rows and trains against the assumed gym. Writing a single row for the accepted
        // item would replace a whole gym with one machine.
        Hevy.Forget();
        var client = await ConnectedAsync();   // no equipment described at all
        var exercise = ExerciseCatalogue.All.First();

        Hevy.Events.Add(Logged(
            exercise.ExternalTemplateId, exercise.Title, new DateTimeOffset(2026, 8, 20, 15, 0, 0, TimeSpan.Zero)));
        await client.PostAsync("/hevy/sync", null);

        var before = await EquipmentAsync(client);

        var answered = await client.PostAsJsonAsync(
            "/training/equipment/suggestions",
            new { item = nameof(EquipmentItem.Dumbbells), accepted = true });

        Assert.Equal(HttpStatusCode.OK, answered.StatusCode);

        var after = await EquipmentAsync(client);

        Assert.All(before!.Items, item => Assert.Contains(item, after!.Items));
        Assert.Contains(nameof(EquipmentItem.Dumbbells), after!.Items);
        Assert.True(after.Items.Count >= before.Items.Count);
    }

    [Fact]
    public async Task Confirming_widens_the_pool_the_generator_draws_from()
    {
        Hevy.Forget();
        var client = await ConnectedAsync(SmallGym);
        var exercise = OutsideTheSmallGym();

        Hevy.Events.Add(Logged(
            exercise.ExternalTemplateId, exercise.Title, new DateTimeOffset(2026, 8, 20, 15, 0, 0, TimeSpan.Zero)));
        await client.PostAsync("/hevy/sync", null);

        var missing = (await SuggestionsAsync(client))!.Suggestions[0].Item;

        await client.PostAsJsonAsync(
            "/training/equipment/suggestions", new { item = missing, accepted = true });

        Assert.Contains(missing, (await EquipmentAsync(client))!.Items);
    }

    [Fact]
    public async Task Declining_changes_nothing_and_the_item_does_not_return()
    {
        Hevy.Forget();
        var client = await ConnectedAsync(SmallGym);
        var exercise = OutsideTheSmallGym();

        Hevy.Events.Add(Logged(
            exercise.ExternalTemplateId, exercise.Title, new DateTimeOffset(2026, 8, 20, 15, 0, 0, TimeSpan.Zero)));
        await client.PostAsync("/hevy/sync", null);

        var before = await EquipmentAsync(client);
        var offered = (await SuggestionsAsync(client))!.Suggestions[0].Item;

        await client.PostAsJsonAsync(
            "/training/equipment/suggestions", new { item = offered, accepted = false });

        var after = await EquipmentAsync(client);
        var again = await SuggestionsAsync(client);

        Assert.Equal(before!.Items, after!.Items);
        Assert.DoesNotContain(again!.Suggestions, suggestion => suggestion.Item == offered);
    }

    [Fact]
    public async Task Declining_twice_is_harmless()
    {
        Hevy.Forget();
        var client = await ConnectedAsync(SmallGym);
        var exercise = OutsideTheSmallGym();

        Hevy.Events.Add(Logged(
            exercise.ExternalTemplateId, exercise.Title, new DateTimeOffset(2026, 8, 20, 15, 0, 0, TimeSpan.Zero)));
        await client.PostAsync("/hevy/sync", null);

        var offered = (await SuggestionsAsync(client))!.Suggestions[0].Item;

        await client.PostAsJsonAsync("/training/equipment/suggestions", new { item = offered, accepted = false });
        var second = await client.PostAsJsonAsync(
            "/training/equipment/suggestions", new { item = offered, accepted = false });

        Assert.Equal(HttpStatusCode.OK, second.StatusCode);
    }

    [Fact]
    public async Task An_exercise_outside_our_catalogue_is_reported_as_a_gap()
    {
        // The real account's case: it logs Iso-Lateral Row (Machine), which is not in our
        // catalogue at all. It implies no equipment, and it is surfaced rather than ignored.
        Hevy.Forget();
        var client = await ConnectedAsync(SmallGym);

        Hevy.Events.Add(Logged(
            "AA1EB7D8", "Iso-Lateral Row (Machine)", new DateTimeOffset(2026, 8, 21, 15, 0, 0, TimeSpan.Zero)));
        await client.PostAsync("/hevy/sync", null);

        var suggestions = await SuggestionsAsync(client);

        var gap = Assert.Single(suggestions!.CatalogueGaps);
        Assert.Equal("AA1EB7D8", gap.ExternalTemplateId);
        Assert.Equal("Iso-Lateral Row (Machine)", gap.Title);
    }

    [Fact]
    public async Task An_item_outside_the_vocabulary_is_refused_with_a_code()
    {
        var client = await ConnectedAsync();

        var response = await client.PostAsJsonAsync(
            "/training/equipment/suggestions", new { item = "Kettlebell", accepted = true });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(
            TrainingErrorCodes.UnknownEquipmentItem,
            (await response.Content.ReadFromJsonAsync<ApiError>())?.Code);
    }
}
