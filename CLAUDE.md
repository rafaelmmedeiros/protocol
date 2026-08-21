# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Status

Early. The application tiers exist as a walking skeleton (login, end to end) and have build and
test tooling; the MCP servers still have neither — do not invent commands that don't exist for
them. Beyond what is written here the architecture lives in the engineer's head; follow their
direction rather than assuming a structure.

## Product

Hevy is the logging surface, and it is very good at that: fast enough to use between sets, and
it exposes an API. What it does not do is think. It will not periodize, will not read a
training history back to you, will not tell you whether the last two months actually
progressed. Logging is solved; judgement is not.

This system is that missing intelligence. It reads training out of Hevy, reasons about it, and
generates training back into it. Hevy stays where sets get logged -- there is no intention of
replacing it -- and the reasoning lives here.

That purpose is what the MCP split already serves: `mcps/hevy` is the read path, read-only by
construction, so analysis can never mutate a training log; a separate `hevy-write` is how
generated programming gets back into the app. The invariant is not fussiness, it is the shape
of the product.

It grows on two fronts, and they are different kinds of knowledge:

- **Harness knowledge** -- how work is done in this repo. It accumulates in this file, in
  `.claude/skills/`, and in `.claude/harness/BACKLOG.md`, and only ever from observed pain.
- **Strength-training knowledge** -- periodization, volume and intensity, exercise selection,
  fatigue management. This is domain knowledge, and it has to be captured somewhere deliberate
  rather than re-derived from scratch each session. Where it lives is not decided yet; it will
  be, when the first feature needs to consult it.

Deployment is local for now: one user, one machine, `docker-compose.app.yml`. Publishing comes
when there are users to publish for. One decision already anticipates it -- the auth cookie's
`SameSite` is configurable precisely because a hosted deployment may split the API and the
frontend across domains.

## Rules

1. All tooling and code — source, identifiers, comments, docs, commit messages, CLI output — is written
   in English. Conversation with the engineer may be in Portuguese or English.

## Layout

```
backend/              the .NET 10 API (solution Protocol.slnx)
  Protocol.Api/       minimal API; Auth/ holds Identity, the DbContext and the endpoints
  Protocol.Api.Tests.Unit/         xUnit, no I/O
  Protocol.Api.Tests.Integration/  xUnit over WebApplicationFactory + Testcontainers Postgres
  global.json         pins the SDK; the machine has several installed
  .config/dotnet-tools.json        dotnet-ef as a local tool, restored with `dotnet tool restore`
  Dockerfile / Dockerfile.tests    runtime image / containerized test runner
frontend/             the Next.js 16 app (App Router, TypeScript, Tailwind)
  app/                pages; app/api/[...path]/ proxies the browser's calls to the API
  lib/                api.ts, session.ts, problem.ts — the non-React logic, unit tested
  e2e/                Playwright specs, run against a stack that is already up
  AGENTS.md           written by Next itself; committed on purpose, see the invariants
  Dockerfile / Dockerfile.e2e      runtime image / Playwright runner
docker-compose.app.yml  the runnable application stack (project protocol-project)
mcps/<name>/          one MCP server per directory; more are coming
  src/<pkg>/          src-layout package, installed by uv/pip (never a loose script)
    config.py         API constants, .env loading, credential lookup
    client.py         the only network access for that server
    server.py         builds the MCPServer instance, registers the tool modules
    tools/            one module per resource group, each exposing register(mcp)
  pyproject.toml      declares a console script named <name>-mcp
  Dockerfile / docker-compose.yml   builds mcp-<name>:latest
  .env / .env.example credentials, .env gitignored at the repo root
.mcp.json             registers every server for this project
```

Conventions that should hold for each new server: package under `src/`, network access
confined to one module, tools grouped by resource and registered through `register(mcp)`,
read and write split into separate servers (`hevy` / `hevy-write`) so a read-only server
stays read-only by construction. `C:/Users/rafae/Projects/MCPs` holds unrelated servers on
an older, flatter version of this shape -- reference only, not a destination for code.

## Running the application stack

`docker-compose.app.yml` is the opposite of the root `docker-compose.yml`: it is meant to be
brought up and left running. Compose project `protocol-project`, images
`protocol-project/<service>:latest`. The MCP images stay in `protocol-mcps`, separate.

```
docker compose -f docker-compose.app.yml up -d --build     # postgres + api + web
docker compose -f docker-compose.app.yml ps                # every service should read healthy
docker compose -f docker-compose.app.yml down              # add -v to drop the database volume
```

The frontend is at `http://localhost:3000`, the API at `http://localhost:8080`. The browser
only ever calls the frontend's origin: `app/api/[...path]/route.ts` proxies to the API, which
keeps the Identity cookie first-party and takes CORS out of the browser's path. Server-side
code reaches the API at `http://api:8080` via `API_URL`.

The database always runs in Docker — the compose service in normal use, and a throwaway
Testcontainers instance for the integration suite. Nothing expects a Postgres on the host.

Dev loop without the images:

```
cd backend  && dotnet run --project Protocol.Api    # needs the compose postgres up
cd frontend && npm run dev
```

As with the MCP servers, an image caches the source at build time: after editing a tier,
`docker compose -f docker-compose.app.yml up -d --build <service>` or the container keeps
serving the old code.

