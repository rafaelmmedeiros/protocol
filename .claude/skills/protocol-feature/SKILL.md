---
name: protocol-feature
description: "The feature workflow for this repo: frame the work, probe the toolchain, read the framework's own docs, build one tier at a time, and climb the verification ladder until the containerized stack is green. Derived from the walking-skeleton build, not invented ahead of it."
argument-hint: "[<what to build>]"
disable-model-invocation: true
---

The product workflow for `protocol`. Use it when building or changing a feature that touches
`backend/`, `frontend/`, or an MCP server under `mcps/`. For work on the tooling itself, use
`/protocol-harness` instead.

Every step below is here because its absence cost something during the walking-skeleton build
(login across .NET + Next + Postgres + Docker). Nothing is here on principle. If a step ever
feels like ceremony for the change in front of you, skip it and log that in the harness
backlog — a step that stops earning its keep should die.

## Proportional effort

- **A one-line change inside a tier that already exists** → step 4 and the parts of the ladder
  that touch it. Nothing else.
- **A new endpoint, page, or tool** → steps 3 through 6.
- **A new tier, a new dependency, or anything that changes a convention** → all six steps, and
  settle step 2 with the engineer before writing code.

## Milestone mode

When the work is a step of a planned milestone — `docs/milestones/M<N>-<slug>/plan.md`, written
by `/protocol-milestone` — the plan is the contract and this section governs the loop. Steps 1
and 2 below are already answered by it: do not re-frame and do not re-decide. Steps 3 through 7
still apply, once per step of the plan.

**Before the first step.** Read the plan in full. Refuse to start while its `## Open questions`
section is non-empty — an unanswered question there is a decision that would otherwise get made
silently, mid-build. Read `progress.md` if it exists and resume from the first step that is not
`completed`; create it with every step `pending` if it does not.

**One step at a time, in the plan's dependency order.** Never two at once, never ahead of a
dependency. That guarantee is the only thing this mode adds over building freely, so breaking it
empties the mode.

For each step:

1. Re-read its section of the plan — description, technical actions, tests, acceptance criteria.
2. Implement the technical actions, in order, touching nothing outside them. An unrelated
   problem noticed on the way is reported, not fixed.
3. Write the test files its Tests table names. Every acceptance criterion should be observable
   from at least one of them.
4. Run **only this step's tests**. The full ladder is for the end.
5. On failure: read the error, fix the root cause, re-run — at most **three attempts**. Then
   stop and report which step is stuck, the failure, and your hypothesis. Never weaken a test,
   never skip one, never quietly edit a completed step to make this one pass.
6. Write the `progress.md` entry, then **stop and ask before starting the next step** — unless
   the engineer asked for a continuous run up front.

**The progress entry is the point.** It is the only artifact that survives the session, and the
line that matters is the last one:

```markdown
### S<N>.X — <name>
- **Status:** completed
- **Tests:** <counts, or "no tests">
- **Observations:** <what a future session would otherwise rediscover — or "none">
```

Write an observation when something was not in the plan and cost time: a container that kept
serving old code, a suite that hung, a migration that left an artifact behind. Not a summary of
what the step did — that is already in the plan and in the diff.

**When every step is done**, climb the full ladder (step 5 below), tick the plan's Deliverables,
confirm every capability bullet is covered, and set the progress file's status to `completed`.
Then close the loop (step 7). If the plan itself turned out to be wrong, that is not something
to patch mid-build: stop, and revise it through `/protocol-milestone`.

## 1. Frame

State which tiers the change touches and what "done" looks like, in one or two sentences,
before touching a file. Read the root `CLAUDE.md` for the standards that bind everything, and
the `CLAUDE.md` of each tier you are about to touch for its invariants and commands.

Name the verification that will prove it — the specific test or command, not "it works". If no
existing test would fail when the feature is absent, that test is part of the work.

If the change makes a training judgement — a rep range, a set count, a progression step, a
volume threshold, a readiness call — invoke `/protocol-training` here, before any of it gets
written. Follow the decision if one exists; research and record one if not. A number that
reaches the code without a `TD-###` beside it (root standard 15) cannot be told apart later
from one that was recalled.

## 2. Settle the conventions that do not exist yet

