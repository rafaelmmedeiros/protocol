# M4 — progress

**Status:** in progress

One entry per step of `plan.md`, in the plan's linearised order. Git carries what changed; this
file carries what a future session would otherwise rediscover.

### S4.1 — What a default gym contains
- **Status:** completed
- **Tests:** no tests; `check-docs` proves the citations resolve (104 documents, no drift)
- **Observations:**
  - **The plan's closed question was reopened by the record it was superseding, and the engineer
    changed their answer.** `TD-004` did not choose a lean gym arbitrarily: it chose by the
    asymmetry of the failure — assume rich and be wrong and the user silently improvises, which
    corrupts the append-only history invisibly; assume lean and be wrong and the session is merely
    less than ideal. Its own *when to revisit* then named the better answer and said to prefer it
    **when the choice is live**: derive the equipment set from history. `M3` built the derivation
    and `M4` gives it something to derive, so the choice is live and the record's own instruction
    was followed. Reading the record before superseding it is what surfaced this; citing it would
    not have.
  - **`TD-004` held two things and only one of them moved.** It scoped the *catalogue* to the
    assumed gym and it defined the *assumed gym*. `TD-019` separates them: the catalogue models
    every movement including machines, and the assumed gym is unchanged. That separation is the
    whole content of the supersession, and it is why `ExerciseCatalogue.AssumedGym` must not gain
    a machine in any later step.
  - **The circularity that kept `ADR-020` silent is broken by `S4.3`, not by this step.** A logged
    machine exercise implied no equipment because no machine existed in the catalogue to carry a
    requirement. Widening the catalogue breaks it without widening any assumption — which is why
    the default can stay lean and the engineer still ends up with their machines.
  - **Code still cites `TD-004` in nine places and was deliberately not touched.** The plan says
    this step writes no code, and the citations are not yet false: the catalogue *is* still scoped
    to the assumed gym today. `S4.3` is the step that falsifies them, and standard 18 puts the
    correction in the commit that does. `ExerciseCatalogue.cs` lines 14 and 17 are the two that
    will read as lies the moment a machine row lands.

### S4.2 — Equipment specific enough to name a machine
- **Status:** pending

### S4.3 — The catalogue widens
- **Status:** pending

### S4.4 — What a logged load means
- **Status:** pending

### S4.5 — How far the catalogue still is
- **Status:** pending

### S4.6 — Erasing everything of mine
- **Status:** pending

### S4.7 — The ladder, containerized
- **Status:** pending
