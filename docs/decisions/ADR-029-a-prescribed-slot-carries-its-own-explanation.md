---
id: ADR-029
title: A prescribed slot is returned with what it trains and why it is there, derived at read time
status: active
binds: [backend, frontend]
decided: 2026-08-24
---

**Context.** Every number the generator produces traces to a record and none of them reaches the
screen. The week endpoint returns a title, sets, a repetition range, a reserve and a rest
interval; the muscle a slot exists to train, its `order_class`, its movement pattern, its
equipment, the volume it credits and the per-muscle shortfall the generator already computes are
all dropped. The candidate endpoint even serialises `equipment` and `orderClass` and the frontend
discards both. Asked where a week came from, the product cannot answer.

`ADR-003` makes a generated week persisted and immutable, which forces the question: is the
explanation part of what gets stored, or computed when the week is read?

**Options.**

### A — Derived at read time from the stored week and the catalogue
- The stored week keeps holding exercise ids and prescription numbers. The read endpoint joins
  the catalogue and computes the per-muscle volume, the shortfalls and the reason a slot exists.
- **Pros:** No migration and no new columns. It matches what already happens — `ShortfallsOf`
  derives shortfalls at read today. A catalogue correction (a muscle attribution fixed under
  `TD-005`) immediately improves every past week's explanation instead of leaving old weeks
  explained by a stale copy.
- **Cons:** The explanation can drift from the reasoning that actually produced the week. If the
  weekly target moves, an old week's volume is recomputed against numbers it was never generated
  under — the failure `ADR-003` already guards against for prescriptions, reappearing one level
  up. Every read pays for the join.

### B — Computed at generation and stored beside the week
- The generator writes the explanation as it decides, and the read returns rows.
- **Pros:** Faithful forever: the week says why it was built, under the rules in force then. Reads
  are cheap.
- **Cons:** A migration, new tables, and a second copy of something derivable — which
  `ADR-003`'s own reasoning admits only for things that cannot be recomputed. It also freezes a
  catalogue error into every week generated before it was found, and `ADR-026` has just finished
  arguing the opposite direction for exactly this class of derived value.

**Recommendation.** A — the drift risk is real but bounded and already understood. What a slot
*trains* is catalogue data, not a decision the week took, so recomputing it is a correction
rather than a rewrite. The one genuinely week-bound quantity is the target the volume is compared
against, and that is already snapshotted onto the week by `ADR-003` alongside goal, days and
duration; the comparison uses the stored value, not today's constant.

**Decision.** A

**Consequences.**

- **The target a week is judged against comes from the week, never from the constant.** Without
  that, this decision would recompute a percentage against a number the week never knew, which is
  exactly `ADR-003`'s reason for existing. `ADR-026`'s test applies unchanged: it can be recomputed
  from data already stored, so it is derived.
- **The response carries codes and data, never display text** (root standard 3). A muscle group,
  an `order_class` and a movement pattern are enum names; every translated string is the
  frontend's (root standard 2).
- **"Why this exercise" is arithmetic and must read as arithmetic.** The generator's own answer is
  "this muscle was furthest from its target and this exercise trains it" — no screen may present a
  slot, a split or a substitution as better for growth, because `TD-003`, `TD-007` and `TD-016`
  each record that it is not.
- **The candidate endpoint stops discarding what it already sends.** Equipment and `order_class`
  are serialised today and dropped in the component; a substitution also changes the repetition
  range and rest with the replacement's own class, and that consequence is invisible before the
  swap.
- **A week now mixes three-set and two-set slots** (`TD-022`), so the explanation has to say which
  of the two a slot is, or the reader takes a ceiling slot for a cut week.

**Revisions.**

- 2026-08-24 — **The consequence above claiming the target is already snapshotted onto the week
  was false, and building `S5.5` found it.** `ADR-003` snapshots `Goal`, `DaysPerWeek` and
  `SessionDurationSeconds`; it has never stored a volume target, and `ShortfallsOf` has been
  comparing against `TrainingPrescription.WeeklyTargetFractionalSets` — today's constant — since
  `M1`. The claim was written without checking the schema.

  **Option A still wins and Option B is still rejected.** What changes is one narrow exception
  inside A, and the line that draws it is `ADR-026`'s test: *could this be recomputed from data
  already stored?* What a slot trains can — it is catalogue data, and it stays derived at read
  time with no column, which is the whole of this record. **The target cannot.** A week holding
  6.0 fractional sets of quadriceps is indistinguishable from one that aimed at 8.0 and ran out
  of minutes, so no amount of joining recovers the number the week was judged against. It is a
  parameter of the decision rather than a property of the plan.

  So `generated_weeks` gains `WeeklyTargetFractionalSets` and `WeeklyCeilingFractionalSets`, an
  additive forward-only migration (root standard 10) extending exactly what `ADR-003` already
  does for goal, days and duration. Everything else this record decided is untouched: no muscle,
  class, pattern, equipment or volume figure is stored.

- 2026-08-24 — **The backfill for the eight existing weeks is `6.0` and `6.0`, and the second
  number is the one worth explaining.** Those weeks were generated under `TD-014`'s target of 6.0,
  **before `TD-022` created a ceiling at all**. Writing `8.0` into them would assert they could
  have bought volume above the target, which no code that produced them was capable of. A ceiling
  equal to the target is the faithful statement that they were built to stop there. It is still an
  assertion about history rather than a recovered fact, which is why it is written here rather
  than left implicit in a migration.
