using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Protocol.Api.Auth;
using Protocol.Api.Hevy;
using Protocol.Api.Training;

var builder = WebApplication.CreateBuilder(args);

// The frontend runs on its own origin and authenticates with a cookie, so credentials must be
// allowed explicitly -- a wildcard origin would be rejected by the browser.
const string FrontendCors = "frontend";
var frontendOrigin = builder.Configuration["Frontend:Origin"] ?? "http://localhost:3000";

builder.Services.AddCors(options => options.AddPolicy(FrontendCors, policy => policy
    .WithOrigins(frontendOrigin)
    .AllowAnyHeader()
    .AllowAnyMethod()
    .AllowCredentials()));

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("Postgres")));

// The key ring is persisted to the database, not to the container's filesystem, which is
// ephemeral. Without this every stored Hevy key becomes undecryptable at the next restart, and
// nothing announces it (ADR-014). The application name is pinned because Data Protection
// otherwise derives it from the content root, which differs between the container and a test
// host -- and a derived name that changes is the same failure wearing a different hat.
builder.Services.AddDataProtection()
    .SetApplicationName("protocol")
    .PersistKeysToDbContext<AppDbContext>();

// Hevy is reached as an ordinary outbound HTTP client from this tier (backend/CLAUDE.md); the
// MCP server under mcps/hevy is exploration tooling and is never in a request path.
builder.Services.AddHttpClient<IHevyClient, HevyClient>(client =>
    client.BaseAddress = new Uri(builder.Configuration["Hevy:BaseUrl"] ?? "https://api.hevyapp.com/v1/"));

// The end-to-end suite runs this very container, so it cannot substitute the client in process
// the way the unit and integration suites do. Without this switch a browser test would reach
// api.hevyapp.com -- a third party's uptime, a real account's rate budget, and a credential in CI.
// Only docker-compose.test.yml sets it; the application stack never does.
if (builder.Configuration.GetValue<bool>(FakeHevyClient.EnabledKey))
{
    builder.Services.AddSingleton<IHevyClient, FakeHevyClient>();
}

builder.Services.AddSingleton<IHevyBackoff, ExponentialHevyBackoff>();
builder.Services.AddScoped<HevyKeyProtector>();
builder.Services.AddScoped<HevyWeekPusher>();
builder.Services.AddScoped<HevyHistoryImporter>();

builder.Services.AddAuthorization();
builder.Services.AddIdentityApiEndpoints<AppUser>()
    .AddEntityFrameworkStores<AppDbContext>();

// The Identity cookie is the session. SameSite=None is required because the API and the
// frontend are different origins in every environment we run.
builder.Services.ConfigureApplicationCookie(options =>
{
    options.Cookie.Name = "protocol.auth";
    options.Cookie.HttpOnly = true;
    // Lax is correct while the API and the frontend share a site (localhost on two ports is
    // same-site -- only the port differs). A deployment that splits them across domains must
    // set Auth:Cookie:SameSite to None, which browsers only honour over HTTPS.
    options.Cookie.SameSite = Enum.Parse<SameSiteMode>(
        builder.Configuration["Auth:Cookie:SameSite"] ?? nameof(SameSiteMode.Lax));
    options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
    // An API answers with status codes, never with a redirect to a login page.
    options.Events.OnRedirectToLogin = context =>
    {
        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
        return Task.CompletedTask;
    };
    options.Events.OnRedirectToAccessDenied = context =>
    {
        context.Response.StatusCode = StatusCodes.Status403Forbidden;
        return Task.CompletedTask;
    };
});

builder.Services.AddOpenApi();
builder.Services.AddHealthChecks().AddDbContextCheck<AppDbContext>();
builder.Services.AddHostedService<DatabaseMigrator>();
// Registration order is start order, and there is no table to seed until the migrator has run.
builder.Services.AddHostedService<ExerciseCatalogueSeeder>();

// After the seeder, and the order is load-bearing: hosted services start in registration order,
// and there is nothing new to map until the new catalogue rows exist (ADR-026).
builder.Services.AddHostedService<PerformedExerciseRemapper>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseCors(FrontendCors);
app.UseAuthentication();
app.UseAuthorization();

app.MapHealthChecks("/health");
app.MapGroup("/auth").MapIdentityApi<AppUser>().WithTags("Auth");
app.MapAuthEndpoints();
app.MapTrainingEndpoints();
app.MapHevyEndpoints();

app.Run();

/// <summary>Exposed so the integration tests can host this application in-process.</summary>
public partial class Program;
