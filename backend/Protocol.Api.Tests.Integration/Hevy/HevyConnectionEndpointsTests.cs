using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Protocol.Api.Auth;
using Protocol.Api.Hevy;
using Protocol.Api.Training;

namespace Protocol.Api.Tests.Integration.Hevy;

/// <summary>
/// Connecting a Hevy account: the key is validated before it is stored, encrypted at rest, and
/// never returned (ADR-014).
/// </summary>
public class HevyConnectionEndpointsTests(ApiFactory factory) : IClassFixture<ApiFactory>
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

    /// <summary>
    /// Who the client is signed in as. Several tests in this class create connections against
    /// the same throwaway database, so anything reading a row has to name its own user rather
    /// than trusting that only one row exists.
    /// </summary>
    private static async Task<string> UserIdOf(HttpClient client) =>
        (await client.GetFromJsonAsync<CurrentUser>("/auth/me"))!.Id;

    [Fact]
    public async Task The_connection_is_unreachable_without_a_session()
    {
        var client = factory.CreateClient();

        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/hevy/connection")).StatusCode);
        Assert.Equal(
            HttpStatusCode.Unauthorized,
            (await client.PutAsJsonAsync("/hevy/connection", new { apiKey = StubHevyClient.ValidKey })).StatusCode);
    }

    [Fact]
    public async Task A_new_user_is_not_connected()
    {
        var client = await SignedInClientAsync();

        var connection = await client.GetFromJsonAsync<HevyConnectionResponse>("/hevy/connection");

        Assert.NotNull(connection);
        Assert.False(connection.Connected);
        Assert.Null(connection.ConnectedAt);
    }

    [Fact]
    public async Task A_valid_key_connects_the_account()
    {
        var client = await SignedInClientAsync();

        var saved = await client.PutAsJsonAsync("/hevy/connection", new { apiKey = StubHevyClient.ValidKey });
        Assert.Equal(HttpStatusCode.OK, saved.StatusCode);

        var connection = await client.GetFromJsonAsync<HevyConnectionResponse>("/hevy/connection");

        Assert.NotNull(connection);
        Assert.True(connection.Connected);
        Assert.NotNull(connection.ConnectedAt);
    }

    [Fact]
    public async Task A_key_Hevy_rejects_is_refused_and_not_stored()
    {
        // The whole point of validating on save: a typo fails while the user is still looking at
        // the field, instead of silently at the first sync days later.
        var client = await SignedInClientAsync();

        var saved = await client.PutAsJsonAsync("/hevy/connection", new { apiKey = StubHevyClient.InvalidKey });

        Assert.Equal(HttpStatusCode.BadRequest, saved.StatusCode);
        Assert.Equal(HevyErrorCodes.HevyKeyInvalid, (await saved.Content.ReadFromJsonAsync<ApiError>())?.Code);

        var connection = await client.GetFromJsonAsync<HevyConnectionResponse>("/hevy/connection");
        Assert.False(connection!.Connected);
    }

    [Fact]
    public async Task A_key_that_could_not_be_checked_is_not_stored_either()
    {
        // "Hevy is down" and "your key is wrong" are different answers, and neither of them is
        // "connected". Storing an unverified key would put the screen into a state the system
        // cannot back up.
        var client = await SignedInClientAsync();

        var saved = await client.PutAsJsonAsync("/hevy/connection", new { apiKey = StubHevyClient.UnreachableKey });

        Assert.Equal(HttpStatusCode.BadGateway, saved.StatusCode);
        Assert.Equal(HevyErrorCodes.HevyUnreachable, (await saved.Content.ReadFromJsonAsync<ApiError>())?.Code);

        var connection = await client.GetFromJsonAsync<HevyConnectionResponse>("/hevy/connection");
        Assert.False(connection!.Connected);
    }

    [Fact]
    public async Task An_empty_key_is_refused_without_asking_Hevy()
    {
        var client = await SignedInClientAsync();

        var saved = await client.PutAsJsonAsync("/hevy/connection", new { apiKey = "   " });

        Assert.Equal(HttpStatusCode.BadRequest, saved.StatusCode);
        Assert.Equal(HevyErrorCodes.HevyKeyInvalid, (await saved.Content.ReadFromJsonAsync<ApiError>())?.Code);
    }

    [Fact]
    public async Task No_response_ever_carries_the_key()
    {
        // ADR-014's hard rule, asserted against the raw body rather than a typed model -- a
        // deserialised record cannot show a field that was added to the response by accident.
        var client = await SignedInClientAsync();

        var saved = await client.PutAsJsonAsync("/hevy/connection", new { apiKey = StubHevyClient.ValidKey });
        var read = await client.GetAsync("/hevy/connection");

        Assert.DoesNotContain(
            StubHevyClient.ValidKey,
            await saved.Content.ReadAsStringAsync(),
            StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(
            StubHevyClient.ValidKey,
            await read.Content.ReadAsStringAsync(),
            StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Reconnecting_replaces_the_key_and_keeps_the_sync_cursor()
    {
        // Replacing a key is re-authenticating the same account, not starting a new history.
        // Resetting the cursor here would re-import everything already read (ADR-018).
        var client = await SignedInClientAsync();
        var userId = await UserIdOf(client);
        await client.PutAsJsonAsync("/hevy/connection", new { apiKey = StubHevyClient.ValidKey });

        var cursor = new DateTimeOffset(2026, 8, 1, 0, 0, 0, TimeSpan.Zero);

        using (var scope = factory.Services.CreateScope())
        {
            // Reaching into the context because a sync cursor has no endpoint yet -- S3.4 is what
            // sets it. The standing exception in backend/CLAUDE.md is for exactly this: state
            // with no read path of its own.
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var stored = await db.HevyConnections.SingleAsync(c => c.UserId == userId);
            stored.SyncCursor = cursor;
            await db.SaveChangesAsync();
        }

        await client.PutAsJsonAsync("/hevy/connection", new { apiKey = StubHevyClient.ValidKey });

        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var stored = await db.HevyConnections.AsNoTracking().SingleAsync(c => c.UserId == userId);
            Assert.Equal(cursor, stored.SyncCursor);
        }
    }

    [Fact]
    public async Task A_stored_key_still_decrypts_after_the_host_restarts()
    {
        // The trap ADR-014 names, proved rather than asserted. A second host is stood up on the
        // same database -- which is what a container restart is -- and the key saved by the
        // first one is read back through the second one's protector. With an ephemeral key ring
        // this throws, and every stored key in production would have been silently orphaned.
        var client = await SignedInClientAsync();
        var userId = await UserIdOf(client);
        await client.PutAsJsonAsync("/hevy/connection", new { apiKey = StubHevyClient.ValidKey });

        await using var restarted = new RestartedApi(factory.ConnectionString);
        _ = restarted.CreateClient();

        using var scope = restarted.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var protector = scope.ServiceProvider.GetRequiredService<HevyKeyProtector>();

        var stored = await db.HevyConnections.AsNoTracking().SingleAsync(c => c.UserId == userId);

        Assert.Equal(StubHevyClient.ValidKey, protector.Unprotect(stored.ProtectedApiKey));
    }
}

/// <summary>The same application, started again over the same database.</summary>
public sealed class RestartedApi(string connectionString) : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment(Environments.Development);
        builder.UseSetting("ConnectionStrings:Postgres", connectionString);
        builder.ConfigureServices(ApiFactory.UseStubHevy);
    }
}
