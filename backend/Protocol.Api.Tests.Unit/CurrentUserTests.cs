using System.Security.Claims;
using Protocol.Api.Auth;

namespace Protocol.Api.Tests.Unit;

public class CurrentUserTests
{
    [Fact]
    public void From_reads_the_identifier_and_the_explicit_email_claim()
    {
        var principal = PrincipalWith(
            (ClaimTypes.NameIdentifier, "user-1"),
            (ClaimTypes.Email, "engineer@protocol.test"),
            (ClaimTypes.Name, "ignored@protocol.test"));

        var user = CurrentUser.From(principal);

        Assert.Equal("user-1", user.Id);
        Assert.Equal("engineer@protocol.test", user.Email);
    }

    [Fact]
    public void From_falls_back_to_the_name_claim_when_no_email_claim_is_issued()
    {
        var principal = PrincipalWith(
            (ClaimTypes.NameIdentifier, "user-2"),
            (ClaimTypes.Name, "engineer@protocol.test"));

        Assert.Equal("engineer@protocol.test", CurrentUser.From(principal).Email);
    }

    [Fact]
    public void From_rejects_a_principal_without_an_identifier()
    {
        var principal = PrincipalWith((ClaimTypes.Name, "engineer@protocol.test"));

        Assert.Throws<InvalidOperationException>(() => CurrentUser.From(principal));
    }

    private static ClaimsPrincipal PrincipalWith(params (string Type, string Value)[] claims) =>
        new(new ClaimsIdentity(claims.Select(c => new Claim(c.Type, c.Value)), "test"));
}
