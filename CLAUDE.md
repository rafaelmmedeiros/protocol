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
generates training back into it. Hevy is where sets get logged, and the reasoning lives here.

**That arrangement is a stage, not the destination.** The intended trajectory is three moves:
local until there is an MVP; then published, so that other Hevy users can train against it and
sharpen it; and eventually a logging surface of its own, at which point Hevy becomes an
integration a user may or may not have rather than the substrate everything stands on. Those
are large steps and none of them is scheduled -- but a decision taken today under the
assumption that Hevy is permanent is a decision that has to be unmade later, in data that is
append-only.

This is why the shape of the Hevy boundary is worth more than convenience anywhere it appears:
their identifiers stay in their own column (standard 8), an exercise is ours and carries theirs
beside it (`ADR-002`), and nothing keys on a Hevy value. The system should be able to lose the
Hevy connection without losing its own history, on the day it grows a logger.

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
  `/protocol-training` is its home, and it holds two things that must not be confused: what the
  literature supports, and what this product decided to do about it.

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

   **It constrains measurement and not the shape of a plan** (`ADR-027`). A generated plan is an
   ordered queue of sessions with no dates; what was *performed* is still bucketed into
   Monday-anchored weeks, and `Training/TrainingWeek` is the one place that bucketing happens.
   The two were the same thing until `M5` and reading this standard as a claim about the
   prescription is how weekday assignment would come back.
7. **Training history is append-only.** It is the substrate every analysis stands on. An
   imported record is never mutated or deleted; a correction arrives as a new record. Hevy
   exposes `updated_at`, so workouts do change upstream — re-import must reconcile without
   destroying what earlier analysis was computed against.

   **An imported row holds two kinds of thing and this protects one of them** (`ADR-026`). What
   Hevy observed — their template id, their title, every weight, repetition, set kind and reported
   effort — is immutable, full stop. What *we derived from it* is not: `ExerciseId` is this
   system's answer to "which of our exercises is that?", computed against a catalogue that changes,
   and recomputing an answer is not mutating an observation. The test before writing to any
   imported row: **could this be recomputed from data already stored, without asking Hevy
   anything?** `ExerciseId` can. A weight cannot. Anything failing that test is a re-import, not an
   update.
8. **External identifiers stay external.** Hevy's `id` and `exercise_template_id` are their
   namespace: store them as explicit external keys beside our own identifier, never as a
   primary key.
9. **An exercise is ours; a title is never an identity.** Our own key identifies an exercise
   inside this system, `exercise_template_id` is how that exercise maps to Hevy (standard 8,
   `ADR-002`), and the title is display only. Titles arrive in English and are shown as they
   arrive for now. Never match, group, key or compare on a title — that is what keeps the
   history intact if the naming is ever translated or reorganised.

### Operations

10. **Migrations are forward-only.** A migration that has been applied anywhere is never
    edited; a mistake is corrected by a new migration.
11. **Secrets come from the environment.** `.env` files and environment variables only, never
    a credential in tracked source. `.env` is gitignored at the repo root.
12. **Logs are structured, and a request is traceable across tiers** by a correlation
    identifier.
13. **Accessibility is a baseline, not a pass at the end.** Semantic elements, labelled
    inputs, keyboard reachability. Another thing that is nearly free now and miserable later.
14. **The development database is never destroyed.** Not by `docker compose down -v`, not by a
    migration, not to get out of a bad state. It holds the derivations, notes and logs this
    system produces and Hevy does not — and unlike the imported training data, none of that can
    be fetched again. The suites have their own stack and their own database precisely so that
    nothing ever has a reason to reset this one, and its volume is declared `external` so that
    compose cannot remove it even when asked. When a reset seems like the only way forward,
    that is the moment to stop and ask.

    **There is now one supported way to get a clean start, and it is not a reset** (`ADR-025`):
    Settings has an *Erase everything of mine* action that removes one user's own rows and
    nothing else. It exists only where `Development:AllowErase` is set, which is
    `docker-compose.app.yml` and the test stack; a published deployment never maps the route.
    Reach for it instead of `psql` — that is the whole reason it was built. It does not weaken
    this standard: the database, the shared catalogue and everyone else's data are untouched,
    and the affordance stops being adequate the moment `M6` starts storing judgements Hevy
    cannot return. It was `M5` until a milestone was inserted ahead of it; `ADR-025` is
    append-only and still says `M5`, which is the same milestone under its former number.
