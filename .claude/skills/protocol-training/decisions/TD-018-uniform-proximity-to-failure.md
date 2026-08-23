---
id: TD-018
title: Proximity to failure is 2 repetitions in reserve for every exercise, superseding TD-010's gradient
status: active
supersedes: TD-010
knowledge: [references/graded-versus-uniform-proximity-to-failure.md, references/proximity-to-failure-and-hypertrophy.md, references/inferring-proximity-to-failure-from-logged-sets.md, references/progression-trigger-under-constant-effort-execution.md]
decided: 2026-08-23
---

**Decision.** Every prescribed set carries **2 repetitions in reserve**, whatever its
`order_class`. `TD-010`'s 3 / 2 / 2 gradient is withdrawn. No set is prescribed to momentary
failure and none at 0 RIR, both of which `TD-010` decided and this record keeps unchanged.

| `order_class` | `TD-010` | **`TD-018`** |
|---|---|---|
| `compound_primary` | 3 | **2** |
| `compound_secondary` | 2 | **2** |
| `isolation` | 2 | **2** |

**Why the gradient goes.** `TD-010` graded its own gradient as convention rather than evidence,
and gave three reasons for it. Two have since been contradicted and the third does not survive
inspection.

**The accuracy argument was backwards.** `TD-010` says RIR misjudgement is "most expensive under a
loaded bar" and more likely there. Halperin et al. (2022) — 12 studies, **414 participants, 262
effect sizes** — found accuracy *improves* with **heavier loads** and fewer repetitions, with no
moderation by upper versus lower body and none by training experience. Remmert et al. (2023), 58
trained and untrained participants across three exercises, found **no significant main effect of
exercise** at all. The heavy low-repetition primary compound is where RIR judgement is *most*
accurate; the light 10-15 repetition isolation slot sits on the wrong side of Halperin's
approximately 12-repetition boundary. **A gradient justified on accuracy alone would have run the
other way.**

**The fatigue argument does not convert.** Multi-joint sessions produce higher creatine kinase, but
at the muscle actually being trained the result inverts: Soares et al. (2015), contralateral arms
in highly trained men, found elbow-flexor torque fell **26.8% after the single-joint exercise
against 15.1% after the multi-joint**, still 8.4% down at 24 h where the compound had recovered.
More importantly, **no source converts either measurement into a proximity recommendation** — both
are framed as recovery-window findings, which are arguments about frequency and volume.

**Only discomfort survives, and it is a function of proximity rather than of exercise type.**
Refalo et al. (2025) measures perceived discomfort against how close to failure a set is taken,
not against whether the movement is compound.

**And ACSM 2026 is uniform.** Its overview of 137 reviews gives one exercise-agnostic target —
near-failure, or 2-3 repetitions in reserve — and makes no compound-versus-isolation distinction
anywhere in its proximity guidance. A gradient can still be chosen, but it would now be chosen
*against* a position stand rather than in the absence of one.

**Why 2 and not 3.** The band ACSM states is 2-3, so both are defensible and the choice is a
tie-break. Three things favour 2. Robinson's dose-response slope is negative, so 2 gives up less
growth than 3. The reason 3 sat specifically on `compound_primary` was the accuracy asymmetry,
which is the argument that just inverted — removing it removes the case for the higher value where
it was applied. And 2 is already what two of the three slots prescribed, so it is the smaller
change from what the generator emits today.

**What it costs.**

- **A slightly harder first block than `TD-010` produced.** `TD-001`'s cold-start argument has two
  halves and only one of them fell: the RIR-inaccuracy half is contradicted, the **adherence** half
  stands untouched. This record accepts one extra repetition of effort on the primary compound
  against that argument, on the grounds that ACSM states 2-3 for everyone and one repetition is
  inside the band. **If adherence evidence ever arrives, differentiating the first block is a new
  record, not a reinterpretation of this one.**
- **The head-to-head still does not exist.** No trial has randomised a graded-RIR programme against
  a uniform one, and none is expected. This record is **convention with a better argument behind
  it than `TD-010` had — not a finding**, and it must not be cited as one.
- **The comparison the gradient actually asserted was never run.** Nobody has tested RIR accuracy
  on a barbell squat against a machine isolation exercise in the same subjects at the same
  proximity. Halperin's null aggregates heterogeneous protocols at I² = 97.9%, and Remmert used
  machines and cables exclusively. The evidence against the gradient is real and it is not the
  experiment the gradient claimed.
- **The safety half is unmeasured.** No source tests injury rate or technical breakdown by
  proximity to failure. That is the ground on which a gradient could still be defended, and this
  record does not refute it — it notes that nobody has measured it either way.
- **Provenance.** Halperin's per-moderator numbers were read from the abstract; the full text is
  paywalled and the upper-versus-lower comparison was seen only as a statement of
  non-significance. Soares's percentages come from an abstract that could not be fetched directly.
  The knowledge note carries both caveats, and a future record leaning harder on either should
  verify them.

**A number is not a stopping criterion, and the two are being conflated.** This record prescribes
"2 repetitions in reserve", and different lifters convert that into different physical events. One
stops at the onset of technical breakdown — velocity falls, other muscles begin compensating — and
calls the result 2 RIR. Another stops when two more repetitions merely *feel* available. Those are
not the same event, and the corpus covers neither: its notes address momentary failure and
self-reported RIR targets, and **technical failure as a distinct construct is absent from it
entirely.** The prescription is a number because that is what the literature is written in; what
the user does with it is unmodelled, and `references/progression-trigger-under-constant-effort-execution.md`
records what that costs downstream.

**How it shows up in code.**

- One constant, cited `TD-018` at the line (root standard 15). The per-`order_class` table goes
  away; anything still branching on `order_class` for RIR is dead code.
- Still stored as an integer on the prescription, and there is still no "to failure" value in the
  domain — the absence is the enforcement, exactly as `TD-010` decided.
- `WeekGeneratorTests` asserts every prescription in every generated week carries RIR 2. The old
  assertion — never below 2 — is now weaker than the rule and should be tightened rather than left.

**What this does not change.** `TD-009`'s repetition ranges are unaffected: they rest on load and
joint demand, and the isolation slot's RIR does not move, so the coupling `TD-009` states between
its ceiling and a conservative RIR still holds. `TD-011`'s rest intervals are unaffected.
`TD-017`'s conversion is unaffected — how many distinct RIR values the generator emits has no
bearing on the mapping at the boundary.

**When to revisit.**

- **A graded-versus-uniform trial appears.** Unlikely, and it would settle this directly.
- **Injury or technical-breakdown evidence by proximity appears.** That is the one argument that
  could reinstate a gradient, and it would reinstate it for a different reason than `TD-010` gave.
- **Adherence evidence from the product's own use.** A user reporting the primary compound feels
  too hard is the signal `TD-001` said to watch, and it now has one fewer counterweight.
- **Halperin's full text is read.** If the upper-versus-lower moderation is weaker than the
  abstract implies, the strongest leg of this record softens.
