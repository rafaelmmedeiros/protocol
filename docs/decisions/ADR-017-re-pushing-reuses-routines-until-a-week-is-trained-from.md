---
id: ADR-017
title: Re-pushing overwrites the routines of an untrained week and creates new ones once it has been trained from
status: active
binds: [backend]
decided: 2026-08-23
---

**Context.** **Hevy has no delete endpoint for a routine or a folder.** `POST` and `PUT` are the
whole surface. Anything this system creates there, it creates permanently.

That collides with how the app is actually used. `ADR-009` allows regenerating a week, and the
engineer predicted the behaviour precisely: users optimise their equipment and their exclusions
and regenerate repeatedly until the week looks acceptable, and only then start training. Every
regeneration of a pushed week is therefore a decision about what happens to routines that cannot
be removed.

There is a second constraint pulling the other way. `routine_id` is the only join between a
prescribed session and a logged workout (`ADR-019`). If a routine's contents are replaced while
its identifier stays the same, two workouts logged against that identifier — one before the
replacement, one after — become indistinguishable by identifier alone.

**Options.**

### A — Overwrite while untrained, create new once trained from
- Re-pushing a week that has **no logged workout matched to it** reuses the existing folder and
  `PUT`s each routine in place. Once **any** workout has matched the week, a regenerated week is
  pushed as new routines and the old ones are left standing.
- **Pros:** during the optimisation phase — which is exactly when regeneration is frequent and no
  training has happened — Hevy stays clean and no orphan accumulates. After training begins, the
  identifier ambiguity cannot arise, because the routines that were trained from are never
  rewritten. The old routines surviving is then correct rather than untidy: they describe what was
  actually trained from.
- **Cons:** two behaviours instead of one, and the switch between them depends on import state, so
  a push has to consult what has been synced. A week trained from and then regenerated does leave
  routines behind permanently.

### B — Always create new routines
- Every push `POST`s a fresh folder and fresh routines.
- **Pros:** one behaviour. The join is never ambiguous.
- **Cons:** unbounded litter in a surface with no delete, produced fastest by the behaviour the
  engineer described. Ten regenerations before the first session leaves nine dead folders the user
  must tidy by hand, and the product caused it.

### C — Always `PUT` in place
- One folder and one set of routines per week, rewritten on every regeneration.
- **Pros:** the cleanest Hevy, one behaviour.
- **Cons:** silently breaks the join for any week that was trained from and then regenerated — the
  comparison would attribute a workout to a prescription that did not exist when it was performed.
  That is the one failure this product cannot tolerate, because it is invisible and it corrupts
  the exact output the system exists to produce.

### D — Refuse to regenerate a week once pushed
- Pushing freezes the week.
- **Pros:** trivially safe.
- **Cons:** takes away the loop the engineer named as the thing that gets a user started, to solve
  a problem that only exists after training has begun.

**Recommendation.** A — the ambiguity in C and the litter in B both have narrow blast radii, and A
takes the cheap half of each: overwrite where nothing can be corrupted, accumulate only where the
accumulation is meaningful.

**Decision.** A

**Consequences worth stating.**

- **A push must know whether the week has been trained from**, so it reads import state. That
  couples push to sync, and the coupling is deliberate rather than incidental.
- **Nothing is ever removed from Hevy by this system.** No code path calls a delete, because none
  exists. If routines need tidying, the user does it in Hevy, and the system tolerates their
  disappearance — a `PUT` against a routine the user deleted fails, and that is handled as a
  push failure rather than as corruption.
- **This does not change `ADR-003` or `ADR-009`.** Our generated week stays immutable and
  append-only on our side regardless of what happens in Hevy; this record governs only the mirror.

**Revisions.**

- 2026-08-23 — **The "create new once trained" half needs no branch, and what replaces it is a
  refusal.** Building `S3.3` made the shape plain. `ADR-009` already makes a regenerated week a
  **new row**, and a new row has no folder — so it takes the create path on its own, and the old
  routines are left standing exactly as this record intends. There is nothing to detect.

  What the detection is actually for is the other case: **the same week pushed again after
  something has been logged against it.** Neither available action is safe there. Replacing its
  routines would leave a logged workout pointing at a prescription that did not exist when it was
  performed, which is the invisible corruption option C was rejected for. Creating fresh routines
  for that same week would overwrite the stored identifiers and orphan the workout that already
  matched, which breaks `ADR-019`'s join in the other direction.

  So a week that has been trained from is **refused**, with `WeekAlreadyTrainedFrom`, and the user
  regenerates instead — which is not a workaround but the ordinary path, since the new week pushes
  freely. Option A is unchanged in substance: overwrite where nothing can be corrupted, accumulate
  only where the accumulation is meaningful. What changed is that the second half is reached by
  regenerating rather than by a branch inside the push.

- 2026-08-24 — **A routine the user deleted in Hevy is recreated, not refused.** The engineer hit
  this immediately: they tidied their own app, and the next push answered "generate a new week and
  send that".

  Refusing was over-cautious, and the reason is specific. This branch is only reached *after* the
  trained-from check above has already passed — so no logged workout points at the identifier
  being replaced, and there is no join to break. Telling someone to throw a week away because they
  deleted a routine costs them the week to protect a link that does not exist.

  `PushedRoutineMissing` survives as a guard rather than as a path: a `NotFound` on the update now
  falls through to a create, so the code is only reachable if creation itself reports one, which
  Hevy does not do.

- 2026-08-24 — **The litter this record rejected option B for still exists, by a different door.**
  Option A reuses the folder when the *same* week is re-pushed, and `ADR-009` makes every
  regeneration a **new week** — which has no folder, so it creates one. Ten regenerations then
  leave ten folders, which is exactly what option B was refused for.

  Observed rather than reasoned: five weeks were generated in fifteen seconds during one debugging
  session, each of which would have created its own folder had the push been working.

  Not decided here. The candidates are carrying the previous week's folder forward when a week is
  regenerated, or one folder per user with routines replaced in place. Both change what a folder
  *means* in Hevy, which is this record's subject, so whichever wins is a new record superseding
  this one rather than a revision of it.