19. **Git is the log of how the work went, not a snapshot of where it ended.** Work is
    committed as each coherent unit finishes — a step of a milestone, a harness fix, a document
    corrected — not batched at the end. Local commits happen without asking; **pushing is a
    separate decision and is always asked for.** Stage by path and read `git status` before
    committing, never `git add -A` from the root. This is cheap per commit and unrecoverable
    later: a decision superseded three steps after it was made is only explainable if the
    commits between them exist, and no amount of documentation reconstructs a history that was
    never recorded.

### Domain

15. **No training judgement in code without a decision record.** Any rep range, set count,
    progression step, volume threshold or readiness call carries the `TD-###` id of the record
    that decided it, in a comment at the line. The records and the knowledge notes behind them
    live in `/protocol-training`; consult it before writing the judgement, and add to it when
    the corpus has no answer. Without the citation a recalled number and a researched one are
    indistinguishable in the source — and only one of them can be defended later.

### Decisions

16. **An architectural decision is a record, not a commit message.** Which library, which
    layout, which protocol, which shape a schema takes — anything where a different answer
    would have produced different code — gets an `ADR-###` in `docs/decisions/`, with the
    options considered and the rejected one written down. Records are append-only: a parameter
    changing is a `Revisions` bullet, a different option winning is a new record superseding the
    old. Standard 15 is its sibling and the two never merge — `ADR` is how the system is built,
    `TD` is what the system asserts about training, and only the second is subject to a research
    gate.

    **A record's factual claims about this repo are verified against the repo before it is
    committed.** Not the decision — the claims it leans on: what a column holds, what another
    record decided, what the code currently does, what a number works out to. Records are
    append-only, so a false one is not corrected but outlived, and the cost lands on whoever
    builds against it. `ADR-029` asserted that the volume target was already snapshotted onto a
    week; it never was, and `S5.5` stopped mid-step to find out. `/protocol-training` carries the
    arithmetic instance of this rule; this is the general one, and it sits here because an `ADR`
    is not always written inside `/protocol-milestone`.

### Boundaries

17. **Hevy is a mapped boundary, not a model.** Everything this system reasons about is
    modelled here — our entities, our keys, our field names, our vocabulary. Hevy's
    representation is translated inward on import and outward on export, in one place per
    direction, and none of its shape reaches the domain: not its identifiers as keys
    (standard 8), not its field names, not its enums, not its titles (standard 9). Hevy is
    where sets get logged in the gym; information is mapped to it and from it, and that is the
    whole of the relationship. A domain type that cannot be constructed without a Hevy payload
    is the failure this prevents — and the day the logging surface is ours, what gets deleted
    is the mapping, not the model.

### Documentation

18. **A document is corrected by the commit that falsifies it.** Three clauses, one per way a
    document has actually drifted here:
    - **Say what exists, not what does not.** "There is no tooling yet" is the sentence that
      ages in silence — nothing fails when it stops being true, and nobody re-reads it.
    - **A path that moves moves its map in the same commit.** A `## Layout` block claims to
      describe the tree as it is; prose may name a generated file or something planned, a
      layout block may not.
    - **A new decision record means re-reading the standards it touches.** A standard that
      contradicts a fresh `ADR` is corrected there and then. Standard 9 once said an exercise
      is identified by a Hevy identifier, one line under standard 8 saying their identifiers
      are never a key — and the next thing to read it would have been a migration.

    `node scripts/check-docs.mjs` enforces the mechanical half: layout entries resolve, and
    every `standard N`, `ADR-###` and `TD-###` cited under `docs/` exists. It is rung 1 of the
    verification ladder. The half it cannot enforce — a claim that is well-formed, resolvable
    and untrue — is why this is a standard and not only a script.

## Where documentation lives

Five homes, chosen by when the reader needs the content — not by topic:

| Home | Holds | Loaded |
|------|-------|--------|
| This file | Product, cross-cutting standards, the map | Always |
| `backend/CLAUDE.md`, `frontend/CLAUDE.md`, `mcps/CLAUDE.md` | That tier's layout, commands and invariants | When working in that tier |
| `docs/` | The roadmap, the `ADR` records, and one directory per planned milestone | When planning or executing a milestone |
| `.claude/skills/` | Procedure to execute (`/protocol-milestone`, `/protocol-feature`, `/protocol-harness`) and domain reference (`/protocol-training`) | On invocation |
| `.claude/harness/BACKLOG.md` | Pains, wins and ideas about how the work goes | Through `/protocol-harness` |

A convention must never depend on being remembered: anything that would be wrong to write
without knowing it goes in a `CLAUDE.md`, never in a skill, because a skill only helps when
something invokes it.