Before scaffolding anything new, list the decisions the repo has not already made — where code
lives, which library, which auth mechanism, how deep the tests go, how Docker runs it. Ask the
engineer about the ones where different answers produce materially different work, with a
recommendation. Decide the rest yourself and say what you decided.

This exists because the walking skeleton needed four such decisions and `CLAUDE.md` answered
none of them. Discovering that mid-build would have meant rewriting.

## 3. Probe the toolchain, then read the framework's own docs

**Probe.** Check what is actually installed and what the current versions are — never assume
from memory. The walking-skeleton build found five .NET SDKs on the machine, which is why
`backend/global.json` pins one. Pin what you find.

**Read.** If a framework ships instructions for agents, follow them before writing the first
line. Next.js writes `frontend/AGENTS.md` and embeds its documentation at
`frontend/node_modules/next/dist/docs/` — it states outright that this version differs from
model training data. Reading `cookies.md` there is what produced the current architecture (the
browser talks only to the Next origin, which proxies to the API); the unread version would
have been the direct-call-with-CORS design, and wrong.

Prefer, in order: the framework's embedded docs → the `context7` MCP → the web.

## 4. Build one tier at a time, compiling as you go

Finish and compile a tier before starting the next. `dotnet build`, `npx tsc --noEmit`, or the
equivalent, after each meaningful edit. Three of the walking skeleton's errors — a missing
package, a missing `using`, an obsolete constructor — existed only at compile time and cost
seconds to find this way.

Write the test for a piece next to the piece, while the context is still loaded, not in a
separate pass at the end.

## 5. Climb the verification ladder

Run it in order. A rung failing means fixing before climbing, because each rung is cheaper than
the one above it.

| # | Rung | Command |
|---|------|---------|
| 1 | Backend compiles | `dotnet build` (in `backend/`) |
| 2 | Backend unit | `dotnet test Protocol.Api.Tests.Unit` |
| 3 | Backend integration, real Postgres | `dotnet test Protocol.Api.Tests.Integration` |
| 4 | Frontend types | `npm run typecheck` (in `frontend/`) |
| 5 | Frontend unit | `npm test` |
| 6 | Stack builds and reports healthy | `docker compose -f docker-compose.app.yml up -d --build` |
| 7 | Smoke | `curl -s http://localhost:8080/health` · `curl -so /dev/null -w "%{http_code}" http://localhost:3000/login` |
| 8 | End to end, in Docker | `docker compose -f docker-compose.test.yml run --rm --build e2e` |
| 9 | Backend suites, in Docker | `docker compose -f docker-compose.test.yml run --rm --build backend-tests` |
| 10 | Nothing unwanted is staged | `git status --short --untracked-files=all` |

Skip the rungs that cannot be affected by the change; never skip a rung that can.

Rung 8 is the authoritative end-to-end run. It builds its own stack — a second api and web
against a throwaway Postgres — so it never writes into the development database, and `--build`
is what makes it test the code just changed rather than a cached image. The same suite works on
the host via `npm run test:e2e`, which is faster to iterate against but needs `npx playwright
install chromium` once and points at whatever stack is on `localhost:3000` — the development
one, which is exactly the run that used to leave accounts behind. Iterate there, conclude on
rung 8.

Rungs 6 and 8 use different compose files on purpose. Leave the development stack up while
they run; the test stack publishes no host ports and will not collide with it.

## 6. Containerized green is the only green

Local green does not count as done for anything that ships in an image. Every deployment
problem in the walking-skeleton build was invisible until `docker compose up` ran: Postgres 18
moved its volume mount, a healthcheck against `localhost` failed because the server binds IPv4
only, Testcontainers inside a container publishes on the host, and the `aspnet` runtime image
has no `curl`. None of them were discoverable by reading.

After editing a service's source, rebuild its image — a running container keeps serving the
old code, exactly as `CLAUDE.md` records for the MCP servers.

Each such trap is commented where it lives, at the line that would otherwise look arbitrary.
Keep doing that: the comment is the record, not a document elsewhere.

## 7. Close the loop

Ask what a future session would have to rediscover. A durable fact about one tier goes to that
tier's `CLAUDE.md`; something that binds every tier goes to the root one; a pain about how the
work itself went goes to `/protocol-harness`. If neither applies, say so and stop — silence is
a valid outcome.

Never put a convention in a skill. A skill only helps when something invokes it, so a rule that
lives there is a rule that will be missed.
