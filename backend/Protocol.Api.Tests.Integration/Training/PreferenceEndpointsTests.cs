using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Protocol.Api.Auth;
using Protocol.Api.Training;

namespace Protocol.Api.Tests.Integration.Training;

/// <summary>
/// Saying what you will not do, and having the generator honour it.
/// </summary>
public class PreferenceEndpointsTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    private const string Password = "Passw0rd!";

    /// <summary>Overhead Press (Barbell), and its dumbbell counterpart.</summary>
    private const string BarbellPress = "7B8D84E8";
    private const string DumbbellPress = "6AC96645";

    private async Task<HttpClient> SignedInClientAsync()
    {
        var email = $"{Guid.NewGuid():N}@protocol.test";
        var client = factory.CreateClient();
        await client.PostAsJsonAsync("/auth/register", new { email, password = Password });
        await client.PostAsJsonAsync("/auth/login?useCookies=true", new { email, password = Password });
        return client;
    }

    private async Task<Guid> ExerciseIdAsync(string externalTemplateId)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        return await db.Exercises
            .Where(exercise => exercise.ExternalTemplateId == externalTemplateId)
            .Select(exercise => exercise.Id)
            .SingleAsync();
    }

    [Fact]
    public async Task Preferences_are_unreachable_without_a_session()
    {
        var client = factory.CreateClient();

        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/training/preferences")).StatusCode);
        Assert.Equal(
            HttpStatusCode.Unauthorized,
            (await client.PutAsJsonAsync("/training/preferences", new { excludedExerciseIds = Array.Empty<Guid>() }))
                .StatusCode);
    }

    [Fact]
    public async Task A_new_user_has_said_nothing()
    {
        var client = await SignedInClientAsync();

        var preferences = await client.GetFromJsonAsync<PreferencesResponse>("/training/preferences");

        Assert.Empty(preferences!.Excluded);
        Assert.Empty(preferences.PreferredVariants);
    }

    [Fact]
    public async Task An_exclusion_is_read_back_and_replaces_the_previous_one()
    {
        var client = await SignedInClientAsync();
        var barbell = await ExerciseIdAsync(BarbellPress);
        var dumbbell = await ExerciseIdAsync(DumbbellPress);

        await client.PutAsJsonAsync("/training/preferences", new { excludedExerciseIds = new[] { barbell } });
        var first = await client.GetFromJsonAsync<PreferencesResponse>("/training/preferences");
        Assert.Equal([barbell], first!.Excluded.Select(row => row.ExerciseId));

        await client.PutAsJsonAsync("/training/preferences", new { excludedExerciseIds = new[] { dumbbell } });
        var second = await client.GetFromJsonAsync<PreferencesResponse>("/training/preferences");
        Assert.Equal([dumbbell], second!.Excluded.Select(row => row.ExerciseId));
    }

    [Fact]
    public async Task A_preferred_variant_is_read_back_with_its_movement_pattern()
    {
        var client = await SignedInClientAsync();
        var dumbbell = await ExerciseIdAsync(DumbbellPress);

        await client.PutAsJsonAsync(
            "/training/preferences",
            new { preferredVariants = new[] { new { movementPattern = "VerticalPush", exerciseId = dumbbell } } });

        var read = await client.GetFromJsonAsync<PreferencesResponse>("/training/preferences");

        var preferred = Assert.Single(read!.PreferredVariants);
        Assert.Equal("VerticalPush", preferred.MovementPattern);
        Assert.Equal(dumbbell, preferred.ExerciseId);
    }

    [Fact]
    public async Task An_exercise_that_is_not_ours_is_refused_with_a_code()
    {
        var client = await SignedInClientAsync();

        var response = await client.PutAsJsonAsync(
            "/training/preferences",
            new { excludedExerciseIds = new[] { Guid.NewGuid() } });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        var error = await response.Content.ReadFromJsonAsync<ApiError>();
        Assert.Equal(TrainingErrorCodes.ExerciseNotFound, error!.Code);
    }

    [Fact]
    public async Task Preferring_an_exercise_for_a_pattern_it_does_not_belong_to_is_refused()
    {
        // Otherwise the preference could never fire, and would look like the generator ignoring
        // what the user asked for.
        var client = await SignedInClientAsync();
        var dumbbellPress = await ExerciseIdAsync(DumbbellPress);

        var response = await client.PutAsJsonAsync(
            "/training/preferences",
            new { preferredVariants = new[] { new { movementPattern = "Squat", exerciseId = dumbbellPress } } });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var error = await response.Content.ReadFromJsonAsync<ApiError>();
        Assert.Equal(TrainingErrorCodes.NotACandidate, error!.Code);
    }

    [Fact]
    public async Task A_user_never_reads_another_users_preferences()
    {
        var first = await SignedInClientAsync();
        var barbell = await ExerciseIdAsync(BarbellPress);
        await first.PutAsJsonAsync("/training/preferences", new { excludedExerciseIds = new[] { barbell } });

        var second = await SignedInClientAsync();
        var read = await second.GetFromJsonAsync<PreferencesResponse>("/training/preferences");

        Assert.Empty(read!.Excluded);
    }

    [Fact]
    public async Task A_generated_week_honours_an_exclusion_end_to_end()
    {
        // The whole point of the step: the engineer will not do a barbell overhead press.
        var client = await SignedInClientAsync();
        var barbell = await ExerciseIdAsync(BarbellPress);

        await client.PutAsJsonAsync(
            "/training/profile",
            new { goal = "Hypertrophy", daysPerWeek = 4, sessionDurationSeconds = 5_400 });
        await client.PutAsJsonAsync("/training/preferences", new { excludedExerciseIds = new[] { barbell } });

        var response = await client.PostAsync("/training/weeks", null);
        response.EnsureSuccessStatusCode();
        var week = await response.Content.ReadFromJsonAsync<GeneratedWeekResponse>();

        var prescribed = week!.Sessions.SelectMany(session => session.Prescriptions).ToList();

        Assert.NotEmpty(prescribed);
        Assert.DoesNotContain(prescribed, p => p.ExternalTemplateId == BarbellPress);
        Assert.Contains(prescribed, p => p.ExternalTemplateId == DumbbellPress);

        // And the prescription followed the exercise across the order_class boundary.
        var press = prescribed.Single(p => p.ExternalTemplateId == DumbbellPress);
        Assert.Equal(8, press.MinReps);
        Assert.Equal(12, press.MaxReps);
    }
}
