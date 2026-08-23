using Microsoft.AspNetCore.DataProtection;

namespace Protocol.Api.Hevy;

/// <summary>
/// Encrypts and decrypts a user's Hevy key (ADR-014).
/// <para>
/// A named wrapper rather than an inline <see cref="IDataProtector"/> for one reason: the
/// purpose string is the encryption boundary. Two call sites that spell it differently produce
/// ciphertext neither can read, and the failure appears at decryption time, far from the typo.
/// Naming it once here makes that impossible.
/// </para>
/// </summary>
public sealed class HevyKeyProtector(IDataProtectionProvider provider)
{
    /// <summary>
    /// The purpose. Versioned so that a future change of scheme can be introduced beside this
    /// one rather than silently invalidating every key already stored.
    /// </summary>
    private const string Purpose = "Protocol.Hevy.ApiKey.v1";

    private readonly IDataProtector _protector = provider.CreateProtector(Purpose);

    public string Protect(string apiKey) => _protector.Protect(apiKey);

    public string Unprotect(string protectedApiKey) => _protector.Unprotect(protectedApiKey);
}
