using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using Protocol.Api.Hevy;
using Protocol.Api.Training;

namespace Protocol.Api.Tests.Integration.Hevy;

/// <summary>
/// The whole loop, end to end: a week is pushed, trained from, synced, and read back with what
/// was performed beside what was prescribed.
/// </summary>
public class ComparisonEndpointsTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    private const string Password = "Passw0rd!";

    private StubHevyClient Hevy => (StubHevyClient)factory.Services.GetRequiredService<IHevyClient>();

    private async Task<(HttpClient Client, GeneratedWeekResponse Week, HevyPushResponse Pushed)> PushedWeekAsync()
    {
        var email = $"{Guid.NewGuid():N}@protocol.test";
        var client = factory.CreateClient();
        await client.PostAsJsonAsync("/auth/register", new { email, password = Password });
        await client.PostAsJsonAsync("/auth/login?useCookies=true", new { email, password = Password });
        await client.PutAsJsonAsync("/hevy/connection", new { apiKey = StubHevyClient.ValidKey });
        await client.PutAsJsonAsync("/training/profile", new
        {
            goal = "Hypertrophy",
            daysPerWeek = 3,
            sessionDurationSeconds = 5_400,
        });

        var week = await (await client.PostAsync("/training/weeks", null))
            .Content.ReadFromJsonAsync<GeneratedWeekResponse>();

        var pushed = await (await client.PostAsJsonAsync($"/hevy/weeks/{week!.Id}/push", new { locale = "en-US" }))
            .Content.ReadFromJsonAsync<HevyPushResponse>();

        return (client, week, pushed!);
    }

    /// <summary>
    /// A workout as if the user had started the given routine and trained its first exercise with
    /// the sequence supplied — effort held constant while the repetitions fall, which is what a
    /// terminate-on-effort lifter produces.
    /// </summary>
    private static HevyWorkoutEvent Trained(
        string routineId,
        GeneratedSessionResponse session,
        DateTimeOffset at,
        params double[] reps)
    {
        var slot = session.Prescriptions[0];

        return new HevyWorkoutEvent(
            "updated",
            new HevyWorkout(
                $"w-{Guid.NewGuid():N}",
                "Whatever Hevy called it",
                routineId,
                null,
                at,
                at.AddHours(1),
                at.AddHours(1),
                at,
                [
                    new HevyWorkoutExercise(0, slot.ExerciseTitle, null, slot.ExternalTemplateId, null,
                    [
                        new HevyWorkoutSet(0, "warmup", 20, 12, null, null, null, null),
                        .. reps.Select((count, index) =>
                            new HevyWorkoutSet(index + 1, "normal", 50, count, null, null, null, null)),
                    ]),
                ]),
            null,
            null);
    }

    private static async Task<WeekComparison?> ComparisonAsync(HttpClient client, Guid weekId) =>
        await client.GetFromJsonAsync<WeekComparison>($"/training/weeks/{weekId}/comparison");

    [Fact]
    public async Task The_comparison_is_unreachable_without_a_session()
    {
        var response = await factory.CreateClient().GetAsync($"/training/weeks/{Guid.NewGuid()}/comparison");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task A_week_nobody_trained_reads_as_nothing_performed()
    {
        Hevy.Forget();
        var (client, week, _) = await PushedWeekAsync();

        var comparison = await ComparisonAsync(client, week.Id);

        Assert.NotNull(comparison);
        Assert.Equal(week.Sessions.Count, comparison.Sessions.Count);
        Assert.All(comparison.Sessions, session => Assert.False(session.Performed));
        Assert.All(
            comparison.Sessions.SelectMany(session => session.Slots),
            slot => Assert.Equal(SlotOutcomes.NotPerformed, slot.Outcome));
        Assert.Equal(0, comparison.Coverage.ImportedWorkouts);
    }

    [Fact]
    public async Task A_trained_session_reads_back_with_its_sequence_intact()
    {
        // The deliverable of this milestone, in one test: pushed, trained, synced, compared.
        Hevy.Forget();
        var (client, week, pushed) = await PushedWeekAsync();

        Hevy.Events.Add(Trained(
            pushed.Sessions[0].RoutineId,
            week.Sessions[0],
            new DateTimeOffset(2026, 8, 24, 15, 0, 0, TimeSpan.Zero),
            11, 9, 8));

        await client.PostAsync("/hevy/sync", null);

        var comparison = await ComparisonAsync(client, week.Id);
        var session = comparison!.Sessions[0];

        Assert.True(session.Performed);
        Assert.NotNull(session.PerformedAt);

        var slot = session.Slots[0];

        // Ordered, and not a total. 11/9/8 is not the same fact as 8/9/11.
        Assert.Equal([11d, 9d, 8d], slot.PerformedSets.Select(set => set.Reps));

        // The warm-up came through the import and is excluded from the comparison (ADR-018, TD-006).
        Assert.Equal(3, slot.PerformedSets.Count);

        // The prescription is beside it, unchanged since it was generated (ADR-003).
        Assert.Equal(2, slot.RepsInReserve);   // TD-018
        Assert.True(slot.MinReps > 0 && slot.MaxReps >= slot.MinReps);
    }

    [Fact]
    public async Task Effort_is_absent_because_the_account_reports_none()
    {
        // Not an edge case: every workout read from a real Hevy account so far carries rpe null on
        // every set. The screen has to tell that from a reported zero (TD-017).
        Hevy.Forget();
        var (client, week, pushed) = await PushedWeekAsync();

        Hevy.Events.Add(Trained(
            pushed.Sessions[0].RoutineId,
            week.Sessions[0],
            new DateTimeOffset(2026, 8, 24, 15, 0, 0, TimeSpan.Zero),
            11, 9));

        await client.PostAsync("/hevy/sync", null);

        var slot = (await ComparisonAsync(client, week.Id))!.Sessions[0].Slots[0];

        Assert.All(slot.PerformedSets, set => Assert.Null(set.RepsInReserve));
    }

    [Fact]
    public async Task Only_the_session_that_was_trained_reads_as_performed()
    {
        Hevy.Forget();
        var (client, week, pushed) = await PushedWeekAsync();

        Hevy.Events.Add(Trained(
            pushed.Sessions[1].RoutineId,
            week.Sessions[1],
            new DateTimeOffset(2026, 8, 26, 15, 0, 0, TimeSpan.Zero),
            12, 11, 10));

        await client.PostAsync("/hevy/sync", null);

        var comparison = await ComparisonAsync(client, week.Id);

        Assert.False(comparison!.Sessions[0].Performed);
        Assert.True(comparison.Sessions[1].Performed);
        Assert.Equal(1, comparison.Coverage.BoundWorkouts);
    }

    [Fact]
    public async Task Freestyle_training_is_imported_and_listed_unbound()
    {
        // ADR-019's degraded path, and the majority case in the account this project has seen.
        Hevy.Forget();
        var (client, week, _) = await PushedWeekAsync();

        Hevy.Events.Add(ImportHistoryTests.Updated(
            "walk-in", new DateTimeOffset(2026, 8, 25, 15, 0, 0, TimeSpan.Zero)));

        await client.PostAsync("/hevy/sync", null);

        var comparison = await ComparisonAsync(client, week.Id);

        Assert.Equal(1, comparison!.Coverage.ImportedWorkouts);
        Assert.Equal(0, comparison.Coverage.BoundWorkouts);
        Assert.Single(comparison.UnboundWorkouts);
        Assert.All(comparison.Sessions, session => Assert.False(session.Performed));
    }

    [Fact]
    public async Task Someone_elses_week_is_not_readable()
    {
        var (_, week, _) = await PushedWeekAsync();
        var (other, _, _) = await PushedWeekAsync();

        Assert.Equal(
            HttpStatusCode.NotFound,
            (await other.GetAsync($"/training/weeks/{week.Id}/comparison")).StatusCode);
    }

    [Fact]
    public async Task A_workout_deleted_upstream_stops_reading_as_performed()
    {
        Hevy.Forget();
        var (client, week, pushed) = await PushedWeekAsync();

        var trained = Trained(
            pushed.Sessions[0].RoutineId,
            week.Sessions[0],
            new DateTimeOffset(2026, 8, 24, 15, 0, 0, TimeSpan.Zero),
            11, 9, 8);

        Hevy.Events.Add(trained);
        await client.PostAsync("/hevy/sync", null);
        Assert.True((await ComparisonAsync(client, week.Id))!.Sessions[0].Performed);

        Hevy.Events.Clear();
        Hevy.Events.Add(new HevyWorkoutEvent(
            "deleted", null, trained.Workout!.Id, new DateTimeOffset(2026, 8, 27, 0, 0, 0, TimeSpan.Zero)));
        await client.PostAsync("/hevy/sync", null);

        var after = await ComparisonAsync(client, week.Id);

        Assert.False(after!.Sessions[0].Performed);
        Assert.Equal(0, after.Coverage.ImportedWorkouts);
    }
}
