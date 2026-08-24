---
id: TD-022
title: A slot bought above the guaranteed target carries two sets, so the ceiling can be landed on
status: active
knowledge: [references/weekly-set-volume-for-hypertrophy.md, references/volume-progression-across-a-block.md, references/cold-start-first-block.md, references/session-time-cost-of-a-set.md]
decided: 2026-08-24
supersedes: TD-021
---

**Decision.**

Everything `TD-021` decided stands — the band, its two edges, the two-phase fill, and the rule
that `TD-013`'s ladder buys the guaranteed target and never the ceiling. One thing changes:

| Quantity | Value | Change from `TD-021` |
|---|---|---|
| Weekly guaranteed target | 6.0 fractional sets | unchanged |
| Weekly ceiling | 8.0 fractional sets | unchanged |
| Sets in a slot drawn in phase 1 | **3** | unchanged (`TD-008`) |
| Sets in a slot drawn in phase 2 | **2** | was 3 |

**Why.** `TD-021` was written and then measured, and the measurement falsified it. A phase-2 slot
of three sets credits 3.0 to its primary muscle, so a muscle sitting at the guaranteed 6.0 lands
at **9.0** — the band from 6.0 to 8.0 is 2.0 wide and **narrower than one slot**. There is no
arrangement in which a muscle finishes inside it. Generated across the supported grid, the result
was per-muscle volumes of **6.0 to 10.5**, with the ceiling exceeded in fourteen of fifteen
modelled muscle groups at 5x60 and fifteen at 5x90.

At two sets the arithmetic closes: **6.0 + 2.0 = 8.0**, exactly the ceiling, by construction
rather than by luck.

**The set size was necessary and not sufficient, and the rest of this record is what building it
taught.** Two sets alone still landed muscles at 10.5, because `TD-021` had written the ceiling as
a *target to draw against* rather than as a *bound on what a muscle ends the week holding* —
nothing ever checked it. Bounding the drawn muscle alone still landed at 10.5, because the
indirect half of `TD-006` credits every other slot's secondary muscles and that is where the
overflow was arriving. Bounding **every muscle a slot credits** still landed at 9.0, because the
top-up ran per session: an unbounded phase-1 draw in a later session lands on top of volume the
ceiling has already bought, and phase 1 must stay unbounded or the guaranteed target could be
refused. Only the fourth arrangement holds — **the two passes are week-wide**, every session
takes its guaranteed volume first, and only then does anything optional get bought against a
bound checked over every credited muscle.

Measured across the supported grid, 2 to 6 days by 25 to 120 minutes: **worst case 8.0, exceeded
nowhere.** The pre-`TD-021` generator's worst case over the same grid was 7.5, which is the
number any future regression should be read against.

**The record this protects is `TD-001`.** It binds the first block to the lower half of the
effective 4-12 range and `cold-start-first-block` says "near the bottom". 8.0 is the top of that
half. Landing at 9.0, let alone 10.5, is outside the posture `TD-001` holds for a lifter this
system has observed nothing about — and `TD-021` would have shipped that under a citation that
appeared to authorise it. Raising the ceiling to 9.0 instead was the other candidate and was
rejected for the same reason: it fixes the arithmetic by moving the number that `TD-001`
constrains, which is the wrong end to move.

**Why this and not what the literature would suggest.** The literature does not distinguish a
two-set slot from a three-set slot at all — `weekly-set-volume-for-hypertrophy` constrains weekly
volume per muscle and nothing below it, which is what `TD-008` already recorded when it called
three sets per slot "pure convention". So moving to two here costs no evidential support, because
there was none to spend: the constrained quantity is the 8.0, and it is now hit rather than
overshot.

**What it costs, and the first one is the uncomfortable one.**

- **Two sets means the opposite of what it means one record away.** `TD-013` uses two sets as its
  third rung — what the ladder falls back to when time will not fit the guaranteed target. Here
  the same number is what a lifter gets *because they had time to spare*. A future reader finding
  a two-set slot cannot tell from the number which of those happened, and the two constants are
  therefore kept separate in code even though they hold the same value today, precisely so they
  can diverge without a migration of meaning.
- **It does not eliminate overshoot, only the guaranteed kind.** The draw has always taken a whole
  slot to close a partial deficit: a muscle carried to 7.5 by phase 1 still lands at 9.5 after one
  two-set slot. What changes is that a muscle at exactly the guaranteed target — the common case —
  now lands exactly on the ceiling instead of 1.0 past it. **Phase 1's own overshoot is untouched
  and predates all of this**: "target 6.0" has always meant "at least 6.0", and no record has ever
  said so out loud. This one does.
- **A session now mixes three-set and two-set slots**, which is visible to the user and has no
  explanation on the screen. `M5`'s explainability capabilities are where that gets answered; until
  then it is a number a reader can notice and not account for.
- **Two decisions downstream had to move**, and both are recorded under `ADR-012`'s area rather
  than here: inferring how far down the ladder a stored week was generated can no longer read
  "some slot has fewer than three sets" as evidence of a set cut, and a substitution must preserve
  the set count of the slot it replaces instead of re-deriving it from the week's cut level.

**How it shows up in code.**

- `TrainingPrescription.CeilingSetsPerSlot = 2`, beside `ReducedSetsPerSlot = 2` and deliberately
  not merged with it (root standard 15 — each cites its own record).
- `WeekGenerator.Build` runs **two week-wide passes**, not two phases inside each session. Pass 1
  fills every session to the guaranteed target with `SetsPerSlot` — or `ReducedSetsPerSlot` when
  `TD-013`'s ladder has descended that far — and pass 2 then tops each session up.
- Pass 2 passes the ceiling into the draw as a bound, and `WouldExceed` rejects a candidate that
  would carry **any** muscle it credits past it, secondary roles included. Pass 1 passes no
  ceiling, which is what keeps the guaranteed target unrefusable.
- A slot carries the set count it was drawn with, through to ordering. This is why `PickedSlot`
  exists at all: before the band, every slot in a week had the same set count and the sort could
  apply one.
- `WeekGeneratorTests` pins the ceiling as an assertion over every modelled muscle group, at every
  supported frequency and duration — the shape of test that would have caught `TD-021` before it
  was written down.

**When to revisit.**

- **`M6` progresses volume across a block.** It moves an edge of the band, and it will have to say
  what a progressed slot's set count is — this record is the precedent that set count and volume
  target are separable.
- **The band widens past one slot.** If a future ceiling sits 3.0 or more above the guaranteed
  target, three-set slots land inside it again and this record's reason evaporates.
- **`TD-013`'s reduced-set rung changes value.** The two constants are equal today by coincidence
  and the coincidence is load-bearing for nobody; if one moves, check that the other was not
  assumed to follow.
