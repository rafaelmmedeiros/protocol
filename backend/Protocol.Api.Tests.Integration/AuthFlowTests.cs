using System.Net;
using System.Net.Http.Json;

namespace Protocol.Api.Tests.Integration;

/// <summary>
/// The walking skeleton's proof: a real request path from register through login to a
/// protected endpoint, against a real Postgres.
/// </summary>
public class AuthFlowTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    private const string Password = "Passw0rd!";

    [Fact]
    public async Task Health_reports_the_database_as_reachable()
    {
        var response = await factory.CreateClient().GetAsync("/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("Healthy", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Me_is_rejected_without_a_session()
    {
        var response = await factory.CreateClient().GetAsync("/auth/me");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Register_then_login_yields_a_session_that_me_recognises()
    {
        var email = $"{Guid.NewGuid():N}@protocol.test";
        var client = factory.CreateClient();

        var register = await client.PostAsJsonAsync("/auth/register", new { email, password = Password });
        Assert.Equal(HttpStatusCode.OK, register.StatusCode);

        var login = await client.PostAsJsonAsync("/auth/login?useCookies=true", new { email, password = Password });
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);

        var me = await client.GetFromJsonAsync<CurrentUserResponse>("/auth/me");
        Assert.NotNull(me);
        Assert.Equal(email, me!.Email);
        Assert.False(string.IsNullOrWhiteSpace(me.Id));
    }

    [Fact]
    public async Task Logout_ends_the_session()
    {
        var email = $"{Guid.NewGuid():N}@protocol.test";
        var client = factory.CreateClient();
        await client.PostAsJsonAsync("/auth/register", new { email, password = Password });
        await client.PostAsJsonAsync("/auth/login?useCookies=true", new { email, password = Password });

        var logout = await client.PostAsync("/auth/logout", null);
        Assert.Equal(HttpStatusCode.NoContent, logout.StatusCode);

        var me = await client.GetAsync("/auth/me");
        Assert.Equal(HttpStatusCode.Unauthorized, me.StatusCode);
    }

    [Fact]
    public async Task Login_with_a_wrong_password_is_rejected()
    {
        var email = $"{Guid.NewGuid():N}@protocol.test";
        var client = factory.CreateClient();
        await client.PostAsJsonAsync("/auth/register", new { email, password = Password });

        var login = await client.PostAsJsonAsync("/auth/login?useCookies=true", new { email, password = "Wrong1!" });

        Assert.Equal(HttpStatusCode.Unauthorized, login.StatusCode);
    }

    private sealed record CurrentUserResponse(string Id, string Email);
}
