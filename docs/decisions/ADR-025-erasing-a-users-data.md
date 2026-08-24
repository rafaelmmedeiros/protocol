---
id: ADR-025
title: A user can erase everything of their own, and the shared catalogue is never touched
status: active
binds: [backend, frontend]
decided: 2026-08-24
---

**Context.** Iterating toward a generator worth trusting means running the whole loop repeatedly —
profile, week, push, sync, compare — and every run leaves state behind. Getting back to a clean
start currently means editing the development database by hand, which is the one thing root
standard 14 exists to stop anyone doing.

**It contradicts standard 7, and that has to be said before anything else.** Training history is
append-only: a correction arrives as a new record, and nothing is mutated or deleted. An erase is
the opposite of that.

What makes it acceptable **today** is that almost nothing here is irrecoverable. A generated week
is deterministic and regenerates from the same profile (`ADR-005`). Imported history comes back
from Hevy on the next sync (`ADR-018`). Equipment and preferences are a few minutes of typing.
**None of that survives `M5`**, which is the milestone that starts storing judgements — a
progression decision, a prescribed load — that Hevy cannot return and no regeneration reproduces.

**Options.**

### A — Erase everything belonging to one user, on request, sparing the shared catalogue
- One authenticated action removes that user's profile, equipment, preferences, declined
  suggestions, generated weeks, imported workouts and snapshots, and their Hevy connection. The
  account survives, so they stay signed in. `exercises` and the Data Protection key ring are
  untouched.
- **Pros:** recovers a clean start without anyone opening `psql`, which is what standard 14 is
  really protecting against. Scoped by user, so it cannot reach another user's data or the seed.
  Losing the Hevy connection is correct rather than incidental — re-entering the key is part of
  exercising the loop from its start.
- **Cons:** it is destructive, and standard 7 says this data is not destroyed. It is also the kind
  of affordance that outlives its justification silently.

### B — Reset only the derived state, keep the imported history
- Wipe weeks and preferences; leave the 757 imported workouts.
- **Pros:** honours standard 7 exactly.
- **Cons:** the import is the slowest thing to redo and also where most real bugs have been — the
  response envelope, the dead folder, the binding. Keeping it means never testing a first sync
  again, which is the path with the worst record so far.

### C — Do not build it; reset by hand when needed
- **Pros:** no destructive path in the product at all.
- **Cons:** "by hand" means the development database, and standard 14 says that is the moment to
  stop and ask rather than the moment to type. A missing affordance does not remove the need; it
  moves it somewhere unguarded.

**Recommendation.** A — with the expiry written into the record rather than trusted to memory.

**Decision.** A

**When this expires.** The moment the system stores a judgement of its own — a chosen load, a
progression step, a note the user wrote — this record stops being adequate and needs superseding.
That is `M5`, not a distant horizon. Whatever replaces it will have to separate *state this system
derived and can derive again* from *state it decided and cannot*.

**Consequences.**

- **It is gated to development**, in the same shape as `Hevy:UseFake`: a configuration switch only
  the local stack sets, so a published deployment does not carry the endpoint at all. A feature
  justified by "we are still iterating" must be absent where that is untrue.
- **It is deliberate, never a side effect.** An explicit action, confirmed, and never something a
  restart, a redeploy or a migration performs on its own.
- **It never touches `exercises` or the key ring.** The catalogue is a global seed shared with
  every other user, and the key ring is what makes every *other* user's stored key readable
  (`ADR-014`). Both are outside "everything of mine", and the code says so at the line.
- **A run of it is worth a log line with its counts**, because afterwards "the data was erased" and
  "the import never ran" look identical.
