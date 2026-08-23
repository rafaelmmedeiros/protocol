---
id: TD-012
title: A slot costs 7.5 minutes; sessions run 25 to 120 minutes
status: active
knowledge: [references/session-time-cost-of-a-set.md, references/warm-up-cost-before-resistance-training.md]
decided: 2026-08-23
---

**Decision.**

**The time model.** `session = warm_up + sum(set durations) + sum(rest) + sum(transitions)`, with
these constants:

| Term | Value | Basis |
|---|---|---|
| Repetition duration | **3 s** | Back-calculated from two trials that measured session duration (3.2 s, 4.3 s). Free for growth across 0.5-8 s |
| Set duration | reps x 3 s, at the `TD-009` midpoint | 24 s primary / 30 s secondary / 38 s isolation |
| Rest | `TD-011` | 180 / 150 / 90 s |
| Transition between slots | **60 s** | **No source. An engineering constant.** |
| Warm-up | **180 s**, once, before the first `compound_primary` only | Constructed from the shape of the warm-up evidence, not read from a source |

**The conversion**, which falls out cleanly across the supported range:

- At prescribed rest: `slots = floor((minutes - 15) / 7.5)`
- At the 90 s rest floor: `slots = floor(minutes / 7.5)`

| Minutes | Slots at prescribed rest | Slots at the rest floor |
|---|---|---|
| 30 | 2 | 4 |
| 40 | 3 | 5 |
| 45 | 4 | 6 |
| 60 | 6 | 8 |
| 75 | 8 | 10 |
| 90 | 10 | 12 |

**No general warm-up is generated, and ramping sets are prescribed only on the first
`compound_primary` slot.** Warm-up sets are not prescription rows and carry no volume credit.

**Supported duration.** `session_duration_seconds` accepts **1500 to 7200** (25 to 120 minutes).
Outside that, `DurationOutOfRange`.

**Why this and not what the literature would suggest.** There is no time-motion literature for
resistance training, so nothing here is a training finding and the note behind it is graded
`thin` however well it fits. What made an additive model defensible rather than invented is that
two crossover trials published a fully specified protocol alongside a measured session duration,
and the model reproduces one to within 1% and the other to within 8% on a single free parameter.

The result that matters is not any constant but a proportion: **rest was 79% and 74% of measured
session duration.** Every other term combined is a fifth of the clock. This is why `TD-013` cuts
rest first, and it is a stronger reason than the one `TD-011` gave — rest is not merely cheap to
cut, it is nearly the whole budget. Cutting to the floor is worth **two extra slots at every
duration between 30 and 90 minutes.**

**The bounds.** The minimum is set by structure, not by dose: Iversen's minimum viable session is
three compound slots — one leg press pattern, one upper push, one upper pull — which costs 31.9
min at prescribed rest and 20.9 min at the floor. 25 minutes is the smallest round figure that
delivers that structure with slack after rest is already cut. 20 min fits it with zero slack; 30
min fits it without touching rest. Both are defensible and 25 was chosen as the point that still
serves a user the minimum-effective-dose literature says is worth serving.

The maximum is **a product bound, not an evidence bound**, in the sense `TD-002` uses when it
rejects seven days a week. At the hardest supported frequency the whole week is ~15 slots, so a
session cannot productively use more than ~90 minutes at `TD-014`'s target — the volume target
binds before the clock does. 120 gives headroom for a larger catalogue or a future volume rise
without admitting values that are almost certainly typos. **Nothing here says a long session is
harmful**; the adherence evidence points the other way.

**What it costs.**

- **The 60-second transition constant is invented, and it is the model's weakest number.** It is
  also the term that grows fastest with slot count: ±30 s moves a 10-slot session by ±5 minutes.
  If generated sessions run long in practice, this is the first constant to raise.
- **The model is calibrated on sets taken to failure**, while `TD-010` prescribes 2-3 RIR. The
  slowest repetitions of a set are the last ones, so 3 s/rep over-predicts set duration for our
  prescriptions by an unknown amount. Over-predicting is the safe direction: sessions come in
  short rather than long.
- **Omitting general warm-up rests on performance evidence that never measured injury.** The
  trials found no effect on repetitions; none looked at risk. This is the boundary most likely to
  be crossed by someone reading the nulls as a licence to omit warm-up entirely.
- **Ramping doubles as load-finding, and `M1` prescribes no load at all.** A user who does not
  know their working weight needs ramping sets to discover it, whatever the physiology says. One
  ramp on the first compound may be too few for that purpose, and no trial isolates the function.
- **The 25-minute minimum refuses a user with 20 minutes** who could have had a defensible
  three-exercise session at floor rest.

**How it shows up in code.**

- The constants live in `Training/`, each citing `TD-012` at the line (root standard 15). The
  transition and warm-up constants carry a comment saying they are engineering estimates, not
  evidence — a reader must not mistake them for the researched ones beside them.
- `session_duration_seconds` validates against 1500-7200 and returns `DurationOutOfRange` (root
  standard 3 — a code, never display text).
- Duration is stored in seconds and rendered in minutes at the edge (root standard 4).
- `WeekGeneratorTests` asserts the conversion at 30, 40, 45, 60, 75 and 90 minutes against the
  table above.

**When to revisit.**

- **One logged Hevy workout with timestamps replaces the transition constant with a measured
  number.** This is the highest-value calibration available and the data source already exists —
  it is the strongest argument in this milestone for prioritising history import.
- **Sessions run long or short in practice.** The model over-predicts by design; systematic error
  in either direction is a signal.
- **`TD-011`'s rest values change.** Rest is three quarters of this model; nothing else moves it
  comparably.
- **Supersets or other density techniques are adopted** (`TD-013` step 4). That changes the
  session equation structurally, not just its constants.
