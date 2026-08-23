---
id: TD-006
title: An indirect set counts 0.5 toward a muscle's weekly volume
status: active
knowledge: [references/per-muscle-training-frequency.md, references/exercise-selection-within-a-movement-pattern.md]
decided: 2026-08-23
---

**Decision.**

A muscle group's weekly volume is counted in **fractional sets**: a set of an exercise where the
muscle is `primary` counts **1.0**; a set where it is `secondary` counts **0.5**. The weekly
target from `TD-001` (4-12 fractional sets, lower half for a first block) is expressed in these
units and in no other.

The 0.5 lives in **one named constant** in the generator, citing this record. It is not a column
on the catalogue and not a literal repeated at call sites.

**Why this and not what the literature would suggest.** This is the literature's own convention
rather than a simplification of it: Baz-Valle et al. (2022), writing specifically on how to
equate volume between hypertrophy programmes, count an indirect set as 0.5, and both 2025
meta-regressions this corpus rests on (Pelland, Remmert) use fractional counting. Adopting
anything else would make `TD-001`'s bound incomparable with the evidence that produced it — the
4-12 range is *stated in fractional sets*, so counting directly would silently inflate every
target by however much indirect work the split happens to contain.

There is also a single trial that gives the convention an empirical shape rather than an
arbitrary one. Mannarino et al. (2021) compared a unilateral row against a unilateral biceps
curl, matched sets to failure: elbow flexor thickness rose 11.06% with the curl and 5.16% with
the row — a ratio of 0.47. One trial is not a validation, and the number reached this corpus
through a secondary summary (see the provenance caveat in
`references/exercise-selection-within-a-movement-pattern.md`), so it is corroboration, not the
justification. The justification is comparability with the volume literature.

**What it costs.**

- **0.5 is disputed, and some argue 0.33.** No source found settles it. A generator that used
  0.33 would prescribe more direct work for arms and delts; one that used 1.0 would prescribe
  far less. The choice moves every volume number in the product, which is exactly why it is one
  constant and not a scattered literal.
- **It is a single weight for every muscle and every exercise.** A row and an overhead press
  almost certainly do not load the biceps and triceps to the same relative degree, and nothing
  in this scheme can express that. The alternative — a per-exercise float — was rejected in
  `TD-005` because it would spread an unauditable training judgement across hundreds of
  catalogue rows.
- **It depends entirely on `secondary` being assigned consistently.** `TD-005` names this as the
  soft spot of the schema, and this record inherits it: a mis-tagged secondary muscle is a
  half-set of phantom or missing volume that no diff will show.

**How it shows up in code.**

- One constant in `Training/`, cited as `TD-006` at the line (root standard 15), used by every
  volume computation.
- Volume targets, the fit-to-time-budget cut (`S1.5`) and any assertion in `WeekGeneratorTests`
  about weekly volume are all expressed in fractional sets. A test asserting a direct set count
  would be asserting something the product does not claim.
- The value is a decimal, not a float-typed literal repeated per muscle; summing many 0.5s must
  not accumulate representation error into a target comparison.

**When to revisit.**

- **Evidence arrives for a per-muscle or per-exercise weighting.** That would supersede this
  record rather than amend it, and would move the weight out of the generator into the
  catalogue — a schema change, so `TD-005` moves with it.
- **The 0.33 position gains support.** It is the live alternative.
- **Training history import lands.** Observed per-exercise progression is the first thing this
  system could use to check whether 0.5 describes its own user, which is a better answer than
  any convention.
