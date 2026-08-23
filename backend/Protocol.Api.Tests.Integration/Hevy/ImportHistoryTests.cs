using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Protocol.Api.Auth;
using Protocol.Api.Hevy;
using Protocol.Api.Training;

namespace Protocol.Api.Tests.Integration.Hevy;

/// <summary>
/// Syncing what changed since last time: the cursor, the paging, and the promise that running it
/// twice adds nothing (ADR-018).
/// </summary>
public class ImportHistoryTests(ApiFactory factory) : IClassFixture<ApiFactory>
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

    internal static HevyWorkoutEvent Updated(string id, DateTimeOffset updatedAt, string? routineId = null) =>
        new(
            "updated",
            new HevyWorkout(
                id,
                "Push",
                routineId,
                null,
                updatedAt.AddHours(-1),
                updatedAt.AddMinutes(-10),
                updatedAt,
                updatedAt.AddHours(-1),
                [
                    new HevyWorkoutExercise(0, "Bench Press (Barbell)", null, "79D0BB3A", null,
                    [
                        new HevyWorkoutSet(0, "normal", 50, 11, null, null, null, null),
                        new HevyWorkoutSet(1, "normal", 50, 9, null, null, null, null),
                    ]),
                ]),
            null,
            null);

    private static async Task<HevySyncResponse?> SyncAsync(HttpClient client) =>
        await (await client.PostAsync("/hevy/sync", null)).Content.ReadFromJsonAsync<HevySyncResponse>();

    private async Task<List<PerformedWorkout>> StoredAsync(string userId)
    {
        using var scope = factory.Services.CreateScope();
        // Reaching into the context: imported history has no read endpoint until S3.5, and what
        // is being asserted here is the storage contract itself. The standing exception in
        // backend/CLAUDE.md is for exactly this.
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        return await db.PerformedWorkouts
            .AsNoTracking()
            .Include(w => w.Exercises).ThenInclude(e => e.Sets)
            .Where(w => w.UserId == userId)
            .OrderBy(w => w.ExternalWorkoutId).ThenBy(w => w.Version)
            .ToListAsync();
    }

    [Fact]
    public async Task Syncing_is_unreachable_without_a_session()
    {
        Assert.Equal(
            HttpStatusCode.Unauthorized,
            (await factory.CreateClient().PostAsync("/hevy/sync", null)).StatusCode);
    }

    [Fact]
    public async Task Syncing_without_a_connected_account_says_so()
    {
        var email = $"{Guid.NewGuid():N}@protocol.test";
        var client = factory.CreateClient();
        await client.PostAsJsonAsync("/auth/register", new { email, password = Password });
        await client.PostAsJsonAsync("/auth/login?useCookies=true", new { email, password = Password });

        var response = await client.PostAsync("/hevy/sync", null);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(
            HevyErrorCodes.HevyNotConnected,
            (await response.Content.ReadFromJsonAsync<ApiError>())?.Code);
    }

    [Fact]
    public async Task The_first_sync_backfills_from_the_epoch()
    {
        Hevy.Forget();
        var (client, _) = await ConnectedAsync();
        Hevy.Events.Add(Updated("w1", new DateTimeOffset(2020, 1, 1, 0, 0, 0, TimeSpan.Zero)));

        var result = await SyncAsync(client);

        Assert.Equal(1, result!.Imported);
        // Nothing has been read, so nothing is too old to matter.
        Assert.Equal(DateTimeOffset.UnixEpoch, Hevy.Requested[0]);
    }

    [Fact]
    public async Task A_second_sync_over_the_same_history_adds_nothing()
    {
        // The acceptance criterion. The feed answers "at or after" the cursor, so the boundary
        // event is re-delivered every time by design -- recognising it is what makes a re-run a
        // no-op rather than a duplicate.
        Hevy.Forget();
        var (client, userId) = await ConnectedAsync();
        Hevy.Events.Add(Updated("w1", new DateTimeOffset(2026, 8, 20, 15, 48, 0, TimeSpan.Zero)));

        await SyncAsync(client);
        var before = (await StoredAsync(userId)).Count;

        var second = await SyncAsync(client);

        Assert.Equal(0, second!.Imported);
        Assert.Equal(before, (await StoredAsync(userId)).Count);
    }

    [Fact]
    public async Task The_cursor_advances_so_the_next_sync_asks_for_less()
    {
        Hevy.Forget();
        var (client, _) = await ConnectedAsync();
        var at = new DateTimeOffset(2026, 8, 20, 15, 48, 0, TimeSpan.Zero);
        Hevy.Events.Add(Updated("w1", at));

        await SyncAsync(client);
        var asksBefore = Hevy.Requested.Count;
        await SyncAsync(client);

        Assert.Equal(DateTimeOffset.UnixEpoch, Hevy.Requested[0]);
        Assert.Equal(at, Hevy.Requested[asksBefore]);
    }

    [Fact]
    public async Task Paging_reads_past_the_ten_item_cap()
    {
        // Hevy caps a page at ten. A history of twenty-five must not arrive as ten.
        Hevy.Forget();
        var (client, userId) = await ConnectedAsync();

        for (var i = 0; i < 25; i++)
        {
            Hevy.Events.Add(Updated($"w{i:D2}", new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero).AddHours(i)));
        }

        var result = await SyncAsync(client);

        Assert.Equal(25, result!.Imported);
        Assert.Equal(25, (await StoredAsync(userId)).Count);
    }

    [Fact]
    public async Task A_refusal_partway_keeps_what_committed_and_the_cursor_with_it()
    {
        // A sync that gives up is a partial success, never a restart (ADR-021). The next run
        // continues from where it stopped, which is also the behaviour least likely to be refused
        // again.
        Hevy.Forget();
        var (client, userId) = await ConnectedAsync();

        for (var i = 0; i < 25; i++)
        {
            Hevy.Events.Add(Updated($"w{i:D2}", new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero).AddHours(i)));
        }

        Hevy.RefuseFromPage = 2;

        var response = await client.PostAsync("/hevy/sync", null);

        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        Assert.Equal(
            HevyErrorCodes.HevyRateLimited,
            (await response.Content.ReadFromJsonAsync<ApiError>())?.Code);

        // The first page committed and is not read again.
        Assert.Equal(10, (await StoredAsync(userId)).Count);

        Hevy.RefuseFromPage = 0;
        var resumed = await SyncAsync(client);

        Assert.Equal(15, resumed!.Imported);
        Assert.Equal(25, (await StoredAsync(userId)).Count);
    }

    [Fact]
    public async Task A_workout_that_cannot_be_mapped_is_kept_reported_and_does_not_stop_the_sync()
    {
        // The decision S3.2 left open, made here: one workout carrying an unmodelled set type must
        // not block every future sync. The payload is stored before it is mapped, so nothing is
        // lost and deciding later what the unknown thing means is a recomputation (ADR-018).
        Hevy.Forget();
        var (client, userId) = await ConnectedAsync();

        var start = new DateTimeOffset(2026, 3, 1, 0, 0, 0, TimeSpan.Zero);
        Hevy.Events.Add(Updated("good-1", start));
        Hevy.Events.Add(new HevyWorkoutEvent(
            "updated",
            new HevyWorkout("odd-1", "Cluster day", null, null, start, start, start.AddHours(1), start,
            [
                new HevyWorkoutExercise(0, "Something", null, "79D0BB3A", null,
                [
                    new HevyWorkoutSet(0, "cluster", 50, 3, null, null, null, null),
                ]),
            ]),
            null,
            null));
        Hevy.Events.Add(Updated("good-2", start.AddHours(2)));

        var result = await SyncAsync(client);

        Assert.Equal(2, result!.Imported);
        Assert.Equal(1, result.Unmapped);
        Assert.Equal(2, (await StoredAsync(userId)).Count);

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        // The payload survived, with the reason it could not be translated beside it.
        var snapshot = await db.HevyWorkoutSnapshots
            .AsNoTracking()
            .SingleAsync(s => s.UserId == userId && s.ExternalWorkoutId == "odd-1");

        Assert.Contains("cluster", snapshot.RawJson, StringComparison.OrdinalIgnoreCase);
        Assert.False(string.IsNullOrWhiteSpace(snapshot.MappingFailure));
    }

    [Fact]
    public async Task Every_imported_workout_keeps_its_payload()
    {
        Hevy.Forget();
        var (client, userId) = await ConnectedAsync();
        Hevy.Events.Add(Updated("w1", new DateTimeOffset(2026, 5, 5, 12, 0, 0, TimeSpan.Zero)));

        await SyncAsync(client);

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var snapshot = await db.HevyWorkoutSnapshots
            .AsNoTracking()
            .SingleAsync(s => s.UserId == userId && s.ExternalWorkoutId == "w1");

        Assert.Null(snapshot.MappingFailure);
        Assert.Contains("79D0BB3A", snapshot.RawJson);
    }

    [Fact]
    public async Task Warm_up_sets_arrive_and_are_kept()
    {
        // Retained on import, excluded only where volume is counted (ADR-018, TD-006). Filtering
        // on the way in would be discarding a fact to save an if.
        Hevy.Forget();
        var (client, userId) = await ConnectedAsync();

        var at = new DateTimeOffset(2026, 6, 1, 12, 0, 0, TimeSpan.Zero);
        Hevy.Events.Add(new HevyWorkoutEvent(
            "updated",
            new HevyWorkout("w-warm", "Push", null, null, at, at, at, at,
            [
                new HevyWorkoutExercise(0, "Bench Press (Barbell)", null, "79D0BB3A", null,
                [
                    new HevyWorkoutSet(0, "warmup", 20, 12, null, null, null, null),
                    new HevyWorkoutSet(1, "normal", 50, 11, null, null, null, null),
                ]),
            ]),
            null,
            null));

        await SyncAsync(client);

        var stored = Assert.Single(await StoredAsync(userId));
        var sets = stored.Exercises.Single().Sets.OrderBy(set => set.Position).ToList();

        Assert.Equal([SetKind.WarmUp, SetKind.Working], sets.Select(set => set.Kind));
    }
}
