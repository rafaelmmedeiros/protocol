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

## 1. Frame

State which tiers the change touches and what "done" looks like, in one or two sentences,
before touching a file. Read `CLAUDE.md` for the invariants of the tiers involved.

Name the verification that will prove it — the specific test or command, not "it works". If no
existing test would fail when the feature is absent, that test is part of the work.

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
| 8 | End to end, in Docker | `docker compose -f docker-compose.app.yml --profile test run --rm e2e` |
| 9 | Backend suites, in Docker | `docker compose -f docker-compose.app.yml --profile test run --rm backend-tests` |
| 10 | Nothing unwanted is staged | `git status --short --untracked-files=all` |

Skip the rungs that cannot be affected by the change; never skip a rung that can.

Rung 8 is the authoritative end-to-end run. The same suite works on the host via
`npm run test:e2e`, which is faster to iterate against but needs `npx playwright install
chromium` once — the Playwright image already has the browser.

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

Ask what a future session would have to rediscover. A durable fact about a tier goes to
`CLAUDE.md`; a pain about how the work itself went goes to `/protocol-harness`. If neither
applies, say so and stop — silence is a valid outcome.
