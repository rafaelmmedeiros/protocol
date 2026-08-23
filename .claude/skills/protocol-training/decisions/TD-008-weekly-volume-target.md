---
id: TD-008
title: Eight fractional sets per muscle group per week, uniform across muscles, three sets per slot
status: superseded-by TD-014
knowledge: [references/weekly-set-volume-for-hypertrophy.md, references/muscle-group-specific-volume-requirements.md, references/cold-start-first-block.md]
decided: 2026-08-23
---

**Decision.**

| Quantity | Value |
|---|---|
| Weekly target, **every** modelled muscle group | **8.0 fractional sets** |
| Hard floor per muscle group | **4.0 fractional sets** |
| Per-session cap per muscle group | **11.0 fractional sets** |
| Sets per slot | **3** |
| Differentiation by muscle group | **none** |

Volume is counted in fractional sets throughout (`TD-006`). The target is uniform: `side_delts`
and `quads` carry the same number.

**Why this and not what the literature would suggest.** The literature does not suggest a number
— it supplies a curve. Pelland's best fit is a **square root**: monotonically rising with
continuously diminishing returns and **no plateau**. There is no optimum in it to find, so any
target is a chosen point, and the honest justification is where on the curve we choose to stand.

8.0 is chosen because the curve is concave and the cheap growth is at the bottom. Most of the
available hypertrophy is bought by the first few sets; at Pelland's per-set estimate the step
from 8 to 12 is worth roughly 1% of muscle thickness over a block, while the step from 4 to 8 is
worth substantially more. 8.0 keeps nearly all of the cheap growth, sits below ACSM 2026's ~10
threshold for near-maximal growth, and sits a full tier below Baz-Valle's 12-20 for trained men —
which is the region where the meta-analyses stop agreeing and which `TD-001` forbids citing.

**Where this sits against `TD-001`, stated plainly.** `TD-001` binds the first block to the lower
half of 4-12. 8.0 is the **top** of that half, not its middle, and
`references/cold-start-first-block.md` uses the phrase "near the bottom of the effective range,"
which argues for 6.0. This record takes the top of the permitted band on the concavity argument
and flags the tension rather than hiding it. **6.0 is the standing alternative and this is the
number most likely to be revisited first.** Anyone reopening it should read `TD-001` before this
record, not after.

**The uniform target is the evidence-supported position, which is not the intuitive one.** No
meta-analysis or meta-regression found stratifies its volume dose-response by muscle group;
ACSM 2026 states one threshold "per muscle group" with no differentiation. The per-muscle volume
tables every training app ships are convention with essentially nothing behind them, and the one
direct muscle-specific signal in the literature favours *higher* volume for triceps — the
opposite of the usual "small muscles need less" story, and probably an indirect-volume confound.
`TD-005`'s omission discipline applies: a per-muscle table would be inventing numbers.

**What it costs.**

- **A trained user is under-dosed, knowingly.** Against Baz-Valle's 12-20 for trained men, 8.0 is
  roughly one tier light. This is `TD-001`'s cost being paid in a concrete number, and it is
  small: fractions of a percent of muscle thickness over a block, recoverable the moment
  escalation exists.
- **The uniform target lands unevenly in practice, and that is arithmetic rather than
  physiology.** Under `TD-006`, `front_delts`, `triceps` and `biceps` will often reach 8.0 mostly
  through 0.5-weighted secondary roles, while `side_delts`, `rear_delts` and `calves` can only
  reach it through direct slots. **If a muscle cannot reach the 4.0 floor, that is a catalogue
  coverage failure to surface — not a reason to move the target.** `TD-004`'s `knee_flexion` hole
  is the existing precedent for naming such a gap rather than papering over it.
- **Three sets per slot is pure convention.** The evidence constrains weekly volume, not sets per
  slot. 3 is chosen so that slot count and weekly target divide cleanly; nothing prefers it over
  2 or 4.
- **The per-session cap will never bind at these volumes.** It is included so that a future
  volume increase cannot silently cross Remmert's ceiling, and it should be asserted as a test
  rather than enforced as a rule.

**How it shows up in code.**

- Named constants in `Training/`, each citing `TD-008` at the line (root standard 15): weekly
  target, floor, per-session cap, sets per slot.
- The generator computes each muscle group's fractional weekly total (`TD-006`) and compares it
  against the target. A muscle below the 4.0 floor after selection is a surfaced failure, not a
  silent shortfall.
- `WeekGeneratorTests` asserts the per-session cap is never exceeded — a guard against a future
  volume change, not a live constraint.
- No per-muscle volume table exists anywhere. If one appears, it was invented.

**When to revisit.**

- **First, and most likely: the 8.0-versus-6.0 choice**, on any signal that the first block is
  too much — a session abandoned, a week not completed. Adherence is the cost `TD-001` weighted
  above growth.
- **Escalation lands.** A second generated week makes the starting number a starting point rather
  than the whole prescription, which changes the argument entirely.
- **A volume meta-regression stratified by muscle group appears.** That would convert the uniform
  target from evidence-supported to superseded.
- **The catalogue cannot deliver 4.0 to some muscle group.** That reopens `TD-004` and `TD-005`,
  not this record.
