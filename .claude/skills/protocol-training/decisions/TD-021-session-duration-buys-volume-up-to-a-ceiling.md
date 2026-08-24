---
id: TD-021
title: Session duration buys volume above the weekly target, up to a ceiling of 8.0 fractional sets
status: active
knowledge: [references/weekly-set-volume-for-hypertrophy.md, references/volume-progression-across-a-block.md, references/cold-start-first-block.md, references/session-time-cost-of-a-set.md, references/cutting-training-volume-under-a-time-constraint.md]
decided: 2026-08-24
---

**Decision.**

The weekly volume figure stops being a single number and becomes a band.

| Quantity | Value | Change |
|---|---|---|
| Weekly **guaranteed target**, every modelled muscle group | **6.0** fractional sets | unchanged — `TD-014` |
| Weekly **ceiling**, when the time is there | **8.0** fractional sets | new |
| Hard floor per muscle group | 4.0 fractional sets | unchanged — `TD-008` |
| Per-session cap per muscle group | 11.0 fractional sets | unchanged — `TD-008` |
| Sets per slot | 3 | unchanged — `TD-008` |

**The fill runs in two phases, and the distinction between them is the whole record.**

1. **Reach 6.0 for every muscle.** Exactly what happens today, `TD-013`'s cut ladder included:
   rest is cut, then sets, in that order, to fit the guaranteed target into the minutes available.
2. **Spend what minutes remain, up to 8.0.** Same draw — neediest muscle first — and it stops at
   whichever arrives first: the ceiling, or a slot that will not fit.

**`TD-013`'s ladder belongs to phase 1 and never to phase 2.** Cutting rest to buy volume *above*
the guaranteed target would trade a researched quantity for a convention, in that direction, which
is backwards. A session that cannot fit more at prescribed rest is finished, not a candidate for
compression.

**What this replaces.** `session_duration_seconds` was a rejection test and nothing else: a slot
that did not fit was refused, and the fill stopped on the volume target long before the clock
mattered. Measured across the supported grid, the generator produces **20 slots and 139 minutes of
work per week in every configuration from 2x40 to 6x90** — identical output for wildly different
availability. At 5x60 that leaves **145 minutes unused against 139 used**; at 6x90, 382 unused. A
profile field that changes nothing between 50 and 120 minutes is a question the product asks and
then ignores.

**Why this and not what the literature would suggest.** The literature is unusually clear about
the direction and silent about the number, and the two halves have to be kept apart.

*The direction is settled.* `weekly-set-volume-for-hypertrophy` establishes a square-root
dose-response **with no plateau** — more sets grow more muscle, with diminishing returns and no
observed point at which the curve turns over. Nothing in that supports leaving a lifter's
available hour on the floor.

*The risk region is far above where this lands.* `volume-progression-across-a-block` is the note
that would argue against this, and it does not: both trials where adding sets failed to help
started **above 20 weekly sets**, and one of them did worse than maintaining. The note's own
bearing says adding volume is "defensible here, indefensible at 22". A band of 6.0 to 8.0 sits
under a third of that.

*The ceiling comes from the first-block posture, not from a dose-response.* `TD-001` binds the
first block to the lower half of the effective range, and `cold-start-first-block` says "near the
bottom". 8.0 is the top of that half — **which is precisely the number `TD-008` chose and
defended on concavity, and which `TD-014` gave up for reachability rather than for dose.** This
record does not reopen `TD-014`: 6.0 remains what every configuration reaches, including the 3x40
case that motivated it. It restores 8.0 only where the minutes exist to pay for it, which is the
one thing `TD-014`'s argument never spoke to.

*Above the band, the evidence stops agreeing.* `weekly-set-volume-for-hypertrophy` records that
past ~12 sets the meta-analyses diverge. The ceiling is set well below that on purpose, and a
future record raising it is making a different bet than this one.

*And the obvious counter-argument runs backwards.* The intuition that longer sessions cost
adherence is contradicted by the one cohort that measured it:
`cutting-training-volume-under-a-time-constraint` reports longer sessions associated with
**better** adherence among users training frequently. So spending declared time does not trade
growth against retention — as far as anything measured says, it trades nothing.

**What it costs.**

- **The dose now varies with availability rather than with need.** Two users identical in every
  other respect receive 6.0 and 8.0 because one of them typed 40 and the other 60. That is the
  honest shape of this decision, and it is a real inequality: the time-poor user is not
  under-trained relative to any threshold, but they are further down a curve that has no plateau.
- **8.0 is a convention wearing a citation.** No trial compared 6 to 8 in this population. It is
  `TD-008`'s number, defended by the same concavity argument, and its provenance is a corpus
  reading rather than a result. `TD-014` already priced the 6-to-8 difference at a fraction of a
  percent of muscle thickness over a block — which cuts both ways and is stated here so nobody
  later mistakes the band's edges for measured quantities.
- **Sessions get longer, and the time model over-predicts.** `TD-012` is calibrated on sets taken
  to failure while `TD-018` prescribes 2 RIR, so real sessions run shorter than estimated. The
  band will therefore leave time unspent even after this change — less of it, and still some.
- **A second constant now governs the same quantity.** Every future volume decision has to say
  which edge of the band it moves. `M6`'s volume progression will land exactly here, and the
  ceiling is the first thing it collides with.
- **It does not fix `TD-016`'s false sentence, because records are append-only.** `TD-016` states
  that a preference may not override "whether the slot exists — slot count is `TD-012`'s minutes
  arithmetic". That was never true: slot count is the volume arithmetic, bounded above by the
  clock. `TD-016`'s actual decision — a preference filters and reorders the draw pool and never
  touches the volume arithmetic — is unaffected and still stands. The correction lives here and in
  the index row, which is the only surface a reader lands on first.

**How it shows up in code.**

- `TrainingPrescription` gains `WeeklyCeilingFractionalSets = 8.0m` beside the existing
  `WeeklyTargetFractionalSets`, each citing its record at the line (root standard 15).
- `WeekGenerator.FillSession` runs the two phases above. Phase 2 uses the same `NextExercise`
  draw against ceiling-scaled per-session shares, so per-muscle frequency keeps landing where
  `TD-003` puts it rather than dumping the extra volume into the first session.
- `CutLevel` is chosen against phase 1 only. A week that reaches 6.0 without cutting and then
  fills to 7.4 reports `CutLevel.None`, because nothing was cut.
- The generator stays a pure function of profile, catalogue, equipment and preferences
  (`ADR-005`, `ADR-006`); nothing here reads a clock or a history.
- `WeekGeneratorTests` asserts the band directly: that 40 and 90 minutes differ **at five days a
  week**, not only at three — the blind spot that let this ship. And that 3x40 still lands at
  6.0, so `TD-014`'s protected case is pinned by a test rather than by argument.

**When to revisit.**

- **`M6` starts progressing volume across a block.** It moves one edge of this band or adds a
  third number, and it should supersede this record rather than reinterpret it.
- **Sessions come in systematically short.** `TD-012`'s 60-second transition constant is the
  model's weakest number and the first thing to raise; the band is what makes that error visible,
  because unused minutes now become slots instead of disappearing.
- **A trial adds sets from a baseline near 6.** That is the missing result, and it would move
  `volume-progression-across-a-block` off `contested` in one direction or the other.
- **A user stops completing the longer sessions.** The adherence evidence points the other way,
  but it is a cohort association and this is the decision it would falsify.
