# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

It is deliberately short. It holds what is true everywhere: what the system is for, the
standards that bind every tier, and a map of where the rest lives. Anything that only matters
inside one tier belongs to that tier's own `CLAUDE.md`.

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

The MCP servers are not part of that. `mcps/hevy` is exploration tooling: it exists so the
training data can be inspected from a session while designing a feature, and it never runs in
the product's path. The system reaches Hevy through its own integration in the backend, as an
external connection like any other. Read this distinction before wiring anything: an MCP server
here is harness, not runtime.

It grows on two fronts, and they are different kinds of knowledge:

- **Harness knowledge** -- how work is done in this repo. It accumulates in these files, in
  `.claude/skills/`, and in `.claude/harness/BACKLOG.md`, and only ever from observed pain.
- **Strength-training knowledge** -- periodization, volume and intensity, exercise selection,
  fatigue management. This is domain knowledge and belongs in a skill, so that it enters a
  session only when a feature actually consults it. It gets written when that feature exists.

Deployment is local for now: one user, one machine, `docker-compose.app.yml`. Publishing comes
when there are users to publish for. One decision already anticipates it -- the auth cookie's
`SameSite` is configurable precisely because a hosted deployment may split the API and the
frontend across domains.

## Standards

These bind every tier. Each one is cheap to hold now and expensive to retrofit: most would
cost a data migration, or a recount of every analysis already produced, if adopted late.

### Language

1. **The code is English; the product is not.** Source, identifiers, comments, docs, commit
   messages and CLI output are written in English. Conversation with the engineer may be in
   Portuguese or English.
2. **The product is multilingual from birth: `en-US` is the default, `pt-BR` is supported.**
   No user-visible string is hardcoded in a component. This is not a feature to add later —
   retrofitting it means touching every screen ever written.
3. **The backend returns codes, never display text.** An error carries a stable
   machine-readable code plus whatever data the message needs; the frontend owns every
   translated string. A translated string is not an identity: the moment code branches on
   message text, translating breaks behaviour.

### Data

4. **Store canonical units, convert only at the render edge.** Weight is kilograms, distance is
   metres, duration is seconds, and the unit lives in the field name — `weight_kg`,
   `distance_meters`, `duration_seconds`, exactly as the Hevy API already does. Pounds and
   miles exist only in what a user sees, decided by their locale or preference.
5. **Store UTC, transmit ISO 8601, localise only at render.**
6. **The training week starts on Monday, always.** It is a periodization convention, not a
   calendar one, and it is never derived from locale — `en-US` starting the week on Sunday
   must not redraw the boundaries of an existing training block. Two screens disagreeing about
   which week a session belongs to reads as an analysis bug, not a formatting one.
7. **Training history is append-only.** It is the substrate every analysis stands on. An
   imported record is never mutated or deleted; a correction arrives as a new record. Hevy
   exposes `updated_at`, so workouts do change upstream — re-import must reconcile without
   destroying what earlier analysis was computed against.
8. **External identifiers stay external.** Hevy's `id` and `exercise_template_id` are their
   namespace: store them as explicit external keys beside our own identifier, never as a
   primary key.
9. **An exercise is identified by `exercise_template_id`; its title is display only.** Titles
   arrive in English and are shown as they arrive for now. Never match, group, key or compare
   on a title — that is what keeps the history intact if the naming is ever translated or
   reorganised.

### Operations

10. **Migrations are forward-only.** A migration that has been applied anywhere is never
    edited; a mistake is corrected by a new migration.
11. **Secrets come from the environment.** `.env` files and environment variables only, never
    a credential in tracked source. `.env` is gitignored at the repo root.
12. **Logs are structured, and a request is traceable across tiers** by a correlation
    identifier.
13. **Accessibility is a baseline, not a pass at the end.** Semantic elements, labelled
    inputs, keyboard reachability. Another thing that is nearly free now and miserable later.

## Where documentation lives

Four homes, chosen by when the reader needs the content — not by topic:

| Home | Holds | Loaded |
|------|-------|--------|
| This file | Product, cross-cutting standards, the map | Always |
| `backend/CLAUDE.md`, `frontend/CLAUDE.md`, `mcps/CLAUDE.md` | That tier's layout, commands and invariants | When working in that tier |
| `.claude/skills/` | Procedure to execute (`/protocol-feature`, `/protocol-harness`) and, later, strength-training reference | On invocation |
| `.claude/harness/BACKLOG.md` | Pains, wins and ideas about how the work goes | Through `/protocol-harness` |

A convention must never depend on being remembered: anything that would be wrong to write
without knowing it goes in a `CLAUDE.md`, never in a skill, because a skill only helps when
something invokes it.

## Layout

```
backend/              the .NET 10 API           -> backend/CLAUDE.md
frontend/             the Next.js 16 app        -> frontend/CLAUDE.md
mcps/<name>/          exploration MCP servers   -> mcps/CLAUDE.md
docker-compose.app.yml  the runnable application stack (compose project protocol-project)
docker-compose.yml      builds the MCP images only (compose project protocol-mcps)
.mcp.json             registers every MCP server for this project
.claude/              skills and the harness backlog
```

The two compose files are opposites and are kept apart on purpose: `docker-compose.app.yml`
runs a stack, the root `docker-compose.yml` only builds images. Their compose projects differ
so the containers stay in separate groups.

## Running the stack

```
docker compose -f docker-compose.app.yml up -d --build     # postgres + api + web
docker compose -f docker-compose.app.yml ps                # every service should read healthy
docker compose -f docker-compose.app.yml down              # add -v to drop the database volume
docker compose -f docker-compose.app.yml logs -f api       # follow one service
```

The frontend is at `http://localhost:3000`, the API at `http://localhost:8080`. The browser
only ever calls the frontend's origin, which proxies to the API; server-side code reaches the
API at `http://api:8080`.

The database always runs in Docker — the compose service in normal use, a throwaway
Testcontainers instance for the integration suite. Nothing expects a Postgres on the host.

Both suites also run containerized, which is what proves a change actually ships:

```
docker compose -f docker-compose.app.yml --profile test run --rm backend-tests
docker compose -f docker-compose.app.yml --profile test run --rm e2e
```

An image caches the source at build time. After editing a tier, rebuild it —
`up -d --build <service>` — or the container keeps serving the old code.

## Direction

The backend and the frontend have landed as a walking skeleton; the MCP servers predate them.
Between them they are plumbing: nothing yet reads a training history or generates a session,
which is the whole point of the product above.

Skills, agents, and further tooling are added when work demands them, not in anticipation.
That applies to the training knowledge as much as to the harness -- it gets a home when a
feature needs to consult it, not before. Update these files as those land.

## Harness

The tooling that supports the work -- these files, `.claude/skills/`, `.mcp.json`, the build
and dev loops -- evolves the same way the architecture does: from observed pain, never ahead of
it. `/protocol-harness` is the trigger for that meta work; `.claude/harness/BACKLOG.md` records
the pains, wins, and ideas, and a rule that hardens graduates from there into a `CLAUDE.md`.

`/protocol-feature` is the counterpart for product work: the workflow for building a feature
across the tiers, with the verification ladder that ends at a green containerized stack. It was
written from the walking-skeleton build, not ahead of it, and every step in it earned its place
by costing something.
