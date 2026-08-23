using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Protocol.Api.Hevy;
using Testcontainers.PostgreSql;

namespace Protocol.Api.Tests.Integration;

/// <summary>
/// Hosts the API in-process against a throwaway Postgres container, so integration tests run
/// against the real database engine instead of a substitute.
/// </summary>
public sealed class ApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder("postgres:18-alpine").Build();

    /// <summary>
    /// The throwaway database, so a test can stand a *second* host on the same data — which is
    /// how the key ring's survival across a restart is proved rather than asserted.
    /// </summary>
    public string ConnectionString => _postgres.GetConnectionString();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment(Environments.Development);
        builder.UseSetting("ConnectionStrings:Postgres", _postgres.GetConnectionString());
        builder.ConfigureServices(UseStubHevy);
    }

    /// <summary>
    /// Replaces the real Hevy client everywhere in the suite.
    /// <para>
    /// Not a convenience. A test that validated a key against api.hevyapp.com would depend on
    /// their uptime, spend a real account's rate budget, and need a real credential in CI. This
    /// registration is what makes "no test run touches the real Hevy account" true by
    /// construction rather than by everyone remembering.
    /// </para>
    /// </summary>
    public static void UseStubHevy(IServiceCollection services)
    {
        // AddHttpClient registered the typed client; remove it before substituting, or both
        // resolve and the last one wins by accident rather than on purpose.
        services.RemoveAll<IHevyClient>();
        services.AddSingleton<IHevyClient, StubHevyClient>();
    }

    public async Task InitializeAsync() => await _postgres.StartAsync();

    public new async Task DisposeAsync()
    {
        await base.DisposeAsync();
        await _postgres.DisposeAsync();
    }
}

/// <summary>
/// Hevy, as the suite sees it. Deterministic by key prefix so a test states its intent in the
/// value it sends rather than in setup.
/// </summary>
public sealed class StubHevyClient : IHevyClient
{
    /// <summary>A key Hevy accepts.</summary>
    public const string ValidKey = "valid-0000-4a1b-9c3d-abcdefabcdef";

    /// <summary>A key Hevy answers about, and rejects.</summary>
    public const string InvalidKey = "wrong-0000-4a1b-9c3d-abcdefabcdef";

    /// <summary>A key Hevy never answers about at all.</summary>
    public const string UnreachableKey = "unreachable-4a1b-9c3d-abcdefabcd";

    public Task<HevyKeyCheck> CheckKeyAsync(string apiKey, CancellationToken token) =>
        Task.FromResult(apiKey switch
        {
            ValidKey => HevyKeyCheck.Valid,
            UnreachableKey => HevyKeyCheck.Unreachable,
            _ => HevyKeyCheck.Invalid,
        });
}
