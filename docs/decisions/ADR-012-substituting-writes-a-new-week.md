---
id: ADR-012
title: Substituting one exercise writes a new week with that slot replaced
status: active
binds: [backend, frontend]
decided: 2026-08-23
---

**Context.** `M2` lets a user swap a prescribed exercise for one that trains the same thing.
`ADR-003` makes a stored week immutable, so the swap cannot edit the week it appears in — and
`ADR-009` has just established that a regeneration writing nothing is preferable to one writing
a duplicate. Substitution has to fit between the two.

The generator is deterministic and **chained**: it fills slots by whichever muscle is furthest
from its running target, so changing one exercise changes the volume credits and therefore the
choices after it. That makes "just regenerate" a materially different experience from "swap
this one".

**Options.**

### A — The swap is recorded as a preference and the week is regenerated
- Store the preference (`ADR-011`), run the generator again, write the result.
- **Pros:** One code path, already built. The preference is permanent, which is usually what
  someone swapping an exercise wants.
- **Cons:** The chaining means a request to change one exercise can return a visibly different
  week — different exercises on other days, different volume distribution. A user asking for one
  change and receiving five will not trust the button. It also conflates two requests: "not this
  exercise, ever" and "not this exercise, here".

### B — A targeted swap writes a new week identical but for the replaced slot
- Copy the current week, replace one prescription, store it as a new week (`ADR-003`).
- **Pros:** What the user asked for is what changes. Immutability is honoured with no exception
  — the previous week stays readable and explainable. Predictable enough to press twice.
- **Cons:** A second way to produce a week, so two paths must stay correct. The swap can move a
  muscle below `TD-008`'s floor, and the shortfall has to be recomputed on the new week rather
  than inherited. It does not persist the choice, so the next generation brings the exercise
  back unless a preference is also recorded.

### C — Both: swap this week, and offer to remember it
- **Pros:** Covers both readings of the request.
- **Cons:** Two mechanisms and a question on screen before either has been used once. If the
  preference model (`ADR-011`) already handles "ever", this is mostly a shortcut to it.

**Recommendation.** B — it keeps the promise the button makes, and the alternative's surprise
is not a rough edge but the direct consequence of a chained generator.

**Decision.** B

**Notes.** The candidate set for a swap needs no new column. Exercises sharing the same
`movement_pattern` and the same `primary` muscle, filtered by available equipment
(`ADR-010`), are computable from what `S1.6` already stores — which means the nullable
`movement_group` tag `TD-015` anticipated may never be needed. If a case appears that this
cannot express, that is when the tag is worth adding.

Recomputing the shortfall on the new week is not optional. A swap that drops a muscle below the
floor must say so, for the same reason `TD-008` refuses to let a coverage failure be silent —
and here the user caused it, so they can undo it.

**Revisions.**
- _(none)_
