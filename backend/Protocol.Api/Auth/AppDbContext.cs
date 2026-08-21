using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Protocol.Api.Auth;

/// <summary>
/// The single EF Core context. Identity owns every table it declares; application tables are
/// added here as features land.
/// </summary>
public sealed class AppDbContext(DbContextOptions<AppDbContext> options)
    : IdentityDbContext<AppUser>(options)
{
}
