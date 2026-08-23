---
id: TD-011
title: Rest is prescribed per slot, from 180 down to 90 seconds, with a hard floor at 90
status: active
knowledge: [references/inter-set-rest-and-hypertrophy.md, references/weekly-set-volume-for-hypertrophy.md]
decided: 2026-08-23
---

**Decision.**

Rest is a property of the slot, not of the session and not of the user (`ADR-007`).

| `order_class` | Rest |
|---|---|
| `compound_primary` | **180 seconds** |
| `compound_secondary` | **150 seconds** |
| `isolation` | **90 seconds** |

**The floor is 90 seconds.** No slot in `M1` is prescribed less, including under time pressure.
Rest is stored in seconds (root standard 4).

**Why this and not what the literature would suggest.** The literature is unusually unhelpful
here and the record follows it carefully in two directions.

**What is supported: a per-slot field, and a floor.** The 2024 Bayesian review — 9 studies, 184
participants — found every hypertrophy effect size for short versus long rest crossing zero
(arm 0.13, thigh 0.17, whole body -0.08). Rest is close to free for growth in the tested range.
What is *not* free is repetitions: Schoenfeld/Haun (2023) ran a Smith-machine squat and a leg
extension side by side at 1, 2 and 3 minutes and found the large difference between **1 minute
and 2-3 minutes**, with trivial differences between 2 and 3 — in **both** exercises. Senna
(2011) and the triceps pull-down work agree. Short rest costs repetitions, and repetitions are
the stimulus.

**What is not supported: the "compounds need more rest" convention, as usually justified.** In
the best-controlled comparison the 1-minute penalty appeared in the isolation exercise as much
as the compound, and a light-load single-joint condition was the *most* rest-sensitive of all.
So the descending gradient here is **not** justified by compounds needing more recovery. It is
justified by discomfort and load magnitude — perceived discomfort was higher for the squat than
the leg extension in the same trial — and by session time. Recorded this way so the usual and
wrong justification does not get attached to it later.

**On the milestone plan's "first slot over two minutes, last slot one minute" criterion.** The
plan asked for this *if the research supports it*. It half does, and the halves are worth
separating:

- **The schema requirement passes.** Per-slot rest is well justified as a field, a session can
  express a descending gradient, and this record does so — 180 down to 90.
- **The 60-second value does not pass.** The one consistent finding in the acute literature is
  that the 1-minute condition loses repetitions, in isolation exercises as much as compounds,
  and the pooled hypertrophy comparison tilts weakly against the short interval. Prescribing 60
  seconds anywhere would be adopting the one value the evidence argues against.

So the gradient is built and the floor is 90 rather than 60. This is the conditional in the
plan's acceptance criterion resolving to "supported in shape, not in value."

**What it costs.**

- **Longer sessions.** Rest is the single largest determinant of session duration, and a 90-second
  floor with a 180-second primary makes sessions longer than a 60-second prescription would.
  `S1.5` inherits this directly, and it is the main pressure on the time budget.
- **The 180-versus-150-versus-90 gradient is convention.** Nothing distinguishes 2 from 3
  minutes for repetitions or for growth. The specific numbers buy comfort and tidy arithmetic,
  not results.
- **Rest by position in the session is untested.** Nothing found tests within-session rest
  gradients, and the reasoning cuts both ways: later slots carry lighter loads, but fatigue is
  *higher* later. This record follows load and discomfort; it could be wrong.
- **Laterality is ignored.** A unilateral set provides contralateral rest by construction, so a
  90-second prescription between unilateral sets is not the same interval as between bilateral
  ones. No hypertrophy source addresses it, and `M1` does not model it despite storing
  `laterality` (`TD-005`).

**How it shows up in code.**

- A rest value per `order_class` in `Training/`, citing `TD-011` at the line (root standard 15),
  and a named floor constant.
- Stored as `rest_seconds` on the prescription (root standard 4 — the unit is in the field name).
  The frontend converts for display; the domain never holds minutes.
- The `S1.5` time-budget cut may reduce rest toward the floor but never below it. **Rest is the
  first thing cut and a slot is the last**, because the evidence between 90 and 180 seconds is
  flat while cutting a slot moves a muscle down a curve whose steepest region is exactly where
  `TD-008` sits.
- `WeekGeneratorTests` asserts no prescription in any generated week rests below 90 seconds, and
  that rest differs between slots within a single session.

**When to revisit.**

- **Session length proves intolerable.** That is the live tension, and it is `S1.5`'s to
  discover. If the budget cannot close with a 90-second floor, the floor is what gets argued
  about — and it should be argued about here, not weakened silently in the generator.
- **Evidence appears on within-session rest gradients**, which would move this from inference to
  finding either way.
- **Evidence appears above 3 minutes**, which is untested rather than disproven.
- **A goal other than hypertrophy is supported.** Rest matters more where load and strength
  matter, and this table was reasoned for one goal.
