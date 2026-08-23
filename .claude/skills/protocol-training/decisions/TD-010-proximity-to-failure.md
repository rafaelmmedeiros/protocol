---
id: TD-010
title: Sets are prescribed at 2-3 repetitions in reserve, never to failure
status: superseded-by TD-018
knowledge: [references/proximity-to-failure-and-hypertrophy.md, references/cold-start-first-block.md, references/repetition-range-and-load-for-hypertrophy.md]
decided: 2026-08-23
---

**Decision.**

| `order_class` | Repetitions in reserve |
|---|---|
| `compound_primary` | **3** |
| `compound_secondary` | **2** |
| `isolation` | **2** |

**No set in `M1` is prescribed to momentary failure, and no set is prescribed at 0 RIR.**

**Why this and not what the literature would suggest.** Two halves, graded differently, and the
record keeps them apart because only one of them is evidence.

**"Not to failure" is evidence.** Refalo et al. (2023), 15 studies, found a *trivial* advantage
for failure over non-failure — ES 0.19, 95% CI 0.00 to 0.37 — and concluded there is no evidence
momentary failure is superior. Grgic et al. (2021) found the same, with a modest advantage
(ES 0.15) confined to a trained subgroup. The best-controlled trial is a within-subject
contralateral-limb design (Refalo 2024, 18 trained adults, 8 weeks) and it is a null: quadriceps
thickness rose similarly with failure and with 1-2 RIR, while failure produced higher perceived
fatigue and RPE. ACSM 2026's summary is "close to failure but not to failure." Failure costs
measurably — a neuromuscular decrement still present at 24 h — and buys nothing detectable.

**The 3/2/2 gradient is convention.** There is a real dose-response toward failure: Robinson et
al. (2024) meta-regressed 55 hypertrophy studies and found RIR slopes negative with intervals
excluding the null. So prescribing 3 RIR on the primary compound gives up a little growth versus
2. It is chosen anyway because the fatigue and accuracy asymmetries are real:

- Failure fatigue persists 24 h and is largest where load is heaviest.
- RIR misjudgement is most expensive under a loaded bar, and novices misjudge by 4-5 repetitions
  (Steele 2017) against 1-2 for experienced lifters.
- Discomfort is higher on a heavy compound than on an isolation exercise at the same proximity.

**No trial has compared a differentiated-RIR programme against a uniform-RIR one for
hypertrophy.** The gradient passes on mechanism and fails on outcome evidence. It is recorded
here, in a decision, precisely so that it cannot be mistaken for a finding.

**How `TD-001`'s cold start modifies this — the more important of its two effects.** Both the
adherence argument and the RIR-inaccuracy argument in `references/cold-start-first-block.md`
point at the conservative end of the dose-response, which is exactly where Robinson says growth
is slightly lower. That trade is deliberate: a trivial effect size against an unrecoverable
adherence risk, in the first 28 days that predict whether a user stays at all.

**What it costs.**

- **A little growth, knowingly.** Robinson's slope is negative; 2-3 RIR is measurably short of
  failure. The magnitude is unknown — **the numeric slope per RIR could not be obtained** (see
  the provenance caveat in the knowledge note), so this record cannot say how much is given up,
  only that it is real and small.
- **A prescribed RIR may not be the RIR performed.** With novice error at 4-5 repetitions, "2
  RIR" may land anywhere from 0 to 6 in an unobserved user. Nothing in the literature tells us
  what an app-prescribed RIR actually produces in the field, and the system never watches the
  set. This is the largest unmodelled gap in the whole prescription.
- **The gradient may be finer than the user can act on.** The difference between 2 and 3 RIR is
  one repetition, and a user who cannot judge RIR to within 4 cannot execute it. The gradient is
  arguably a distinction the product asserts and the user cannot honour.
- **It couples to `TD-009`.** The rep ceiling of 15 exists because this record is conservative;
  the two move together.

**Pre-empting a reasonable objection.** `TD-005` deliberately omitted a `fatigue_cost` attribute,
and a per-`order_class` RIR and rest table is functionally a three-valued fatigue proxy. The
distinction is that this keys off `order_class`, a column that already exists and is already a
judgement recorded once, rather than inventing a per-exercise number that would be treated as
measured. A future session should read this paragraph before concluding the omission crept back
in — and should apply the same test to anything else it wants to add.

**How it shows up in code.**

- An RIR per `order_class` in `Training/`, citing `TD-010` at the line (root standard 15).
- Stored as an integer on the prescription. There is no "to failure" value in the domain — the
  absence is the enforcement.
- `WeekGeneratorTests` asserts no prescription in any generated week carries RIR below 2.

**When to revisit.**

- **Training history import lands.** Logged repetitions against prescribed ranges are the first
  evidence this system could have about what a prescribed RIR actually produces for its user —
  the gap named above, closed by observation rather than literature.
- **Robinson's slope magnitude becomes available**, or a better-powered proximity meta-analysis
  lands. The current dose-response is one exploratory meta-regression whose authors call their
  own fit modest.
- **The user reports sessions feel too easy.** That is the expected complaint from a
  conservative first block and the signal `TD-001` said to watch for.
- **Escalation lands.** Proximity to failure is a natural progression variable, and a second week
  makes this a starting point rather than a fixed rule.