## Testing

```
cd backend  && dotnet test Protocol.slnx     # unit + integration; integration starts its own Postgres
cd frontend && npm run typecheck && npm test # tsc, then vitest over lib/
cd frontend && npm run test:e2e              # Playwright, against a stack that is ALREADY up
```

Running Playwright on the host needs its browser once: `npx playwright install chromium`. The
containerized run does not — the Playwright image already carries it.

Both suites also run containerized, which is what proves a change actually ships:

```
docker compose -f docker-compose.app.yml --profile test run --rm backend-tests
docker compose -f docker-compose.app.yml --profile test run --rm e2e
```

Playwright never starts a server itself; `E2E_BASE_URL` points it at the running stack, so the
local and containerized runs share one code path.

## Application invariants

- Read and write share one API here; the read/write split is an MCP convention, not a
  repo-wide one.
- Identity owns authentication. `MapIdentityApi` provides register, login and refresh; only
  what it leaves out — `/auth/me` and `/auth/logout` — is written by hand in `AuthEndpoints`.
- The session is a cookie, not a token. `Auth:Cookie:SameSite` defaults to `Lax`, which is
  correct while the API and the frontend share a site; splitting them across domains requires
  `None`, which browsers honour only over HTTPS.
- Migrations run from a hosted service, never between `builder.Build()` and `app.Run()` —
  code in that gap also executes under `dotnet ef`, which would make every design-time command
  require a live database. Add one with
  `dotnet dotnet-ef migrations add <Name> --project Protocol.Api --output-dir Migrations`.
- `frontend/AGENTS.md` is generated by `next dev` and committed deliberately: Next.js 16
  differs from model training data and ships its own documentation at
  `frontend/node_modules/next/dist/docs/`. Read it before writing frontend code.
  There is no `frontend/CLAUDE.md`, and that is the stable state, not an omission.
  `next dev` writes these files only when it detects an agent, and only when the managed block
  is missing: with `AGENTS.md` holding the block it leaves `CLAUDE.md` alone, but deleting both
  makes it scaffold both again. This file stays the single source of project rules; the
  generated one only pointed back at `AGENTS.md` anyway.

## Running an MCP server

Docker is the default: `.mcp.json` spawns each server with `docker run -i --rm`, so Claude Code
starts the container itself at session start. Nothing runs between sessions.

```
docker compose build            # build every MCP image (project: protocol-mcps)
docker compose build hevy       # rebuild one after changing its source
uv run --directory D:/projects/protocol/mcps/hevy hevy-mcp    # dev loop, skips the rebuild
```

The root `docker-compose.yml` exists to BUILD images, not to run them -- an stdio server needs
its stdin wired to one client, so `docker compose up` would start a container talking to nobody.

Grouping: compose project `protocol-mcps`, images `protocol-mcps/<name>:latest`. `.mcp.json`
passes `--label com.docker.compose.project=protocol-mcps` so the ad-hoc containers land in the
same Docker Desktop group as the build.

Dependencies come from `uv.lock` in both paths: the image installs with `uv sync --locked`,
so a container and the `uv run` dev loop resolve to the same versions. Add a dependency with
`uv add`, never by hand-editing a pin, and commit the updated lock.

**Docker caches the source at build time**: after editing a server, `docker compose build <name>`
or the restarted session keeps serving the old code. Changes to `.mcp.json` also only take effect
on restart.

## mcp-hevy invariants

- Read-only: every tool goes through `client.get`, the module's single network verb. Hevy's
  write endpoints (POST/PUT on workouts, routines, body_measurements) belong in a separate
  `mcps/hevy-write` server, never in this one.
- SDK note: `mcp` 2.x renamed `FastMCP` to `MCPServer` (`mcp.server.mcpserver`) and returns
  snake_case fields (`server_info`, `input_schema`, `is_error`). The servers under
  `C:/Users/rafae/Projects/MCPs` still import `mcp.server.fastmcp` because their Docker images
  pinned 1.x -- do not copy that import into new code.
- Hevy caps `pageSize` at 10 on every list endpoint except `exercise_templates` (100), and has
  no server-side search -- hence the paging helper `hevy_recent_workouts` and the client-side
  `search` filter on `hevy_list_exercise_templates`.

## Direction

The backend and the frontend have landed as a walking skeleton; the MCP servers predate them.
Between them they are plumbing: nothing yet reads a training history or generates a session,
which is the whole point of the product above.

Skills, agents, and further tooling are added when work demands them, not in anticipation.
That applies to the training knowledge as much as to the harness -- it gets a home when a
feature needs to consult it, not before. Update this file as those land.

## Harness

The tooling that supports the work -- this file, `.claude/skills/`, `.mcp.json`, the build and
dev loops -- evolves the same way the architecture does: from observed pain, never ahead of it.
`/protocol-harness` is the trigger for that meta work; `.claude/harness/BACKLOG.md` records the
pains, wins, and ideas, and a rule that hardens graduates from there into this file.

`/protocol-feature` is the counterpart for product work: the workflow for building a feature
across the tiers, with the verification ladder that ends at a green containerized stack. It was
written from the walking-skeleton build, not ahead of it, and every step in it earned its place
by costing something.
