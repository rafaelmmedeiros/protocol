---
id: ADR-007
title: Rest between sets is prescribed per slot, never collected from the user
status: active
binds: [cross-cutting]
decided: 2026-08-22
---

**Context.** The first sketch of the training profile had a rest field, on the reasoning that
rest is a preference. Examining a real session refuted it. A chest press opening a session runs
in the 8–12 range near two repetitions in reserve and wants well over two minutes; a forearm
movement placed last — deliberately, to exploit accumulated fatigue and to work a slow-twitch
dominant muscle through metabolic stress — runs 12–15 and is done with a minute. Both are in
the same session, for the same person, on the same day.

Rest is therefore a property of the **slot** — the exercise, its repetition range, its
proximity to failure, and its position in the session — and not a property of the person. One
number in a profile cannot express a session that legitimately contains several.

**Options.**

### A — Collected as a literal value
- The user types a rest interval and the system uses it.
- **Pros:** Trivial to build and to explain. The user feels in control.
- **Cons:** Cannot represent a session whose rests differ by slot, which is every session. The
  user silently overwrites a training judgement, and the repetition range prescribed alongside
  it may become incoherent with the rest they chose.

### B — Collected as a ceiling
- The user states the most they will tolerate; the system prescribes within it.
- **Pros:** Keeps the judgement with the system while respecting a real constraint on
  adherence. Consistent with how duration and frequency work.
- **Cons:** A ceiling below what a heavy compound needs forces a choice the user did not know
  they were making — either the prescription degrades or the ceiling is ignored. Needs a
  decided rule for that collision before the field can exist.

### C — Not collected; prescribed per slot
- The profile has no rest field. Rest is derived where the slot is decided, from the repetition
  range, the movement's demand and its position in the session.
- **Pros:** Expresses the real session. Keeps rest with the decision that produces it, where it
  can cite a record (standard 15). Removes a field whose value could not have been correct.
- **Cons:** The user cannot say "I only have an hour and long rests ruin that" — the constraint
  reaches the system only through session duration, indirectly.

**Recommendation.** C

**Decision.** C

**Notes.** What rest *is* per slot is deliberately not decided here — it is researched with the
standard session models, in the milestone's prescription research step, and lands as a `TD-###`.
This record settles only that the user is not the one who answers it.

Rest days are a different question with a different answer and must not be folded into this
one: they follow from weekly frequency, and which days are off is its own training judgement.

The ceiling in option B stays available for the day session duration proves too indirect a
constraint — that would be a new record superseding this one, not a revision of it.

**Revisions.**
- _(none)_
