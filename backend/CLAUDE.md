# backend

The .NET 10 API. The repo-wide standards in the root `CLAUDE.md` apply here too — canonical
units, UTC, append-only history, codes instead of display text — and this file only covers what
is specific to this tier.

## Layout

```
Protocol.slnx                      the solution (.NET 10 emits .slnx, not .sln)
Protocol.Api/
  Program.cs                       composition root: CORS, Identity, cookie, health, endpoints
  Auth/                            AppUser, AppDbContext, AuthEndpoints, DatabaseMigrator
  Training/                        the domain: catalogue, profile, equipment, generator, weeks
  Hevy/                            the boundary: client, contracts, both mappers, key protection
  Migrations/                      EF Core migrations, forward-only
  appsettings.json                 defaults; every value is overridable by environment
Protocol.Api.Tests.Unit/           xUnit, no I/O
Protocol.Api.Tests.Integration/    WebApplicationFactory over a Testcontainers Postgres
global.json                        pins the SDK; the machine has several installed
.config/dotnet-tools.json          dotnet-ef as a local tool
Dockerfile                         runtime image (build on the SDK, ship on the runtime)
Dockerfile.tests                   containerized test runner
```

## Commands

```
dotnet build                                   whole solution
dotnet test Protocol.slnx                      unit + integration
dotnet test Protocol.Api.Tests.Unit            unit only, no Docker needed
dotnet run --project Protocol.Api              dev loop; needs the compose postgres up
dotnet tool restore                            once per clone, for dotnet-ef
dotnet dotnet-ef migrations add <Name> --project Protocol.Api --output-dir Migrations
```

The integration suite starts its own Postgres through Testcontainers, so it needs Docker but
not the compose stack — it is isolated by construction and has never been able to touch the
development database. The containerized run is what a change ships on:

```
docker compose -f docker-compose.test.yml run --rm --build backend-tests   # from the repo root
```

## Invariants

- **Read and write share one API here.** The read/write split is an MCP convention, not a
  repo-wide one.
- **Identity owns authentication.** `MapIdentityApi` provides register, login and refresh; only
  what it leaves out — `/auth/me` and `/auth/logout` — is written by hand in `AuthEndpoints`.
  Do not reimplement what the framework already exposes.
- **The session is a cookie, not a token.** `Auth:Cookie:SameSite` defaults to `Lax`, correct
  while the API and the frontend share a site; splitting them across domains requires `None`,
  which browsers honour only over HTTPS.
- **Migrations run from a hosted service**, never between `builder.Build()` and `app.Run()`.
  Code in that gap also executes under `dotnet ef`, which would make every design-time command
  require a live database. `DatabaseMigrator` exists for exactly this reason.
- **JSON is camelCase**, which is the ASP.NET Core default. The frontend depends on it; do not
  change the serializer's naming policy without changing the frontend in the same commit.
- **Configuration is overridable by environment.** `appsettings.json` holds defaults only;
  compose passes `ConnectionStrings__Postgres` and `Frontend__Origin`. No credential is ever
  committed.
- **The Hevy integration lives in `Hevy/`**, as an ordinary outbound HTTP client. The MCP server
  under `mcps/hevy` is exploration tooling for a session and must never be part of a request
  path.
- **`IHevyClient` is the only way out.** Every call to Hevy goes through it, which is what keeps
  their shape out of `Training/` (root standard 17) and what lets the suites substitute the whole
  service. `ApiFactory` replaces it for every integration test, so no suite can reach
  `api.hevyapp.com` even by accident.
- **The Data Protection key ring is persisted to the database**, not to the container's
  filesystem, and the application name is pinned. Both are load-bearing: an ephemeral ring or a
  name derived from the content root would leave every stored Hevy key silently undecryptable
  after a restart (`ADR-014`).

## Testing

Unit tests cover pure logic and take no dependency on I/O. Integration tests drive the real
application over HTTP against a real Postgres, because that is what catches the things a
substitute hides.

Assert through the API, not against the database. A test that reads tables couples itself to
the schema and fails on migrations that broke nothing.

The one standing exception is seeded reference data with no endpoint — `ExerciseCatalogueTests`
reads the context directly, because what it asserts *is* the seed contract, which is exactly
what should fail when it stops holding. Give a test that reads tables a comment saying why the
rule does not apply to it, or it reads as one that forgot the rule.
