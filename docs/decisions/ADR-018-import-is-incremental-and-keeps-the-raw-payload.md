---
id: ADR-018
title: Import is incremental over the workout events feed, appends versions, and keeps the raw payload
status: active
binds: [backend]
decided: 2026-08-23
---

**Context.** Training history is append-only and never mutated or deleted; a correction arrives as
a new record (standard 7). Hevy's history does change upstream — a workout carries `updated_at`,
and users edit sessions after finishing them. Two endpoints are available:
`GET /v1/workouts`, which pages the current state, and `GET /v1/workouts/events?since=`, which
returns `updated` and `deleted` events with a page size capped at ten.

`TD-017` is the other force here. It converts Hevy's RPE into our repetitions in reserve by
discarding information deliberately, and it names the conditions under which that conversion
should be revisited — including one the product's own corpus can answer. A conversion that may
change is a conversion whose inputs have to survive.

**Options.**

### A — Incremental over the events feed, appending a version per event, retaining the raw payload
- A per-user cursor holds the last event timestamp. Each sync pages `events?since=<cursor>`. Every
  `updated` event appends a **new version row** for that workout rather than overwriting one; a
  `deleted` event appends a **tombstone version** rather than removing anything. The raw JSON is
  stored alongside the mapped rows, and reads take the latest version.
- **Pros:** deletions are observable, which the plain workouts endpoint cannot express — there, a
  removed workout simply stops appearing, which is indistinguishable from a paging bug. Cost after
  the first sync is proportional to what changed, not to history size. Retaining the raw payload
  means a changed mapping is a recomputation rather than a re-fetch, which matters because `TD-017`
  expects to be revisited and Hevy is not a guaranteed archive. Standard 7 is satisfied literally.
- **Cons:** a cursor is state that can be wrong, and recovering from a bad one needs a full
  backfill. The ten-item page cap makes the first backfill many calls. Storage grows with edits,
  not just with training.

### B — Full re-fetch of `/v1/workouts` each sync, diffed by `updated_at`
- Page the whole history every time and write what changed.
- **Pros:** no cursor, no state to corrupt, trivially recoverable.
- **Cons:** cost grows without bound as the history does, for a user whose history is the point of
  the product. And a deleted workout is invisible: it leaves no trace and no event, so the system
  would keep counting volume for a session the user removed.

### C — Incremental, but mapped only
- Same feed, same versioning, but the raw payload is discarded once mapped.
- **Pros:** less storage, and no duplicated representation of the same fact.
- **Cons:** a mapping change — which `TD-017` explicitly anticipates, and which `ADR-019`'s match
  rules could also force — becomes a full re-fetch of the entire history against a third-party API
  that may no longer hold it. Discarding the input to a conversion that is known to be provisional
  is the cheapest possible mistake to avoid.

**Recommendation.** A — B's blindness to deletions is disqualifying on its own, and C trades a
small storage saving for the ability to correct a conversion the product has already said it
expects to correct.

**Decision.** A

**Consequences.**

- **The first sync is a backfill** from the feed's default epoch, and it is the expensive one. The
  ten-item page cap is Hevy's, not ours, and the import is written to page rather than to assume.
- **A tombstone is a version, not a deletion.** Nothing removes a row. Analyses read the latest
  version of each workout and skip tombstoned ones, which keeps standard 7 intact while still
  respecting that the user meant to remove the session.
- **`warmup` sets are filtered at read time, not at import.** They are part of the payload and are
  retained; what they are excluded from is the fractional volume arithmetic (`TD-006`). Filtering
  on the way in would be discarding a fact to save an `if`.
- **The mapping runs in one place** on the way in (standard 17), and `TD-017` is the whole of the
  effort conversion.

**Revisions.**

- 2026-08-23 — **What a sync does with a workout it cannot map**, decided while building `S3.4`
  and left open on purpose by `S3.2`. The inbound mapper refuses an unmodelled set type rather
  than defaulting it to a working set, because a silent default would inflate every fractional
  volume figure the system produces. That refusal needed a policy at the sync level, and the three
  candidates were: abort the sync, skip without advancing the cursor, or skip and continue.

  The first two are the same failure wearing different clothes — **one odd workout would block
  every future sync, permanently**, and the user could not fix it because the offending data is in
  a third party's account.

  So the sync **stores the payload first, then maps**, and a mapping failure costs a report rather
  than the data. The snapshot lands with the reason recorded beside it, no mapped rows are written,
  the cursor advances, and the run continues. `Unmapped` is returned as its own count so a number
  that stops being zero is a visible signal that the catalogue or a record needs widening.

  This is retention doing the work it was retained for: because the payload is kept, deciding later
  what an unknown set type means is a **recomputation**, not a re-fetch of a history the vendor may
  no longer hold. The option this record chose is what made the cheap answer available.

- 2026-08-23 — **The cursor is inclusive, so the boundary event is re-delivered by design.** The
  feed answers "at or after", which means the last event of one sync is the first event of the
  next. Idempotence is therefore a requirement of the importer rather than a property of the feed:
  an update whose `updated_at` already has a row is skipped, and a deletion whose latest version is
  already a tombstone is skipped. Without that, every sync would append a duplicate version of
  whatever sat on the boundary.
