using System.Net;
using System.Net.Http.Json;
using Protocol.Api.Training;

namespace Protocol.Api.Tests.Integration.Training;

/// <summary>
/// Describing a gym, and what the generator does with it.
/// </summary>
public class EquipmentEndpointsTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    private const string Password = "Passw0rd!";

    private async Task<HttpClient> SignedInClientAsync()
    {
        var email = $"{Guid.NewGuid():N}@protocol.test";
        var client = factory.CreateClient();
        await client.PostAsJsonAsync("/auth/register", new { email, password = Password });
        await client.PostAsJsonAsync("/auth/login?useCookies=true", new { email, password = Password });
        return client;
    }

    [Fact]
    public async Task Equipment_is_unreachable_without_a_session()
    {
        var client = factory.CreateClient();

        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/training/equipment")).StatusCode);
        Assert.Equal(
            HttpStatusCode.Unauthorized,
            (await client.PutAsJsonAsync("/training/equipment", new { items = new[] { "Barbell" } })).StatusCode);
    }

    [Fact]
    public async Task A_new_user_starts_with_the_gym_TD_004_assumed()
    {
        // No rows is not an empty gym. A user who never opens this screen behaves exactly as in
        // M1, which is what makes equipment additive rather than a break (ADR-013).
        var client = await SignedInClientAsync();

        var equipment = await client.GetFromJsonAsync<EquipmentResponse>("/training/equipment");

        Assert.NotNull(equipment);
        Assert.Equal(
            ExerciseCatalogue.AssumedGym.Select(item => item.ToString()).Order(),
            equipment!.Items);
    }

    [Fact]
    public async Task The_vocabulary_travels_with_the_answer()
    {
        // So the screen never hardcodes a list that would drift from the enum.
        var client = await SignedInClientAsync();

        var equipment = await client.GetFromJsonAsync<EquipmentResponse>("/training/equipment");

        Assert.Equal(
            Enum.GetValues<EquipmentItem>().Select(item => item.ToString()),
            equipment!.Vocabulary);
    }

    [Fact]
    public async Task A_described_gym_is_read_back_exactly()
    {
        var client = await SignedInClientAsync();
        var items = new[] { "Barbell", "WeightPlates", "Bench", "Bodyweight" };

        var written = await client.PutAsJsonAsync("/training/equipment", new { items });
        Assert.Equal(HttpStatusCode.OK, written.StatusCode);

        var read = await client.GetFromJsonAsync<EquipmentResponse>("/training/equipment");

        Assert.Equal(items.Order(), read!.Items);
    }

    [Fact]
    public async Task Describing_a_gym_twice_replaces_it_rather_than_adding_to_it()
    {
        var client = await SignedInClientAsync();

        await client.PutAsJsonAsync("/training/equipment", new { items = new[] { "Barbell", "WeightPlates" } });
        await client.PutAsJsonAsync("/training/equipment", new { items = new[] { "Dumbbells" } });

        var read = await client.GetFromJsonAsync<EquipmentResponse>("/training/equipment");

        Assert.Equal(["Dumbbells"], read!.Items);
    }

    [Fact]
    public async Task An_empty_gym_is_refused_rather_than_read_as_never_described()
    {
        // The two are opposite intents and would otherwise be the same zero rows (ADR-013).
        var client = await SignedInClientAsync();

        var response = await client.PutAsJsonAsync("/training/equipment", new { items = Array.Empty<string>() });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var error = await response.Content.ReadFromJsonAsync<ApiError>();
        Assert.Equal(TrainingErrorCodes.EquipmentSetEmpty, error!.Code);
    }

    [Fact]
    public async Task An_item_this_product_does_not_know_is_refused_with_a_code()
    {
        var client = await SignedInClientAsync();

        var response = await client.PutAsJsonAsync(
            "/training/equipment",
            new { items = new[] { "Barbell", "TrapBar" } });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var error = await response.Content.ReadFromJsonAsync<ApiError>();
        Assert.Equal(TrainingErrorCodes.UnknownEquipmentItem, error!.Code);
    }

    [Fact]
    public async Task A_generated_week_only_contains_what_the_described_gym_can_perform()
    {
        // The acceptance criterion, end to end: a barbell and a rack but no bench means no
        // bench press, even though the barbell is owned.
        var client = await SignedInClientAsync();
        await client.PutAsJsonAsync(
            "/training/profile",
            new { goal = "Hypertrophy", daysPerWeek = 3, sessionDurationSeconds = 3_600 });
        await client.PutAsJsonAsync(
            "/training/equipment",
            new { items = new[] { "Bodyweight", "Barbell", "WeightPlates", "SquatRack" } });

        var response = await client.PostAsync("/training/weeks", null);
        response.EnsureSuccessStatusCode();
        var week = await response.Content.ReadFromJsonAsync<GeneratedWeekResponse>();

        var prescribed = week!.Sessions.SelectMany(session => session.Prescriptions).ToList();
        Assert.NotEmpty(prescribed);

        // 79D0BB3A is Bench Press (Barbell): barbell owned, bench not.
        Assert.DoesNotContain(prescribed, p => p.ExternalTemplateId == "79D0BB3A");
        // 6A6C31A5 is Lat Pulldown (Cable): no cable station owned.
        Assert.DoesNotContain(prescribed, p => p.ExternalTemplateId == "6A6C31A5");
        // D04AC939 is Squat (Barbell): everything it needs is owned.
        Assert.Contains(prescribed, p => p.ExternalTemplateId == "D04AC939");
    }
}
