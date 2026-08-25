using System.Net;
using System.Net.Http.Json;
using Protocol.Api.Training;

namespace Protocol.Api.Tests.Integration.Training;

/// <summary>
/// The profile endpoints, driven over HTTP against a real Postgres — asserted through the API,
/// never against the tables.
/// </summary>
public class TrainingProfileEndpointsTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    private const string Password = "Passw0rd!";

    /// <summary>Registers a fresh user and returns a client carrying its session cookie.</summary>
    private async Task<HttpClient> SignedInClientAsync()
    {
        var email = $"{Guid.NewGuid():N}@protocol.test";
        var client = factory.CreateClient();

        await client.PostAsJsonAsync("/auth/register", new { email, password = Password });
        await client.PostAsJsonAsync("/auth/login?useCookies=true", new { email, password = Password });

        return client;
    }

    private static object Profile(
        string goal = "Hypertrophy",
        int days = 4,
        int seconds = 3_600,
        string? split = null) =>
        new { goal, daysPerWeek = days, sessionDurationSeconds = seconds, split };

    [Fact]
    public async Task The_profile_is_unreachable_without_a_session()
    {
        var client = factory.CreateClient();

        var read = await client.GetAsync("/training/profile");
        var write = await client.PutAsJsonAsync("/training/profile", Profile());

        Assert.Equal(HttpStatusCode.Unauthorized, read.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, write.StatusCode);
    }

    [Fact]
    public async Task Reading_before_writing_reports_that_no_profile_exists()
    {
        var client = await SignedInClientAsync();

        var response = await client.GetAsync("/training/profile");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        var error = await response.Content.ReadFromJsonAsync<ApiError>();
        Assert.Equal(TrainingErrorCodes.ProfileNotFound, error!.Code);
    }

    [Fact]
    public async Task A_profile_is_read_back_exactly_as_it_was_written()
    {
        var client = await SignedInClientAsync();

        var written = await client.PutAsJsonAsync("/training/profile", Profile(days: 5, seconds: 3_000));
        Assert.Equal(HttpStatusCode.OK, written.StatusCode);

        var read = await client.GetFromJsonAsync<TrainingProfileResponse>("/training/profile");

        Assert.NotNull(read);
        Assert.Equal("Hypertrophy", read!.Goal);
        Assert.Equal(5, read.DaysPerWeek);
        // Seconds, not minutes: the domain never holds a rendered unit (root standard 4).
        Assert.Equal(3_000, read.SessionDurationSeconds);
    }

    [Fact]
    public async Task Writing_twice_replaces_the_profile_rather_than_adding_one()
    {
        var client = await SignedInClientAsync();

        await client.PutAsJsonAsync("/training/profile", Profile(days: 3, seconds: 2_400));
        await client.PutAsJsonAsync("/training/profile", Profile(days: 6, seconds: 4_200));

        var read = await client.GetFromJsonAsync<TrainingProfileResponse>("/training/profile");

        Assert.Equal(6, read!.DaysPerWeek);
        Assert.Equal(4_200, read.SessionDurationSeconds);
    }

    [Fact]
    public async Task A_user_never_reads_another_users_profile()
    {
        var first = await SignedInClientAsync();
        await first.PutAsJsonAsync("/training/profile", Profile(days: 2));

        var second = await SignedInClientAsync();
        var response = await second.GetAsync("/training/profile");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Theory]
    [InlineData("Strength")]
    [InlineData("WeightLoss")]
    [InlineData("powerlifting")]
    public async Task Any_goal_other_than_hypertrophy_is_refused_with_its_code(string goal)
    {
        // A goal the schema knows and a goal it does not both answer the same thing: this
        // product does not programme for it yet (ADR-004).
        var client = await SignedInClientAsync();

        var response = await client.PutAsJsonAsync("/training/profile", Profile(goal: goal));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var error = await response.Content.ReadFromJsonAsync<ApiError>();
        Assert.Equal(TrainingErrorCodes.GoalNotSupported, error!.Code);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(7)]
    public async Task A_frequency_outside_the_supported_range_is_refused_with_its_bounds(int days)
    {
        var client = await SignedInClientAsync();

        var response = await client.PutAsJsonAsync("/training/profile", Profile(days: days));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var error = await response.Content.ReadFromJsonAsync<ApiError>();
        Assert.Equal(TrainingErrorCodes.FrequencyOutOfRange, error!.Code);
        // The bounds travel with the code so the frontend never duplicates TD-002's numbers.
        Assert.Equal(2, error.Min);
        Assert.Equal(6, error.Max);
    }

    [Theory]
    [InlineData(1_200)]
    [InlineData(9_000)]
    public async Task A_duration_outside_the_supported_range_is_refused_with_its_bounds(int seconds)
    {
        var client = await SignedInClientAsync();

        var response = await client.PutAsJsonAsync("/training/profile", Profile(seconds: seconds));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var error = await response.Content.ReadFromJsonAsync<ApiError>();
        Assert.Equal(TrainingErrorCodes.DurationOutOfRange, error!.Code);
        Assert.Equal(1_500, error.Min);
        Assert.Equal(7_200, error.Max);
    }

    [Theory]
    // Admitted at five sessions but not at four, and a name the enum does not know at all.
    [InlineData("UpperLowerPushPullLegs")]
    [InlineData("NotATemplate")]
    public async Task A_split_the_frequency_does_not_admit_is_refused_with_its_code(string split)
    {
        var client = await SignedInClientAsync();

        var response = await client.PutAsJsonAsync("/training/profile", Profile(days: 4, split: split));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var error = await response.Content.ReadFromJsonAsync<ApiError>();
        Assert.Equal(TrainingErrorCodes.SplitNotAdmitted, error!.Code);
    }

    [Fact]
    public async Task A_profile_carries_what_was_chosen_what_it_resolves_to_and_what_is_on_offer()
    {
        var client = await SignedInClientAsync();

        // Never chose: null travels as null, and resolves to the frequency's default (ADR-030).
        var unchosen = await client.PutAsJsonAsync("/training/profile", Profile(days: 5));
        var before = await unchosen.Content.ReadFromJsonAsync<TrainingProfileResponse>();

        Assert.Null(before!.Split);
        Assert.Equal(nameof(SplitTemplateId.UpperLowerUpperLowerFull), before.ResolvedSplit);
        Assert.Equal(
            [
                nameof(SplitTemplateId.UpperLowerUpperLowerFull),
                nameof(SplitTemplateId.UpperLowerPushPullLegs),
            ],
            before.AdmittedSplits);

        // Chose: the choice is stored and is distinguishable from having taken the default.
        var chosen = await client.PutAsJsonAsync(
            "/training/profile",
            Profile(days: 5, split: nameof(SplitTemplateId.UpperLowerPushPullLegs)));
        var after = await chosen.Content.ReadFromJsonAsync<TrainingProfileResponse>();

        Assert.Equal(nameof(SplitTemplateId.UpperLowerPushPullLegs), after!.Split);
        Assert.Equal(nameof(SplitTemplateId.UpperLowerPushPullLegs), after.ResolvedSplit);
    }

    [Fact]
    public async Task A_chosen_split_survives_regeneration_and_shapes_the_week()
    {
        var client = await SignedInClientAsync();
        await client.PutAsJsonAsync(
            "/training/profile",
            Profile(days: 5, split: nameof(SplitTemplateId.UpperLowerPushPullLegs)));

        var first = await client.PostAsync("/training/weeks", null);
        first.EnsureSuccessStatusCode();

        var week = (await first.Content.ReadFromJsonAsync<GeneratedWeekResponse>())!;

        Assert.Equal(
            ["Upper", "Lower", "Push", "Pull", "Legs"],
            week.Sessions.Select(session => session.Kind));
    }

    [Fact]
    public async Task A_rejected_profile_is_not_stored()
    {
        var client = await SignedInClientAsync();

        await client.PutAsJsonAsync("/training/profile", Profile(days: 9));
        var read = await client.GetAsync("/training/profile");

        Assert.Equal(HttpStatusCode.NotFound, read.StatusCode);
    }

    [Fact]
    public async Task No_response_carries_display_text()
    {
        // Root standard 3: the backend returns codes and data. The moment a sentence appears
        // here, translating it breaks behaviour.
        var client = await SignedInClientAsync();

        var response = await client.PutAsJsonAsync("/training/profile", Profile(days: 1));
        var body = await response.Content.ReadAsStringAsync();

        // Crude on purpose, and it catches the real failure mode: a code has no spaces in it
        // and a sentence does.
        Assert.DoesNotContain(' ', body);
    }
}
