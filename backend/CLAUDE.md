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
- **Startup work runs from hosted services, in registration order, and the order is load-bearing.**
  `DatabaseMigrator` → `ExerciseCatalogueSeeder` → `PerformedExerciseRemapper`. There is no table
  to seed before the migrations run, and nothing new to remap before the new catalogue rows exist.
  Reordering these registrations breaks things that fail silently rather than loudly — a remap that
  finds nothing logs that it found nothing and the coverage number simply stays wrong.
- **A plan is an ordered queue, and `WeekStartDate` and `Day` are history rather than fields.**
  Both columns are nullable and hold values only on rows generated before `ADR-027`; nothing
  writes them now. They are kept because a week that *was* anchored still means what it meant
  (root standard 7, `ADR-003`), and the week screen renders both shapes from the same component.
  Re-populating either is not a small convenience — it reintroduces the systematic per-muscle
  deficit the queue exists to remove.
- **Only a declaration is stored; a binding is derived on every read.** A session carries `Declared`
  (marked or skipped) because a statement cannot be recomputed from anything, while whether a
  workout bound to it is a join on `routine_id` (`ADR-019`) and is read fresh each time. That is
  `ADR-029`'s test — *could this be recomputed from data already stored?* — and it pays for itself:
  a workout deleted upstream stops binding its session, which a stored column would have got wrong
  in silence.
- **The generator fills a week in two passes, week-wide, and the order is load-bearing.** Pass 1
  takes every session to `TD-014`'s guaranteed target against no ceiling; pass 2 then spends the
  minutes the user declared and pass 1 did not need, bounded by `TD-022`'s ceiling over **every**
  muscle a slot credits, secondary roles included. Collapsing the two into one loop per session
  is the obvious-looking refactor and it silently breaks the bound: a later session's unbounded
  pass-1 draw lands on top of volume the ceiling already bought. That arrangement was measured at
  9.0 against a ceiling of 8.0 in ten of fifteen muscle groups, and it compiles, passes every
  other test, and produces a plausible week.
- **A derived column is refreshed by a hosted service, never by a migration.** Both the seeder's
  requirements backfill and `PerformedExerciseRemapper` (`ADR-026`) exist because the need recurs:
  every catalogue widening reopens the same gap. A migration would close today's instance and leave
  the next to be discovered the way this one was — by measuring real data and being surprised.
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
