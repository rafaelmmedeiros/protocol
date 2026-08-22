# Harness Backlog — Evolution Mode

The living registry for evolving this repo's harness: `CLAUDE.md`, the `.claude/` skills and
settings, `.mcp.json`, and the build / dev loops. Managed through the `/protocol-harness` skill.

Scope is the whole repo, not one tier: today the MCP servers under `mcps/`, and the backend and
frontend still to come. A pain from any of them goes in the same tables here.

**Rules of this backlog:**

- **Pains require evidence.** Every pain cites a real observed case from work in this repo —
  never "would be nice to have". Without a case, it is an idea.
- **Wins are tracked too.** Knowing what already works prevents regressing it when the harness
  is refactored.
- **Proportional effort.** Small fixes resolve inline and just flip the row to `done`.
  Substantial ones get `.claude/harness/<Name>/DESIGN.md`.
- **Settled rules graduate.** A rule that hardens moves to `CLAUDE.md`; a discrete reusable
  fact goes to memory. This file is deliberation and tracking, not the home of settled rules.

Status values: `open` · `in-progress` · `done` · `parked`.

## Pains

| ID | Evidence (real observed case) | Status | Fix |
|----|-------------------------------|--------|-----|
| P1 | Building the walking skeleton (login across .NET 10, Next 16, Postgres, Docker) there was no workflow to follow. Every step was improvised on the spot: probing the toolchain, settling four conventions `CLAUDE.md` did not answer, reading Next's embedded docs, and inventing a ten-rung verification ladder. Nothing recorded it afterwards, so the next feature would have improvised the same things again. | done | `/protocol-feature` skill written from the observed steps; `CLAUDE.md` gained the application layout, the stack/test commands and the tier invariants. |
| P4 | The E2E suite runs against the compose stack, which is the development database. After a day of work 18 of the 19 rows in `AspNetUsers` were test accounts left behind by Playwright runs. Separately, `docker compose down -v` destroyed that same database once during the walking-skeleton build, one character away from the safe command and documented on the same line as it. | done | Decided: two databases, one for tests and one for development, so nothing ever has a reason to reset the development one. The development database is never destroyed — now standard 14 — and the `-v` flag was removed from the documented commands. The 18 test accounts were deleted. The isolation itself is substantial and gets a `DESIGN.md` before code: the candidates are an ephemeral stack under its own compose project, a second api/web pair pointed at a test database, or tests cleaning up after themselves (weakest — a run that crashes cleans nothing). Also open within it: whether a documented rule is enough, or the volume should be declared external so compose is unable to remove it. Shipped: `docker-compose.test.yml`, a throwaway stack under its own compose project (tmpfs Postgres, no published ports, both suites), and the development `pgdata` declared `external` so compose is unable to remove it. Proof was a count, not a green suite: a full E2E run left the development database at 1 account while its 3 test accounts landed in the test database, and a deliberate `down -v` on the development stack left the volume intact. `.claude/harness/TestIsolation/DESIGN.md`. |
| P3 | `CLAUDE.md` reached 216 lines and the engineer noticed it growing. Most of it was tier detail — .NET commands, Next invariants, MCP build loops — carried into every session regardless of what was being worked on, while the standards that actually bind everything were buried among them. | done | Split into a short root file (product, cross-cutting standards, map) plus `backend/`, `frontend/` and `mcps/CLAUDE.md`. Nested `CLAUDE.md` was chosen over a skill per tier because a skill only loads when invoked, and a convention that depends on being remembered is a convention that gets missed. |
| P2 | `CLAUDE.md` said "there is no build, lint, or test tooling yet" while `backend/` and `frontend/` had full suites. A statement of absence goes stale silently — nothing fails when it stops being true. | done | Rewritten to scope the claim to the MCP servers. The wider lesson (state what exists, not what does not) is not yet a rule; a second case would earn it. |

## Wins

| ID | What worked | Keep because |
|----|-------------|--------------|
| W1 | The verification ladder run in order — compile, unit, integration on a real Postgres, typecheck, frontend unit, stack up, smoke, E2E in Docker, backend suites in Docker, `git status`. | Each rung is cheaper than the one above it, and the top four rungs caught four failures the local run could not see at all. It is the most reusable artifact the skeleton produced. |
| W2 | Following the framework's own agent instructions before writing code. `frontend/AGENTS.md` pointed at `node_modules/next/dist/docs/`; `cookies.md` there is what produced the proxy architecture. | Without it the design would have been browser-to-API with CORS — plausible, and wrong for a cookie session. Framework docs shipped in-repo beat recalled knowledge. |
| W3 | Docker traps commented at the line that would otherwise look arbitrary (Postgres 18's moved volume mount, IPv4-only healthchecks, Testcontainers publishing on the host, `aspnet` shipping without `curl`). | The comment sits where someone would edit and break it, and there is no second document to keep in sync. |
| W4 | Deciding conventions with the engineer up front, then building without further interruption. | Four decisions (auth mechanism, layout, test depth, Docker orchestration) would each have forced a rewrite if discovered mid-build. |
| W6 | Proving the test isolation with a row count on the development database rather than with a green suite, and then running the forbidden `docker compose down -v` against the development stack on purpose. | A passing suite says the tests work, not that they stayed out of the development data — those are different claims and only the count tests the second. And a safety mechanism nobody has ever triggered is a belief: `-v` had to be run to know it was harmless. |
| W5 | Checking whether a generated `frontend/CLAUDE.md` would compete with the root one by reading Next's own `generate-agent-files.js` and then deleting the file and running `next dev` to see if it came back. | It did not: the generator skips `CLAUDE.md` while `AGENTS.md` holds the managed block. Reading the generator alone would have suggested the file was unavoidable — running the experiment is what settled it. |

## Ideas

| ID | Idea | Status | Note |
|----|------|--------|------|
| I1 | Add a prune mode that reviews standing rules and drops the ones that stopped earning their keep. | parked | Deliberately deferred: `CLAUDE.md` is still small enough to read whole. Revisit when it accumulates procedure rather than facts. |
| I2 | An MCP server for Postgres, so the database can be inspected through named read-only tools instead of `psql` in a shell. | open | Raised while looking at the walking skeleton's data. Not yet a pain: `docker compose exec postgres psql` already answers everything, and the skeleton was built without ever needing the database directly. The value when it lands is read-only by construction (the `hevy`/`hevy-write` split), structured output, and named tools rather than recalled `psql` meta-commands — not access itself. Scope it to analysis and debugging, not to tests: a test that reads the database couples to the schema, and neither candidate database suits it anyway (Testcontainers dies with the test process, and the compose one is a dev environment). Trigger it the first time a debugging session actually stumbles over the database. |
