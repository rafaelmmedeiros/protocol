# Test isolation — P4

Design for backlog item **P4**. Written before code, because the change touches the compose
topology, the documented commands and the verification ladder in `/protocol-feature`.

## Problem

Tests write into the development database, and the command that resets a stack is one character
away from the command that stops it.

## Evidence

- After a day of walking-skeleton work, 18 of the 19 rows in `AspNetUsers` were accounts left
  behind by Playwright runs. Every E2E run registers new accounts (`uniqueEmail()` in
  `frontend/e2e/auth.spec.ts`) and never removes them.
- `docker compose down -v` destroyed the development database once during that build. It is
  documented on the same line as the safe `down`, and nothing warns before it runs.

## Already settled

Recorded in P4 and shipped: the development database is never destroyed (root `CLAUDE.md`,
standard 14), `-v` was removed from every documented command, and the 18 accounts were deleted.
What remains is the mechanism — a rule that only a reader obeys is not isolation.

## What is actually at stake

The scope is narrower than "the test suites":

- **The backend integration suite is already isolated by construction.**
  `backend/Protocol.Api.Tests.Integration/ApiFactory.cs` starts its own Postgres through
  Testcontainers and hosts the API in-process against it. It never sees the compose database,
  and it dies with the test process. Nothing to fix here.
- **Only the E2E suite touches the development database.** In `docker-compose.app.yml` the
  `e2e` service depends on `web`, which reaches `api`, which is pointed at the `postgres`
  service — the development database, the one holding the `pgdata` volume.

And there are two independent failure modes, which need two different fixes:

| | Failure | Cause |
|---|---|---|
| **Accumulation** | Test data lands in the development database | The E2E suite runs against the development stack |
| **Destruction** | The whole database disappears | The dev stack owns a volume compose is allowed to remove |

Isolating the tests fixes the first and does nothing for the second: `down -v` on the dev stack
is still one keystroke away. Both halves are in scope here.

## Candidates for the accumulation half

### A. An ephemeral test stack under its own compose project — **recommended**

A standalone `docker-compose.test.yml` with `name: protocol-test`: its own `postgres` (data on
tmpfs, nothing persisted), its own `api` and `web` built from the same contexts, and the test
runners. No host ports published — the E2E container already reaches the app at
`http://web:3000` over the compose network — so the test stack and the development stack can be
up at the same time without a port conflict.

- **Isolation:** total, and by construction. The test stack has no route to `pgdata`; it does
  not declare it.
- **`down -v` becomes safe there**, which is where a reset is legitimately wanted.
- **Cost:** roughly sixty lines that mirror the app file, and a Postgres boot plus migrations
  on every run.
- **Trap to record in the file:** this is a standalone file rather than an override layered
  with a second `-f`, because compose *appends* `ports` when merging instead of replacing them.
  An override that tried to unpublish 5432 would end up publishing it twice.

### B. A second api/web pair against a test database inside the same project

Add `postgres-test`, `api-test`, `web-test` to `docker-compose.app.yml` and point the E2E
runner at `web-test`.

- Isolates the data, but keeps everything in one project — so `down -v` on that project still
  removes `pgdata` along with the test volume. The destruction half gets worse, not better:
  more reasons to reset a project that also holds the development database.
- The app file stops being "the runnable application stack" and becomes a stack plus a shadow
  copy of itself. Rejected.

### C. Tests clean up after themselves

An `afterAll` that deletes the accounts it created.

- Weakest, as already noted in the backlog: a run that crashes or is interrupted cleans nothing,
  and that is precisely the run that leaves the most behind. It also has to reach into the
  database or add a delete endpoint that exists only for tests.
- It does not address destruction at all. Rejected as the primary mechanism; the E2E suite
  still gets nothing to clean up under A.

## The destruction half

Declare the development volume external, so compose is *unable* to remove it:

```yaml
volumes:
  pgdata:
    external: true
    name: protocol-project_pgdata
```

The volume with that exact name already exists on this machine, so this is a declaration
change, not a data migration. Compose skips external volumes on `down -v`.

The one behaviour change worth knowing: if the volume is absent, `up` fails with a clear error
instead of silently creating an empty one. That is the desirable direction — a stack that
silently comes up on an empty database looks like data loss and *is* indistinguishable from it.
A fresh clone therefore needs `docker volume create protocol-project_pgdata` once, documented
next to the stack commands.

This answers the question P4 left open: a documented rule is not enough. The rule tells a human
what not to type; `external: true` makes the tool incapable of doing it.

## Affected files

| File | Change |
|------|--------|
| `docker-compose.test.yml` | New. `name: protocol-test`, tmpfs postgres, api, web, `e2e`, no published ports |
| `docker-compose.app.yml` | `pgdata` becomes external; the `test` profile services move out |
| `CLAUDE.md` | "Running the stack": the test-stack commands, the one-off volume create, why the two projects are separate; standard 14 gains its mechanism |
| `backend/CLAUDE.md`, `frontend/CLAUDE.md` | Containerized test commands point at the new file |
| `.claude/skills/protocol-feature/SKILL.md` | Ladder rungs 8 and 9 (lines 83–84) change command |

## Verification

The claim is "a full E2E run leaves the development database untouched", so the proof is a
before/after count, not a green suite:

1. Dev stack up; record `select count(*) from "AspNetUsers"` on the development database.
2. Test stack up; run the E2E suite there — green.
3. Re-count on the development database — **unchanged**. This is the actual proof.
4. `docker compose -f docker-compose.test.yml down -v` — the test volume is gone, the dev one
   is still listed by `docker volume ls`.
5. Deliberately run `docker compose -f docker-compose.app.yml down -v` on the dev stack once —
   the volume survives and the row count is intact. The whole point is that this command is
   now harmless, so it has to be run on purpose at least once.
6. Backend suites green in their container.
7. Both stacks up simultaneously, no port conflict.

## Open questions for the engineer

1. **Where does `backend-tests` live?** It needs no stack at all — Testcontainers gives it its
   own database — so it could stay in the app file or move to the test file simply so that
   "tests run from `docker-compose.test.yml`" has no exception. Leaning: move it, for the rule
   without an exception.
2. **Image tags for the test stack.** If it builds `protocol-project/api:latest` it silently
   replaces the image the dev stack runs. Leaning: give it `protocol-test/*` tags.
3. **Any published ports on the test stack?** None is the clean answer, but a failing E2E run
   is easier to debug when the app is reachable from a browser. Leaning: none, and add them
   only when a debugging session actually asks for it.

## Outcome

Shipped as designed, with every open question resolved the way the leanings pointed:
`backend-tests` moved to the test file, the test images carry `protocol-test/*` tags, and the
test stack publishes no host ports.

The verification ran in full:

| Step | Result |
|------|--------|
| Baseline on the development database | 1 account — the real one |
| E2E on the test stack | 4 passed |
| **Development database re-counted** | **still 1** — the 3 accounts the suite registered went to the test database |
| Both stacks up at once | no port conflict; the test stack publishes nothing |
| Backend suites in the test stack | 3 unit, 5 integration, green |
| `down -v` on the test stack | everything gone, as intended |
| `down -v` on the development stack, on purpose | `protocol-project_pgdata` survived; stack back up, count still 1 |
| Missing external volume, probed on a throwaway file | `external volume "..." not found`, and no volume created |

Two things the run settled that the design had only assumed:

- **tmpfs on `/var/lib/postgresql` works** for the Postgres 18 image — the ownership question
  the mount raises never materialised.
- **The `-v` half is genuinely closed.** The command was run against the development stack on
  purpose, which is the only way to know it is harmless rather than merely believed to be.
