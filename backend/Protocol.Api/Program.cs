using Microsoft.EntityFrameworkCore;
using Protocol.Api.Auth;
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

app.Run();

/// <summary>Exposed so the integration tests can host this application in-process.</summary>
public partial class Program;
