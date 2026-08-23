using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Protocol.Api.Auth;
using Protocol.Api.Hevy;
using Protocol.Api.Training;

namespace Protocol.Api.Tests.Integration.Hevy;

/// <summary>
/// Reconciling records that changed upstream, without ever rewriting one (root standard 7,
/// ADR-018).
/// </summary>
public class ReconciliationTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    private const string Password = "Passw0rd!";

    private StubHevyClient Hevy => (StubHevyClient)factory.Services.GetRequiredService<IHevyClient>();

    private async Task<(HttpClient Client, string UserId)> ConnectedAsync()
    {
        var email = $"{Guid.NewGuid():N}@protocol.test";
        var client = factory.CreateClient();
        await client.PostAsJsonAsync("/auth/register", new { email, password = Password });
        await client.PostAsJsonAsync("/auth/login?useCookies=true", new { email, password = Password });
        await client.PutAsJsonAsync("/hevy/connection", new { apiKey = StubHevyClient.ValidKey });

        return (client, (await client.GetFromJsonAsync<CurrentUser>("/auth/me"))!.Id);
    }

    private async Task<List<PerformedWorkout>> VersionsAsync(string userId, string externalId)
    {
        using var scope = factory.Services.CreateScope();
        // Reaching into the context: versioning is the storage contract itself and has no read
        // endpoint. backend/CLAUDE.md's standing exception covers exactly this case.
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        return await db.PerformedWorkouts
            .AsNoTracking()
            .Include(w => w.Exercises).ThenInclude(e => e.Sets)
            .Where(w => w.UserId == userId && w.ExternalWorkoutId == externalId)
            .OrderBy(w => w.Version)
            .ToListAsync();
    }

    private static HevyWorkoutEvent WithReps(string id, DateTimeOffset updatedAt, params double[] reps) =>
        new(
            "updated",
            new HevyWorkout(id, "Push", null, null, updatedAt.AddHours(-1), updatedAt, updatedAt,
                updatedAt.AddHours(-1),
                [
                    new HevyWorkoutExercise(0, "Bench Press (Barbell)", null, "79D0BB3A", null,
                        [.. reps.Select((count, index) =>
                            new HevyWorkoutSet(index, "normal", 50, count, null, null, null, null))]),
                ]),
            null,
            null);

    private static async Task SyncAsync(HttpClient client) => await client.PostAsync("/hevy/sync", null);

    [Fact]
    public async Task An_edit_upstream_appends_a_version_and_leaves_the_earlier_one_readable()
    {
        // The acceptance criterion, and the whole of root standard 7: a correction arrives as a
        // new record. An analysis produced against the first reading is still explainable.
        Hevy.Forget();
        var (client, userId) = await ConnectedAsync();

        var first = new DateTimeOffset(2026, 8, 20, 15, 48, 0, TimeSpan.Zero);
        Hevy.Events.Add(WithReps("w1", first, 11, 9, 8));
        await SyncAsync(client);

        // The user corrected the session in Hevy the next day.
        Hevy.Events.Clear();
        Hevy.Events.Add(WithReps("w1", first.AddDays(1), 11, 9, 7));
        await SyncAsync(client);

        var versions = await VersionsAsync(userId, "w1");

        Assert.Equal(2, versions.Count);
        Assert.Equal([1, 2], versions.Select(v => v.Version));

        Assert.Equal(
            [11d, 9d, 8d],
            versions[0].Exercises.Single().Sets.OrderBy(s => s.Position).Select(s => s.Reps));
        Assert.Equal(
            [11d, 9d, 7d],
            versions[1].Exercises.Single().Sets.OrderBy(s => s.Position).Select(s => s.Reps));
    }

    [Fact]
    public async Task The_current_reading_is_the_later_version()
    {
        Hevy.Forget();
        var (client, userId) = await ConnectedAsync();

        var first = new DateTimeOffset(2026, 8, 20, 15, 48, 0, TimeSpan.Zero);
        Hevy.Events.Add(WithReps("w1", first, 11, 9, 8));
        await SyncAsync(client);

        Hevy.Events.Clear();
        Hevy.Events.Add(WithReps("w1", first.AddDays(1), 12));
        await SyncAsync(client);

        var current = Assert.Single(PerformedVolume.Current(await VersionsAsync(userId, "w1")));

        Assert.Equal(2, current.Version);
        Assert.Equal([12d], current.Exercises.Single().Sets.Select(s => s.Reps));
    }

    [Fact]
    public async Task A_deletion_upstream_writes_a_tombstone_and_removes_no_row()
    {
        // "A workout deleted in Hevy stops counting toward volume without any row being removed."
        // The events feed is the only surface that reports this at all -- on the plain workouts
        // list a removed workout simply stops appearing.
        Hevy.Forget();
        var (client, userId) = await ConnectedAsync();

        var at = new DateTimeOffset(2026, 8, 20, 15, 48, 0, TimeSpan.Zero);
        Hevy.Events.Add(WithReps("w1", at, 11, 9, 8));
        await SyncAsync(client);

        Hevy.Events.Clear();
        Hevy.Events.Add(new HevyWorkoutEvent("deleted", null, "w1", at.AddDays(1)));
        await SyncAsync(client);

        var versions = await VersionsAsync(userId, "w1");

        Assert.Equal(2, versions.Count);
        Assert.False(versions[0].IsDeleted);
        Assert.True(versions[1].IsDeleted);

        // The sets are still there, on the version below the tombstone.
        Assert.Equal(3, versions[0].Exercises.Single().Sets.Count);

        // And it stops counting.
        var catalogue = ExerciseCatalogue.All.ToDictionary(exercise => exercise.Id);
        Assert.Empty(PerformedVolume.ByMuscle(PerformedVolume.Current(versions), catalogue));
    }

    [Fact]
    public async Task A_deletion_delivered_twice_tombstones_once()
    {
        Hevy.Forget();
        var (client, userId) = await ConnectedAsync();

        var at = new DateTimeOffset(2026, 8, 20, 15, 48, 0, TimeSpan.Zero);
        Hevy.Events.Add(WithReps("w1", at, 11));
        await SyncAsync(client);

        Hevy.Events.Clear();
        Hevy.Events.Add(new HevyWorkoutEvent("deleted", null, "w1", at.AddDays(1)));
        await SyncAsync(client);
        await SyncAsync(client);

        Assert.Equal(2, (await VersionsAsync(userId, "w1")).Count);
    }

    [Fact]
    public async Task Deleting_a_workout_we_never_read_is_a_no_op()
    {
        // The user may have removed a session from before they connected. Nothing to tombstone is
        // not an error.
        Hevy.Forget();
        var (client, userId) = await ConnectedAsync();

        Hevy.Events.Add(new HevyWorkoutEvent(
            "deleted", null, "never-seen", new DateTimeOffset(2026, 8, 21, 0, 0, 0, TimeSpan.Zero)));

        await SyncAsync(client);

        Assert.Empty(await VersionsAsync(userId, "never-seen"));
    }

    [Fact]
    public async Task Each_version_keeps_its_own_payload()
    {
        // A changed conversion is a recomputation rather than a re-fetch, and that only holds if
        // every reading kept its own input (ADR-018).
        Hevy.Forget();
        var (client, userId) = await ConnectedAsync();

        var first = new DateTimeOffset(2026, 8, 20, 15, 48, 0, TimeSpan.Zero);
        Hevy.Events.Add(WithReps("w1", first, 11, 9, 8));
        await SyncAsync(client);

        Hevy.Events.Clear();
        Hevy.Events.Add(WithReps("w1", first.AddDays(1), 11, 9, 7));
        await SyncAsync(client);

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var snapshots = await db.HevyWorkoutSnapshots
            .AsNoTracking()
            .Where(s => s.UserId == userId && s.ExternalWorkoutId == "w1")
            .OrderBy(s => s.Version)
            .ToListAsync();

        Assert.Equal(2, snapshots.Count);
        Assert.Contains("\"reps\":8", snapshots[0].RawJson.Replace(" ", string.Empty));
        Assert.Contains("\"reps\":7", snapshots[1].RawJson.Replace(" ", string.Empty));
    }
}
