---
id: ADR-020
title: Equipment derived from training history is proposed for confirmation, adds only, and never removes
status: active
binds: [backend, frontend]
decided: 2026-08-23
---

**Context.** `M2` gives the user a screen to describe their gym, and the engineer's verdict on it
was that it is limited and tedious. The import makes a cheaper source available: a logged workout
names an `exercise_template_id`, our catalogue maps that to an exercise (`ADR-002`), and that
exercise declares the equipment items it requires (`ADR-013`). Training history is therefore
evidence about a gym.

It is evidence, not proof. The account already in hand logs `Iso-Lateral Row (Machine)`, which
`TD-004`'s assumed gym excludes — the loud failure that record chose over a silent one, and
exactly the signal this decision is about. But a logged exercise can also come from a hotel gym on
a trip, a friend's garage, or a machine that has since been removed.

The asymmetry that decides this record: **what the equipment set contains changes what the
generator may draw** (`TD-016`), so a wrong addition changes next week's prescription, and a wrong
removal silently narrows the pool with no explanation the user could trace.

**Options.**

### A — Derived items are proposed; the user confirms; confirmation only ever adds
- After a sync, items implied by logged exercises and not already in the user's set are surfaced
  as suggestions with the exercise that implied each one. Accepting adds them. Nothing is ever
  removed by inference.
- **Pros:** turns the tedious screen into a list of one-tap confirmations backed by what the user
  actually did, which is the reported pain. The user stays the authority on their own gym, and
  every item in the set has a traceable reason. The add-only rule kills the worst failure outright:
  absence of an exercise from a history is not evidence the equipment is missing — it is far more
  often evidence the exercise was never programmed.
- **Cons:** a confirmation step, and suggestions can pile up for a user who trains in several
  places.

### B — Applied automatically
- Derived items enter the set on sync.
- **Pros:** no step at all; the screen could eventually disappear.
- **Cons:** one session in a hotel gym silently widens the draw pool, and next week's prescription
  changes for a reason the user cannot see or undo. It also makes the equipment set something the
  system asserts rather than something the user owns, which is the opposite of what `M2` decided.

### C — Not derived at all
- The manual screen stays the only source.
- **Pros:** nothing new to build or explain.
- **Cons:** leaves the reported pain in place while sitting on the data that fixes it, and wastes
  the loudest signal `TD-004` set up.

**Recommendation.** A — the inference is strong enough to propose and never strong enough to
apply, and the add-only rule is what keeps a sparse history from quietly shrinking a real gym.

**Decision.** A

**Consequences.**

- **A suggestion cites its evidence** — which logged exercise implied the item, and when. A
  suggestion the user cannot audit is an assertion.
- **A declined suggestion stays declined** and is not re-proposed on every sync, or the feature
  becomes noise the user learns to dismiss without reading.
- **A logged exercise outside our catalogue implies nothing**, because there is no requirement set
  to read. That is a gap in the catalogue rather than a gap in the gym, and it is worth surfacing
  separately — it is the signal that `TD-004`'s assumptions need widening.
- **Nothing here touches the generator.** Derived equipment enters the same per-user set `ADR-013`
  already defines, and everything downstream is unchanged.
