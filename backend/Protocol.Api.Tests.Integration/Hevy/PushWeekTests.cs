using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Protocol.Api.Auth;
using Protocol.Api.Hevy;
using Protocol.Api.Training;

namespace Protocol.Api.Tests.Integration.Hevy;

/// <summary>
/// Pushing a generated week into Hevy: a folder for the week, one routine per session, and the
/// identifiers stored beside our own (ADR-015, standard 8).
/// </summary>
public class PushWeekTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    private const string Password = "Passw0rd!";

    private StubHevyClient Hevy => (StubHevyClient)factory.Services.GetRequiredService<IHevyClient>();

    /// <summary>A signed-in user with a connected account and a generated week.</summary>
    private async Task<(HttpClient Client, string UserId, GeneratedWeekResponse Week)> ReadyAsync(
        int daysPerWeek = 3)
    {
        var email = $"{Guid.NewGuid():N}@protocol.test";
        var client = factory.CreateClient();
        await client.PostAsJsonAsync("/auth/register", new { email, password = Password });
        await client.PostAsJsonAsync("/auth/login?useCookies=true", new { email, password = Password });

        var userId = (await client.GetFromJsonAsync<CurrentUser>("/auth/me"))!.Id;

        await client.PutAsJsonAsync("/hevy/connection", new { apiKey = StubHevyClient.ValidKey });
        await client.PutAsJsonAsync("/training/profile", new
        {
            goal = "Hypertrophy",
            daysPerWeek,
            sessionDurationSeconds = 5_400,
        });

        var week = await (await client.PostAsync("/training/weeks", null))
            .Content.ReadFromJsonAsync<GeneratedWeekResponse>();

        return (client, userId, week!);
    }

    private static async Task<HevyPushResponse?> PushAsync(HttpClient client, Guid weekId, string locale = "en-US")
    {
        var response = await client.PostAsJsonAsync($"/hevy/weeks/{weekId}/push", new { locale });
        return await response.Content.ReadFromJsonAsync<HevyPushResponse>();
    }

    [Fact]
    public async Task Pushing_is_unreachable_without_a_session()
    {
        var response = await factory.CreateClient()
            .PostAsJsonAsync($"/hevy/weeks/{Guid.NewGuid()}/push", new { locale = "en-US" });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task A_week_becomes_one_folder_and_one_routine_per_session()
    {
        Hevy.Forget();
        var (client, _, week) = await ReadyAsync(daysPerWeek: 3);

        var pushed = await PushAsync(client, week.Id);

        Assert.NotNull(pushed);
        Assert.NotNull(pushed.FolderId);
        Assert.Equal(week.Sessions.Count, pushed.Sessions.Count);
        Assert.All(pushed.Sessions, session => Assert.False(string.IsNullOrWhiteSpace(session.RoutineId)));

        // Asserted against what Hevy received, not only against what we stored: saving an
        // identifier proves we recorded something, not that we sent the right thing.
        Assert.Single(Hevy.FolderTitles);
        Assert.Equal(week.Sessions.Count, Hevy.Created.Count);
        Assert.All(Hevy.Created, created => Assert.Equal(pushed.FolderId, created.Payload.FolderId));
    }

    [Fact]
    public async Task The_identifiers_are_stored_beside_our_own_records()
    {
        var (client, userId, week) = await ReadyAsync();
        var pushed = await PushAsync(client, week.Id);

        using var scope = factory.Services.CreateScope();
        // Reaching into the context because these columns have no read path of their own -- they
        // are the join ADR-019 uses, not something an endpoint returns. The standing exception in
        // backend/CLAUDE.md is for exactly this.
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var stored = await db.GeneratedWeeks
            .AsNoTracking()
            .Include(w => w.Sessions)
            .SingleAsync(w => w.Id == week.Id && w.UserId == userId);

        Assert.Equal(pushed!.FolderId, stored.HevyRoutineFolderId);
        Assert.All(stored.Sessions, session => Assert.False(string.IsNullOrWhiteSpace(session.HevyRoutineId)));
    }

    [Fact]
    public async Task Pushing_twice_before_any_training_reuses_the_folder_and_replaces_the_routines()
    {
        // The behaviour ADR-017 exists for. Hevy has no delete, and the engineer predicted users
        // regenerate repeatedly before starting -- so the optimisation phase must not leave a
        // trail of dead folders the product caused and cannot clean up.
        Hevy.Forget();
        var (client, _, week) = await ReadyAsync();

        var first = await PushAsync(client, week.Id);
        var second = await PushAsync(client, week.Id);

        Assert.Equal(first!.FolderId, second!.FolderId);
        Assert.Single(Hevy.FolderTitles);
        Assert.Equal(week.Sessions.Count, Hevy.Created.Count);      // created once
        Assert.Equal(week.Sessions.Count, Hevy.Updated.Count);      // replaced on the second push

        Assert.Equal(
            first.Sessions.Select(session => session.RoutineId),
            second.Sessions.Select(session => session.RoutineId));
    }

    [Fact]
    public async Task A_week_something_has_trained_from_is_not_rewritten()
    {
        // Once a workout has been logged against a routine, that routine is evidence: rewriting
        // it would leave the workout pointing at a prescription that did not exist when it was
        // performed (ADR-017). Regenerating produces a new week, which pushes freely.
        Hevy.Forget();
        var (client, userId, week) = await ReadyAsync();
        var pushed = await PushAsync(client, week.Id);

        using (var scope = factory.Services.CreateScope())
        {
            // Standing in for S3.4, which is what will really write these rows.
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.PerformedWorkouts.Add(new PerformedWorkout
            {
                Id = Guid.CreateVersion7(),
                UserId = userId,
                ExternalWorkoutId = $"workout-{Guid.NewGuid():N}",
                ExternalRoutineId = pushed!.Sessions[0].RoutineId,
                StartedAt = DateTimeOffset.UtcNow,
                EndedAt = DateTimeOffset.UtcNow,
                ExternallyUpdatedAt = DateTimeOffset.UtcNow,
                Version = 1,
            });
            await db.SaveChangesAsync();
        }

        var updatesBefore = Hevy.Updated.Count;
        var again = await client.PostAsJsonAsync($"/hevy/weeks/{week.Id}/push", new { locale = "en-US" });

        Assert.Equal(HttpStatusCode.Conflict, again.StatusCode);
        Assert.Equal(
            HevyErrorCodes.WeekAlreadyTrainedFrom,
            (await again.Content.ReadFromJsonAsync<ApiError>())?.Code);
        Assert.Equal(updatesBefore, Hevy.Updated.Count);
    }

    [Fact]
    public async Task Another_users_training_never_refuses_my_push()
    {
        // A routine identifier is Hevy's, not ours, so nothing guarantees it is unique across
        // accounts. Without a user filter on the "has this been trained from" lookup, one user's
        // imported training refuses another user's push -- which is what the E2E suite found when
        // sixteen workers ran against one API.
        Hevy.Forget();
        var (client, _, week) = await ReadyAsync();
        var pushed = await PushAsync(client, week.Id);

        var (_, otherUserId, _) = await ReadyAsync();

        using (var scope = factory.Services.CreateScope())
        {
            // Someone else's workout, carrying *our* routine identifier.
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.PerformedWorkouts.Add(new PerformedWorkout
            {
                Id = Guid.CreateVersion7(),
                UserId = otherUserId,
                ExternalWorkoutId = $"workout-{Guid.NewGuid():N}",
                ExternalRoutineId = pushed!.Sessions[0].RoutineId,
                StartedAt = DateTimeOffset.UtcNow,
                EndedAt = DateTimeOffset.UtcNow,
                ExternallyUpdatedAt = DateTimeOffset.UtcNow,
                Version = 1,
            });
            await db.SaveChangesAsync();
        }

        var again = await client.PostAsJsonAsync($"/hevy/weeks/{week.Id}/push", new { locale = "en-US" });

        Assert.Equal(HttpStatusCode.OK, again.StatusCode);
    }

    [Fact]
    public async Task A_routine_the_user_deleted_in_Hevy_is_a_push_failure_and_not_corruption()
    {
        Hevy.Forget();
        var (client, _, week) = await ReadyAsync();
        var pushed = await PushAsync(client, week.Id);

        // The user tidied up in Hevy. We cannot delete, and we cannot assume it is still there.
        foreach (var session in pushed!.Sessions)
        {
            Hevy.Missing.Add(session.RoutineId);
        }

        var again = await client.PostAsJsonAsync($"/hevy/weeks/{week.Id}/push", new { locale = "en-US" });

        Assert.Equal(HttpStatusCode.Conflict, again.StatusCode);
        Assert.Equal(
            HevyErrorCodes.PushedRoutineMissing,
            (await again.Content.ReadFromJsonAsync<ApiError>())?.Code);
    }

    [Fact]
    public async Task Pushing_without_a_connected_account_says_so()
    {
        var email = $"{Guid.NewGuid():N}@protocol.test";
        var client = factory.CreateClient();
        await client.PostAsJsonAsync("/auth/register", new { email, password = Password });
        await client.PostAsJsonAsync("/auth/login?useCookies=true", new { email, password = Password });
        await client.PutAsJsonAsync("/training/profile", new
        {
            goal = "Hypertrophy",
            daysPerWeek = 3,
            sessionDurationSeconds = 5_400,
        });

        var week = await (await client.PostAsync("/training/weeks", null))
            .Content.ReadFromJsonAsync<GeneratedWeekResponse>();

        var response = await client.PostAsJsonAsync($"/hevy/weeks/{week!.Id}/push", new { locale = "en-US" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(
            HevyErrorCodes.HevyNotConnected,
            (await response.Content.ReadFromJsonAsync<ApiError>())?.Code);
    }

    [Fact]
    public async Task Pushing_someone_elses_week_finds_nothing()
    {
        var (_, _, week) = await ReadyAsync();
        var (other, _, _) = await ReadyAsync();

        var response = await other.PostAsJsonAsync($"/hevy/weeks/{week.Id}/push", new { locale = "en-US" });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task The_pushed_note_is_in_the_locale_the_push_carried()
    {
        Hevy.Forget();
        var (client, _, week) = await ReadyAsync();

        await PushAsync(client, week.Id, locale: "pt-BR");

        var notes = Hevy.Created
            .SelectMany(created => created.Payload.Exercises)
            .Select(exercise => exercise.Notes)
            .ToList();

        Assert.NotEmpty(notes);
        Assert.All(notes, note => Assert.Contains("velocidade", note!, StringComparison.OrdinalIgnoreCase));
    }
}
