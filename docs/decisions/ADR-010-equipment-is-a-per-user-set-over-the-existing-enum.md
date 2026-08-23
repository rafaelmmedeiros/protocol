---
id: ADR-010
title: Equipment availability is a per-user set over the enum the catalogue already uses
status: superseded-by ADR-013
binds: [backend, frontend]
decided: 2026-08-23
---

**Context.** `TD-004` programmes `M1` against one assumed gym — barbell, dumbbells, cables, a
pull-up bar, no selectorised machines — and says plainly that this is an assumption to be
superseded, not a model. `M2` replaces it with what the user actually has.

The catalogue already carries a single-valued `equipment` column on every row (`TD-005`), and
`S1.6` populated it on all 36 exercises specifically so that this milestone would be a filter
over an existing column rather than a migration plus a retag. That was the plan; this record
decides whether to keep it.

**Options.**

### A — A per-user set of `Equipment` values
- The user marks which of `barbell`, `dumbbell`, `cable`, `machine`, `smith_machine`,
  `bodyweight`, `bodyweight_loadable`, `band`, `kettlebell`, `other` they have. Selection
  filters the catalogue to rows whose `equipment` is in the set.
- **Pros:** No catalogue change and no migration on `exercises`. Ten checkboxes is a setup
  screen someone finishes. The filter is one `WHERE ... IN`, which keeps the generator's
  selection logic untouched.
- **Cons:** Coarse. "I have dumbbells" does not say *which* dumbbells, so it cannot express a
  rack that stops at 30 kg — and it cannot distinguish a cable station from a lat pulldown,
  which `TD-004` treats as separate things.

### B — Per-exercise availability
- The user marks each catalogue row as available or not.
- **Pros:** Exact. Expresses any gym precisely, including odd ones.
- **Cons:** 36 decisions today and hundreds later, asked before the user has seen a single
  session. It also puts the burden in the wrong place: the user knows their gym has cables,
  not which of our rows that enables.

### C — A described gym: equipment types plus loadable ranges
- A set of equipment plus, per type, what loads it offers.
- **Pros:** The only option that can eventually answer "can this user actually make 47.5 kg",
  which is what a load prescription will need.
- **Cons:** `M1` prescribes no load at all, so every part of this beyond the set is a field
  nothing reads. It is the right model for the milestone that prescribes weight, and
  speculative before it.

**Recommendation.** A — it is the option `S1.6` already prepared the ground for, and the only
one whose cost is proportional to what `M2` actually does.

**Decision.** A

**Notes.** Two limits are accepted knowingly. Equipment does not distinguish a cable station
from a lat pulldown, so a user with neither still gets `vertical_pull` offered through the
pull-up bar and a user with only one of them is over-served — the honest fix is finer enum
values, and it can be a `Revisions` bullet here rather than a new model. And loadable ranges are
out: they belong to the milestone that prescribes load, and `C` is where to return when it
arrives.

The default for a new user is the set `TD-004` assumed, so the behaviour of a user who never
opens the screen is exactly `M1`'s — the assumption becomes a default rather than disappearing.

**Revisions.**
- _(none)_
