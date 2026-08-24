---
id: TD-023
title: A frequency offers a set of templates with one default, and a cycle replaces the week as what a template repeats over
status: active
knowledge: [references/split-templates-by-frequency.md, references/per-muscle-training-frequency.md]
decided: 2026-08-24
supersedes: TD-003
---

**Decision.**

`TD-003` mapped each supported frequency to exactly one template and named "the user asks to
choose" as the first of its own revisit triggers. That has happened. The mapping becomes a set,
with a default that is `TD-003`'s answer unchanged.

| Sessions | Templates offered | Per-muscle, per cycle |
|---|---|---|
| 2 | **Full body x2** — only | 2x |
| 3 | **Full body x3** *(default)* · Upper / Lower / Full | 3x · 2x |
| 4 | **Upper / Lower x2** *(default)* · Push / Pull / Legs / Full | 2x · 2x |
| 5 | **U / L / U / L / Full** *(default)* · Upper / Lower / Push / Pull / Legs | 3x · 2x |
| 6 | **Push / Pull / Legs x2** *(default)* · Upper / Lower x3 | 2x · 3x |

Every figure above was **measured against `SplitTemplate.ScopeOf`**, not reasoned out: each
template's cycle was walked and each muscle group counted. All nine offered templates give every
modelled muscle group at least 2x. `Upper / Lower` at two sessions was considered and **fails** —
it delivers 1x to all sixteen groups — which is why the two-session row still offers no choice and
why `TD-003` mapped it to full body in the first place.

**Two constraints, and one of them has changed shape.**

1. **A template repeats over a fixed number of sessions.** `TD-003` said "repeats weekly", and
   `ADR-027` has since removed the weekday assignment: a plan is an ordered queue. So the unit a
   template repeats over is a **cycle**, and per-muscle frequency is per cycle rather than per
   week. **How a cycle maps onto days is not decided here** — that is the dose window, and it is
   `M5`'s own separate research step. Nothing in this record assumes an answer to it.
2. **Every muscle group reaches at least twice per cycle.** From
   `per-muscle-training-frequency`, which is explicit that this is a **soft** floor: ACSM 2026
   says at least twice weekly, the volume-equated evidence does not require it, and it is adopted
   because every sane template delivers it anyway — not because 1x is shown to fail.

**Why this and not what the literature would suggest.** The literature suggests nothing, and that
remains the finding rather than a gap. `per-muscle-training-frequency` is graded `settled` and
closes it: with weekly volume equated, how it is distributed across sessions does not change
hypertrophy. `split-templates-by-frequency` is graded `thin` and says plainly that **no trial has
compared, say, Upper/Lower x2 against PPL + Upper/Lower at 5 days**, which is precisely the choice
this record now hands to the user.

So offering a choice costs nothing and claims nothing. What it buys is the one thing the corpus
does support: `self-selected-exercise-and-autonomy` reaches device-measured behaviour, and
`TD-016` already took that bet one level down at the exercise. This is the same bet at the
template.

**What it costs.**

- **The surface with no evidence behind it doubles.** Nine templates instead of five, none of
  them compared to any other in any trial. A future session must not read the specificity of this
  table as evidential weight — the note under it stays `thin`, and that is not a temporary state
  awaiting research nobody is running.
- **`TD-003`'s rest distribution is gone, and this record does not replace it.** That record
  refused to stack sessions Mon-Tue-Wed with four days off. `ADR-027` removed the weekday, so
  nothing schedules rest any more and the user's own spacing decides it. Nothing in the corpus
  prices what that costs, because every trial equates volume and reports frequency per week.
- **Two templates at the same frequency differ in per-muscle frequency, and we cannot argue for
  either.** At five sessions the default gives 3x and the alternative 2x; both sit inside the
  2-3x band every defensible convention lands in, and the evidence separating them does not
  exist.
- **The default preserves `TD-003` exactly**, which is deliberate and is also a mild trap: a user
  who never chooses is following a convention that now looks like a decision because it sits in a
  table of alternatives.

**How it shows up in code.**

- `SplitTemplate` exposes the templates admitted per frequency and which is the default, citing
  this record. The existing single mapping becomes the default column.
- A frequency admits only the templates in its row; anything else is rejected with a code (root
  standard 3), never a message.
- Nothing in the UI may present a template as better for growth — `SplitTemplate` already carries
  that warning in a comment, and a chooser is exactly where a recommendation badge would
  reintroduce the claim.
- Where the choice is stored, and what null means, is `ADR-030` and not this record.

**When to revisit.**

- **A trial compares specific templates at equal frequency and volume.** None exists; one would
  move `split-templates-by-frequency` off `thin` and might turn a preference into a default.
- **The dose window lands somewhere that makes per-cycle frequency drift from 2-3x per week.** A
  five-session cycle taken over eleven days is roughly 1.3x per week per muscle, under the soft
  floor this record assumes. That interaction is real and belongs to whichever record decides the
  window.
- **Weekly volume rises above ~12 fractional sets per muscle.** At two sessions that lands on the
  contested per-session ceiling, and the two-session row would need re-examining — `TD-003`
  flagged this and it is inherited unchanged.
