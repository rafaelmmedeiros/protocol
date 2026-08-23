using Microsoft.AspNetCore.DataProtection;
using Protocol.Api.Hevy;

namespace Protocol.Api.Tests.Unit.Hevy;

/// <summary>
/// The key is encrypted at rest and is readable again (ADR-014).
/// </summary>
public class HevyKeyProtectionTests
{
    private static HevyKeyProtector Protector(IDataProtectionProvider? provider = null) =>
        new(provider ?? new EphemeralDataProtectionProvider());

    [Fact]
    public void A_protected_key_round_trips()
    {
        var protector = Protector();
        const string key = "1f9c8e42-0000-4a1b-9c3d-abcdefabcdef";

        Assert.Equal(key, protector.Unprotect(protector.Protect(key)));
    }

    [Fact]
    public void Ciphertext_does_not_contain_the_key()
    {
        // The point of the record: what lands in the column must not be the key, and must not
        // merely be the key wearing an encoding. Substring is the crude check that catches the
        // crude mistake -- storing it verbatim, or base64 of it, would both fail here.
        var protector = Protector();
        const string key = "1f9c8e42-0000-4a1b-9c3d-abcdefabcdef";

        var ciphertext = protector.Protect(key);

        Assert.DoesNotContain(key, ciphertext, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(
            Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(key)),
            ciphertext,
            StringComparison.Ordinal);
    }

    [Fact]
    public void The_same_key_protects_to_different_ciphertext_each_time()
    {
        // Data Protection is randomised, so two rows holding the same key are not comparable by
        // their ciphertext. Asserted because the opposite would be a real weakness and because
        // it forecloses ever using the column to answer "is this the same key as before".
        var protector = Protector();
        const string key = "1f9c8e42-0000-4a1b-9c3d-abcdefabcdef";

        Assert.NotEqual(protector.Protect(key), protector.Protect(key));
    }

    [Fact]
    public void A_key_protected_under_one_key_ring_cannot_be_read_by_another()
    {
        // This is the failure ADR-014 names, reproduced deliberately: a second key ring cannot
        // read the first one's ciphertext. It is why the ring is persisted to the database, and
        // why a container restart that lost it would silently orphan every stored key.
        var written = Protector().Protect("1f9c8e42-0000-4a1b-9c3d-abcdefabcdef");
        var other = Protector();

        Assert.ThrowsAny<Exception>(() => other.Unprotect(written));
    }
}
