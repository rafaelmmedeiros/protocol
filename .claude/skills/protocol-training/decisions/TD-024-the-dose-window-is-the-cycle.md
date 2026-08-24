---
id: TD-024
title: The dose window is the cycle — the week the user declared, not the week the calendar has
status: active
knowledge: [references/weekly-set-volume-for-hypertrophy.md, references/per-muscle-training-frequency.md, references/cold-start-first-block.md]
decided: 2026-08-24
---

**Decision.**

`TD-014` and `TD-022` state a target and a ceiling **per week**, and `ADR-027` has left the plan
with no weeks. The window becomes the **cycle**: one pass through the template.

| Quantity | Value | Change |
|---|---|---|
| Guaranteed target, per muscle group, **per cycle** | 6.0 fractional sets | unchanged in value (`TD-014`) |
| Ceiling, per muscle group, **per cycle** | 8.0 fractional sets | unchanged in value (`TD-022`) |
| Floor, per muscle group, **per cycle** | 4.0 fractional sets | unchanged in value (`TD-008`) |
| Measurement window for performed volume | the Monday-anchored week | unchanged (root standard 6) |

**No number moves. Only what they attach to.**

**Why the cycle is the honest unit, and not a compromise.** Under `TD-023` a cycle holds exactly
as many sessions as the frequency the user declared — two sessions at a frequency of two, six at
six, in every row of that table. **So a cycle already is the week the user said they would
train.** What `ADR-027` removed was not the user's week; it was the calendar's claim to be the
same thing.

**What the generator fills against is therefore the cycle**, and it fills it the same way it
filled a week: pass one to the guaranteed target, pass two to the ceiling (`TD-022`).

**Why not the two alternatives.**

- **A rolling seven-day window breaks `ADR-005`.** The generator would have to know how many
  sessions the user will complete in the next seven days — future behaviour — to know how much
  volume to put in the plan. `ADR-005` makes generation a pure function of profile and catalogue
  with no clock and no history, and that is what makes a week assertable whole. A window that
  depends on pace makes the same profile produce different plans, which is the property that
  record exists to protect.
- **A Monday-anchored dose window fails for the same reason, one step later.** Under a queue,
  which sessions land in which calendar week is unknowable at generation. The generator would be
  filling against a partition it cannot see.

**Why this and not what the literature would suggest.** The literature does not reach this case at
all, and saying so is the point. `weekly-set-volume-for-hypertrophy` states outright that
**volume as prescribed is not volume as performed** — every trial equated what was assigned and
supervised, and none models a user who does not complete it. The week in that literature is the
reporting unit of the dose-response curve, not a demonstrated biological window: the same note
records that there is **no evidence-derived correct weekly number, only a curve**. So attaching
the target to a cycle rather than to seven days is not contradicted by anything measured — and it
is not endorsed either, because every trial delivered its dose in seven days.

**What it costs, and the first two are mirror images.**

- **A stretched cycle under-doses, and the system will not hide it.** Five sessions taken over
  eleven days delivers 6.0 per cycle and roughly 3.8 per calendar week — under `TD-008`'s floor of
  4.0. The floor is a per-cycle floor and is still met; the *realised weekly rate* is not, and
  that gap is reported rather than corrected. Correcting it would mean prescribing more because
  the user is slow, which is adding volume to someone already showing they have less capacity for
  it than they declared.
- **A compressed cycle over-doses, and `TD-022`'s ceiling no longer bounds a calendar week.**
  That record measured "8.0, exceeded nowhere" per plan. A user on a two-session template training
  six days a week consumes three cycles in a calendar week and reaches 18 — well into the region
  above ~12 where `weekly-set-volume-for-hypertrophy` records that the meta-analyses stop
  agreeing. **This is the sharper of the two costs**, because the under-dosed case is merely
  slower progress and this one leaves the evidenced range entirely. It is reported, not blocked;
  what blocks it is a decision no record has taken, and the honest trigger for taking it is
  observing it happen.
- **The declared frequency now carries weight it did not before.** It picked a template; it is now
  also the denominator of the dose. A user who declares five and trains three is not merely behind
  schedule — they are on a different dose than the one their plan was built for.
- **Two windows exist and a reader must hold both.** The cycle prescribes, the Monday week
  measures. They coincide exactly when the user trains at the pace they declared, which is the
  only case any of this was designed around.

**How it shows up in code.**

- `TrainingPrescription`'s target, ceiling and floor keep their values and their records; what
  changes is the comment at each line and the name of the window they bound.
- The generator fills a cycle. Nothing reads a clock or a history (`ADR-005`, `ADR-006`).
- Performed volume is bucketed into Monday-anchored weeks for every report (root standard 6), and
  the realised weekly rate is reported beside the per-cycle figure so the two are never confused.
- A test asserts the property this record leans on: **cycle length equals the declared frequency**
  for every row of `TD-023`. If a future template breaks that, this record's central claim — that
  a cycle is the declared week — silently stops being true.

**When to revisit.**

- **A user consistently completes cycles much faster or slower than their declared frequency.**
  That is the signal that the declared week and the lived week have separated, and it is
  measurable from the moment `M5` reports the realised rate.
- **The compressed case is observed.** Reaching 18 fractional sets a week by consuming cycles
  quickly is outside the evidenced range, and the decision to bound it should be taken against a
  real case rather than in anticipation.
- **`M6` reads volume across cycles to progress.** It will have to choose which window its
  comparison uses, and choosing the other one would put two dose units in the same arithmetic.
