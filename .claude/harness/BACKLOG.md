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
| P2 | `CLAUDE.md` said "there is no build, lint, or test tooling yet" while `backend/` and `frontend/` had full suites. A statement of absence goes stale silently — nothing fails when it stops being true. | done | Rewritten to scope the claim to the MCP servers. The wider lesson (state what exists, not what does not) is not yet a rule; a second case would earn it. |

## Wins

| ID | What worked | Keep because |
|----|-------------|--------------|
| W1 | The verification ladder run in order — compile, unit, integration on a real Postgres, typecheck, frontend unit, stack up, smoke, E2E in Docker, backend suites in Docker, `git status`. | Each rung is cheaper than the one above it, and the top four rungs caught four failures the local run could not see at all. It is the most reusable artifact the skeleton produced. |
| W2 | Following the framework's own agent instructions before writing code. `frontend/AGENTS.md` pointed at `node_modules/next/dist/docs/`; `cookies.md` there is what produced the proxy architecture. | Without it the design would have been browser-to-API with CORS — plausible, and wrong for a cookie session. Framework docs shipped in-repo beat recalled knowledge. |
| W3 | Docker traps commented at the line that would otherwise look arbitrary (Postgres 18's moved volume mount, IPv4-only healthchecks, Testcontainers publishing on the host, `aspnet` shipping without `curl`). | The comment sits where someone would edit and break it, and there is no second document to keep in sync. |
| W4 | Deciding conventions with the engineer up front, then building without further interruption. | Four decisions (auth mechanism, layout, test depth, Docker orchestration) would each have forced a rewrite if discovered mid-build. |
| W5 | Checking whether a generated `frontend/CLAUDE.md` would compete with the root one by reading Next's own `generate-agent-files.js` and then deleting the file and running `next dev` to see if it came back. | It did not: the generator skips `CLAUDE.md` while `AGENTS.md` holds the managed block. Reading the generator alone would have suggested the file was unavoidable — running the experiment is what settled it. |

## Ideas

| ID | Idea | Status | Note |
|----|------|--------|------|
| I1 | Add a prune mode that reviews standing rules and drops the ones that stopped earning their keep. | parked | Deliberately deferred: `CLAUDE.md` is still small enough to read whole. Revisit when it accumulates procedure rather than facts. |
| I2 | An MCP server for Postgres, so the database can be inspected through named read-only tools instead of `psql` in a shell. | open | Raised while looking at the walking skeleton's data. Not yet a pain: `docker compose exec postgres psql` already answers everything, and the skeleton was built without ever needing the database directly. The value when it lands is read-only by construction (the `hevy`/`hevy-write` split), structured output, and named tools rather than recalled `psql` meta-commands — not access itself. Scope it to analysis and debugging, not to tests: a test that reads the database couples to the schema, and neither candidate database suits it anyway (Testcontainers dies with the test process, and the compose one is a dev environment). Trigger it the first time a debugging session actually stumbles over the database. |
