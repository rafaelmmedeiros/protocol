---
id: ADR-030
title: The split is an optional profile choice, defaulting to the mapping rather than replacing it
status: active
binds: [backend, frontend]
decided: 2026-08-24
---

**Context.** `TD-003` maps each supported frequency to exactly one split and lists "the user asks
to choose" as the first of its own revisit triggers. That moment has arrived: at five days the
record itself calls its row the weakest in the table, says both candidates are compromises and
that no trial has compared them, and the engineer asked for the arrangement it did not pick.

Since split organisation is not a hypertrophy variable once weekly volume is equated
(`per-muscle-training-frequency`, `settled`), this is a preference with no growth cost — which
is what makes it cheap, and also what makes it easy to model wrongly by treating it as
significant.

**Options.**

### A — An optional column on the training profile, null meaning the mapped default
- `training_profiles` gains a nullable split template. Null means "whatever `TD-003`'s successor
  maps this frequency to", and a value means the user chose.
- **Pros:** Existing rows migrate as null and keep behaving exactly as they do now (standard 10 —
  forward-only, and this is additive). The distinction between *chose the default* and *never
  chose* survives, which matters when the mapping changes: a user who never chose should follow
  the new mapping, and one who chose should not be silently moved.
- **Cons:** Nullable columns invite a reader to treat null as unset-and-broken. The default has to
  be resolved in one place or two call sites will disagree about what null means.

### B — A non-null column written with the mapped value at profile creation
- Every profile stores a concrete split from the start.
- **Pros:** No null, one code path, the stored week is explainable from the profile alone.
- **Cons:** Freezes today's mapping into every existing row, so a corrected mapping never reaches
  anyone — and it would arrive as a data migration over rows that legitimately never expressed a
  preference. It also makes "the user chose this" indistinguishable from "the system defaulted",
  permanently.

### C — Not a profile field at all — chosen per generation
- The split is an argument to the generate action rather than stored state.
- **Pros:** No schema change. Suits a user who wants to try one.
- **Cons:** The choice does not survive the next generation, so a preference has to be re-entered
  every time. `ADR-004` already settled that what the user tells the system about how they train
  belongs on the profile, and preference lives beside preference.

**Recommendation.** A — the null carries information the other two destroy, and that information
is precisely what makes a future mapping change safe.

**Decision.** A

**Consequences.**

- **Which splits a frequency may offer is a training judgement and is not decided here.** It
  supersedes `TD-003` and is a research step of `M5`; this record decides only where the answer
  is stored and what null means.
- **The default is resolved in exactly one place**, and the generator reads a resolved split
  rather than a nullable one.
- **A stored week is unaffected.** `ADR-003` snapshots what a week was generated under; changing
  the profile's split does not touch a week already generated, it changes the next one.
- **The schema is decided late on purpose**, which is `/protocol-milestone`'s own rule: the field
  is only knowable once the consumer's variables are — here, once the research step has said which
  templates exist per frequency.
- **Nothing in the UI may present one split as better for growth.** `TD-003` records the null and
  `SplitTemplate` already carries the warning in a comment; a chooser is exactly where that claim
  would sneak back in as a recommendation badge.
