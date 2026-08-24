---
id: ADR-024
title: Imported load is the total lifted, and a unilateral set is the exception that needs its own answer
status: active
binds: [backend]
decided: 2026-08-24
---

**Context.** `weight_kg` arrives on every imported set and nothing said what it counts. For a
barbell it is unambiguous. For a dumbbell exercise it is not: 30 kg could mean 30 in each hand or
30 across both, and the two readings differ by a factor of two in every load the system will ever
prescribe (`M5`).

Hevy's OpenAPI document does not say. Asking is cheap, and the engineer answered plainly: the log
is a **total** — a 30 kg barbell curl and a 30 kg dumbbell curl are the same load, 15 in each hand,
the same volume.

**That answer is confirmed by their own history rather than taken on trust**, which is why it is a
decision and not a note. Across their logged sets:

| Movement | Barbell | Dumbbell |
|---|---|---|
| Spider curl | 18.8 kg | **18.9 kg** |
| Bicep curl | 22.3 kg | 18.6 kg |
| Preacher curl | 18.1 kg | 19.3 kg |

Under a per-hand reading the dumbbell figures would sit at roughly **half** the barbell ones — 11
against 22. They sit alongside them, across 233 logged dumbbell curl sets.

**Options.**

### A — Imported `weight_kg` is the total lifted, whatever the implement
- Volume-load arithmetic uses it as it arrives, and a prescribed load is expressed the same way.
- **Pros:** matches the observed data and the stated intent. It makes barbell and dumbbell figures
  directly comparable — which matters even though
  `load-increment-granularity-and-progression` forbids carrying a *load* across variants, because
  comparability is what lets that rule be stated rather than assumed.
- **Cons:** it is one account's convention. Another user, or Hevy itself, may mean per-hand, and
  nothing in the payload distinguishes them.

### B — Per implement, inferred from the exercise's equipment
- Treat dumbbell rows as per-hand and double them.
- **Pros:** matches what several logging apps do.
- **Cons:** contradicted by the only real data this project has, and it would silently double every
  dumbbell load the system reasons about.

### C — Store it uninterpreted and refuse to compare across implements
- Never put a barbell and a dumbbell number into the same arithmetic.
- **Pros:** cannot be wrong.
- **Cons:** gives up volume-load entirely, and volume-load is the only load-aware quantity `M5` can
  compute before it prescribes anything.

**Recommendation.** A — it is the reading the data supports, and B would apply a factor-of-two error
silently to every dumbbell movement.

**Decision.** A

**The exception, named rather than assumed.** A **unilateral** set breaks the reading, and the same
history shows it: `Single Preacher Curl (Dumbbell)` averages 19.3 kg against `Preacher Curl
(Barbell)` at 18.1 — one arm apparently outlifting two on a bar, which is not what happened. What
happened is that a single-arm entry records the implement, because "total" and "per hand" are the
same number there.

The catalogue already carries `Laterality`, so this is expressible rather than a gap. **It is not
decided here**, because the right answer depends on what `M5` does with load, and deciding it now
would be modelling a preference before its consumer exists (the counter-intuitive rule
`/protocol-milestone` states). What this record fixes is the bilateral case; the unilateral one is
flagged and left to the record that consumes it.

**Consequences.**

- **The unit is in the field name and the meaning is not** (root standard 4). `weight_kg` says
  kilograms and cannot say *total*, so the meaning lives here and is cited wherever volume-load is
  computed.
- **This is one account's convention, and publishing will test it.** The horizon in
  `docs/ROADMAP.md` reaches other Hevy users; the first one who logs per-hand turns this into a
  per-user setting or a detection problem. Recorded now so that day meets a record to argue with
  rather than a surprise.
