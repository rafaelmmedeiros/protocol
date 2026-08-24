---
id: ADR-026
title: Imported training is remapped when the catalogue widens, because the mapping column is ours and the record is theirs
status: active
binds: [backend]
decided: 2026-08-24
---

**Context.** `M4` widened the catalogue from 36 rows to 63 and the coverage number did not move.
Measured against the development database immediately after the seed:

```
explained 1,394 | unexplained 3,798 | total 5,192 | 126 distinct movements
```

Those are the same 3,798 and the same 126 that motivated the milestone in `docs/ROADMAP.md`. The
catalogue got wider; the history's mapping did not.

The cause is one line. `PerformedExercise.ExerciseId` is resolved **once, at import**
(`HevyHistoryImporter` passes a lookup into `HevyInboundMapper`) and frozen into the row. `ADR-018`
imports incrementally from a cursor, so the 757 workouts already read are never fetched again and
their mapping never gets another chance. Coverage improves only for training logged *after* a
widening — which is the opposite of what a catalogue widening is for, and leaves `M5` progressing a
quarter of a week for a lifter whose history is already here.

**The tension has to be named before the options.** Root standard 7 says an imported record is
never mutated or deleted. Every option below except D writes to a row that was imported.

**Options.**

### A — Fill the mapping column wherever it is null and the template now resolves
- `ExerciseId` is set from `ExternalTemplateId` for rows that have one and lack the other. No
  weight, repetition, date, set kind or effort value is touched, and nothing is deleted.
- **Pros:** the smallest write that fixes it. It restores a fact that was always true — this
  movement *is* that exercise — rather than asserting a new one. It needs no network call, so it
  cannot fail differently from the original import, and it is idempotent: a row already mapped is
  not a candidate.
- **Cons:** it writes to an imported record, and standard 7 does not carve out exceptions by
  itself. Left unrecorded it becomes precedent for writes that are not this careful.

### B — Stop storing the mapping and resolve it at read time
- Drop the column; join `performed_exercises` to `exercises` on `ExternalTemplateId` wherever
  volume is counted.
- **Pros:** conceptually right. The mapping is then always current and every future widening is
  retroactive for free, with no write to any imported row at all.
- **Cons:** the largest change — a migration, and every read of performed volume rewritten. It also
  discards something real: the record of *how we mapped at the time*, which is the same kind of fact
  the append-only history exists to preserve. And `M5` is about to touch every load-aware read
  anyway, so doing it now means doing it twice.

### C — Clear the cursor and re-import everything
- Re-read all 757 workouts, producing new versions through the path `ADR-018` already defines.
- **Pros:** uses only machinery that exists, and reconciles rather than mutates — exactly what
  standard 7 describes.
- **Cons:** it creates a second version of every workout to change one derived column, which is
  noise in the substrate every later analysis reads. It also spends a real account's rate budget to
  recompute something already in the database.

### D — Accept it; coverage rises as new training is logged
- **Pros:** writes nothing.
- **Cons:** 3,798 logged exercises never count toward any muscle, ever. `M4` existed to fix that
  number and would have moved the catalogue without moving the measurement.

**Recommendation.** A — with the boundary written down rather than assumed, because it is the thing
that will be reached for again the next time the catalogue grows.

**Decision.** A

**The boundary, stated so it can be cited.** A row in `performed_exercises` holds two different
kinds of thing, and standard 7 protects one of them:

- **Theirs, and immutable:** `ExternalTemplateId`, `ExternalTitle`, `Position`, and everything in
  `performed_sets` — weight, repetitions, set kind, reported effort. These are the observation. A
  correction arrives as a new version through re-import, never as an edit.
- **Ours, and derived:** `ExerciseId`. It is not a fact about the workout; it is this system's
  answer to "which of our exercises is that?", computed from `ExternalTemplateId` against a
  catalogue that changes. Recomputing an answer is not mutating an observation.

The test for any future write to an imported row is whether it could be recomputed from data
already stored without asking Hevy anything. `ExerciseId` can. A weight cannot.

**Consequences.**

- **It runs in the seeder's slot, not in a migration**, and for the same reason the requirements
  backfill already there is not a migration: this is not a one-off. Every future catalogue widening
  creates exactly this gap, and a migration would fix today's while leaving the next one to be
  discovered the same way — by measuring and being surprised. A hosted service registered after
  `ExerciseCatalogueSeeder` runs after the new rows exist and is idempotent, so it costs one
  indexed query per startup once there is nothing to do.
- **It never overwrites a mapping that exists.** Only `ExerciseId IS NULL` rows are candidates, so a
  row mapped under an earlier catalogue keeps the exercise it was mapped to. A movement whose
  meaning genuinely changed is a supersession in the catalogue, not a silent remap here.
- **A movement we still do not model stays null**, which is correct and is what `S4.5` reports. The
  count falling to zero would mean the catalogue models everything, and it does not.
- **The run is logged with its count** (root standard 12). A remap that silently did nothing and a
  remap that fixed 3,798 rows are indistinguishable from every screen afterwards.
- **`M5` should reconsider option B.** It rewrites the load-aware reads regardless, which is where
  the cost of B mostly sits. This record is the cheap fix in front of the right one, and says so.