## Layout

```
backend/                 the .NET 10 API         -> backend/CLAUDE.md
frontend/                the Next.js 16 app      -> frontend/CLAUDE.md
mcps/<name>/             exploration MCP servers -> mcps/CLAUDE.md
docs/ROADMAP.md          the capability spine: what gets built, in what order
docs/decisions/          ADR-###: how the system is built, append-only
docs/milestones/         one directory per planned milestone (plan.md + progress.md)
scripts/check-docs.mjs   fails when a document drifts from the tree (standard 18)
docker-compose.app.yml   the runnable application stack (compose project protocol-project)
docker-compose.test.yml  the throwaway stack the suites run against (project protocol-test)
docker-compose.yml       builds the MCP images only (compose project protocol-mcps)
.mcp.json                registers every MCP server for this project
.claude/                 skills, agents and the harness backlog
```

The three compose files are kept apart on purpose, and their compose projects differ so the
containers stay in separate groups. `docker-compose.app.yml` runs the stack that holds the
development data; `docker-compose.test.yml` runs a throwaway copy of it that owns nothing worth
keeping; the root `docker-compose.yml` runs nothing at all and only builds images.

## Running the stack

```
docker compose -f docker-compose.app.yml up -d --build     # postgres + api + web
docker compose -f docker-compose.app.yml ps                # every service should read healthy
docker compose -f docker-compose.app.yml down              # stops the stack; the data survives
docker compose -f docker-compose.app.yml logs -f api       # follow one service
```

**Never pass `-v` to `down` here.** It is one character away from the safe command and nothing
warns before it runs. The `pgdata` volume is declared `external` precisely so that compose
cannot remove it either way — the rule tells a reader what not to type, the declaration makes
the tool incapable of it. On the test stack below, `down -v` is safe and expected.

A fresh clone creates the volume once, because an external volume is never created implicitly:

```
docker volume create protocol-project_pgdata
```

Without it `up` fails and says so, which is the intended direction: a stack that silently comes
up on an empty database is indistinguishable from one that lost its data.

The frontend is at `http://localhost:3000`, the API at `http://localhost:8080`. The browser
only ever calls the frontend's origin, which proxies to the API; server-side code reaches the
API at `http://api:8080`.

The database always runs in Docker — the compose service in normal use, a throwaway
Testcontainers instance for the integration suite. Nothing expects a Postgres on the host.

## Running the suites

Both suites run containerized, which is what proves a change actually ships — and they run
against their own stack, never against the one above:

```
docker compose -f docker-compose.test.yml run --rm --build e2e             # browser -> web -> api -> its own postgres
docker compose -f docker-compose.test.yml run --rm --build backend-tests   # unit + integration
docker compose -f docker-compose.test.yml down -v                          # safe here, and the way to reset
```

That stack publishes no host ports, so it runs while the development stack is up; its Postgres
keeps its data in RAM and starts empty every time. The separation is not tidiness: an E2E run
registers accounts and never removes them, and they used to land in the development database.

An image caches the source at build time. After editing a tier, rebuild it —
`up -d --build <service>` — or the container keeps serving the old code.

## Direction

The backend and the frontend have landed as a walking skeleton; the MCP servers predate them.
Between them they are plumbing: nothing yet reads a training history or generates a session,
which is the whole point of the product above.

Skills, agents, and further tooling are added when work demands them, not in anticipation.
That applies to the training knowledge as much as to the harness: `/protocol-training` now has
the shape a fact goes into, and it is deliberately empty -- the corpus grows one question at a
time, and only a question a feature is actually asking. Update these files as those land.

## Harness

The tooling that supports the work -- these files, `.claude/skills/`, `.mcp.json`, the build
and dev loops -- evolves the same way the architecture does: from observed pain, never ahead of
it. `/protocol-harness` is the trigger for that meta work; `.claude/harness/BACKLOG.md` records
the pains, wins, and ideas, and a rule that hardens graduates from there into a `CLAUDE.md`.

`/protocol-feature` is the counterpart for product work: the workflow for building a feature
across the tiers, with the verification ladder that ends at a green containerized stack. It was
written from the walking-skeleton build, not ahead of it, and every step in it earned its place
by costing something.

`/protocol-milestone` sits above it and answers a different question — not *how is this built
well*, but *what gets built, in what order, against which decisions*. It turns a milestone from
`docs/ROADMAP.md` into a plan with steps, tests and an order, and it is where the decisions a
package of features needs get made and recorded before any of them reach the code.
