using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Protocol.Api.Auth;
using Protocol.Api.Training;

namespace Protocol.Api.Tests.Integration.Training;

/// <summary>
/// Erasing everything one user owns, and nothing else (ADR-025).
/// <para>
/// The base <see cref="ApiFactory"/> does <b>not</b> set the switch, which is what makes the
/// absence test honest: it is the default configuration answering, not a flag flipped off for the
/// occasion.
/// </para>
/// </summary>
public class EraseUserDataTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    private const string Password = "Passw0rd!";

    private HttpClient EraseEnabledClient() =>
        factory
            .WithWebHostBuilder(builder => builder.UseSetting(EraseUserData.EnabledKey, "true"))
            .CreateClient();

    private static async Task SignInAsync(HttpClient client)
    {
        var email = $"{Guid.NewGuid():N}@protocol.test";
        await client.PostAsJsonAsync("/auth/register", new { email, password = Password });
        await client.PostAsJsonAsync("/auth/login?useCookies=true", new { email, password = Password });
    }

    /// <summary>
    /// Enough state for an erase to have something to remove, built the way a user builds it.
    /// <para>
    /// Every step is asserted, because a silently-failed setup makes the erase test pass for the
    /// wrong reason: there is nothing to delete, so everything is gone afterwards.
    /// </para>
    /// </summary>
    private static async Task GiveThemSomethingToLoseAsync(HttpClient client)
    {
        var profile = await client.PutAsJsonAsync("/training/profile", new
        {
            goal = "Hypertrophy",
            daysPerWeek = 3,
            sessionDurationSeconds = 3_600,
        });
        Assert.True(profile.IsSuccessStatusCode, await profile.Content.ReadAsStringAsync());

        var equipment = await client.PutAsJsonAsync("/training/equipment", new
        {
            items = new[] { "Barbell", "WeightPlates", "Bench", "SquatRack", "Bodyweight" },
        });
        Assert.True(equipment.IsSuccessStatusCode, await equipment.Content.ReadAsStringAsync());

        var week = await client.PostAsJsonAsync("/training/weeks", new { });
        Assert.True(week.IsSuccessStatusCode, await week.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task With_the_switch_off_the_endpoint_does_not_exist()
    {
        // 404 from the router rather than a polite refusal, and the difference is the point
        // (ADR-025). A documented endpoint that says no is one relaxed check away from a published
        // deployment that erases people's data; an unmapped route is not.
        var client = factory.CreateClient();
        await SignInAsync(client);

        var response = await client.PostAsJsonAsync("/training/erase", new { confirmed = true });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task An_erase_without_its_confirmation_is_refused()
    {
        // Deliberate, never a side effect. The confirmation is a field rather than a screen
        // concern precisely so a replayed request cannot arrive at the destructive path.
        var client = EraseEnabledClient();
        await SignInAsync(client);

        var response = await client.PostAsJsonAsync("/training/erase", new { confirmed = false });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var error = await response.Content.ReadFromJsonAsync<ApiError>();
        Assert.Equal(TrainingErrorCodes.EraseNotConfirmed, error!.Code);
    }

    [Fact]
    public async Task Erasing_leaves_every_screen_reading_as_a_fresh_users()
    {
        // The milestone's acceptance criterion, asserted through the API on purpose: what is being
        // reproduced is *what the product looks like to a new account*, and a row count cannot say
        // that. Tables can be empty while a screen still answers from a session or from a row this
        // erase did not know about.
        var client = EraseEnabledClient();
        await SignInAsync(client);
        await GiveThemSomethingToLoseAsync(client);

        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/training/profile")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/training/weeks/current")).StatusCode);

        var erased = await client.PostAsJsonAsync("/training/erase", new { confirmed = true });
        Assert.Equal(HttpStatusCode.OK, erased.StatusCode);

        var counts = await erased.Content.ReadFromJsonAsync<ErasedCounts>();
        Assert.Equal(1, counts!.Profiles);
        Assert.True(counts.Equipment > 0);
        Assert.Equal(1, counts.GeneratedWeeks);

        // A fresh user's readings, one per screen the loop passes through.
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync("/training/profile")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync("/training/weeks/current")).StatusCode);

        // Equipment answers with TD-004's assumed gym again, which is what "never described" means
        // rather than an empty set (ADR-013).
        var equipment = await client.GetFromJsonAsync<EquipmentReading>("/training/equipment");
        Assert.Equal(
            ExerciseCatalogue.AssumedGym.Select(item => item.ToString()).Order(),
            equipment!.Items.Order());

        // And the account survived, so the user is still signed in rather than bounced to login.
        // Every reading above would have been a 401 if it had not.
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/auth/me")).StatusCode);
    }

    [Fact]
    public async Task Another_users_data_is_untouched()
    {
        // Scoped by user, so it cannot reach anyone else. Asserted through their screens rather
        // than their rows: "their data is still there" and "their product still works" are
        // different claims, and only the second one matters to them.
        var mine = EraseEnabledClient();
        await SignInAsync(mine);
        await GiveThemSomethingToLoseAsync(mine);

        var theirs = EraseEnabledClient();
        await SignInAsync(theirs);
        await GiveThemSomethingToLoseAsync(theirs);

        Assert.Equal(
            HttpStatusCode.OK,
            (await mine.PostAsJsonAsync("/training/erase", new { confirmed = true })).StatusCode);

        Assert.Equal(HttpStatusCode.OK, (await theirs.GetAsync("/training/profile")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await theirs.GetAsync("/training/weeks/current")).StatusCode);
    }

    [Fact]
    public async Task The_shared_catalogue_and_the_key_ring_survive()
    {
        // Reads the context directly, and the standing rule in backend/CLAUDE.md does not reach
        // this case: neither table has an endpoint that could answer, and what is asserted is
        // precisely that rows *nobody asked about* were left alone. Going through the API here
        // would prove the opposite of what is wanted — that the caller's own data is gone.
        //
        // `exercises` is a global seed every other account's stored weeks reference (root
        // standard 7). The Data Protection key ring is what makes every *other* user's stored Hevy
        // key decryptable (ADR-014) — dropping it destroys credentials belonging to people who did
        // not ask for anything, and nothing fails until they next sync.
        var client = EraseEnabledClient();
        await SignInAsync(client);
        await GiveThemSomethingToLoseAsync(client);

        int catalogueBefore;
        int keysBefore;
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            catalogueBefore = await db.Exercises.CountAsync();
            keysBefore = await db.DataProtectionKeys.CountAsync();
        }

        Assert.True(catalogueBefore > 0, "the catalogue should have been seeded before this ran");

        await client.PostAsJsonAsync("/training/erase", new { confirmed = true });

        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            Assert.Equal(catalogueBefore, await db.Exercises.CountAsync());
            Assert.Equal(keysBefore, await db.DataProtectionKeys.CountAsync());
        }
    }

    private sealed record EquipmentReading(IReadOnlyList<string> Items, IReadOnlyList<string> Vocabulary);
}
