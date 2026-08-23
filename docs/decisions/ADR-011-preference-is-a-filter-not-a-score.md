---
id: ADR-011
title: Exercise preference is stored as exclusions and preferred variants, never as a score
status: active
binds: [backend, frontend]
decided: 2026-08-23
---

**Context.** The engineer will not do a barbell overhead press and does it with dumbbells. The
catalogue holds both, and today the barbell always wins — selection orders by `order_class`
before anything else, and `Overhead Press (Barbell)` is `compound_primary` while the dumbbell
version is `compound_secondary`. There is no path by which a user's preference reaches that
decision.

The engineer also observed that people tune equipment and exercises until the programme feels
acceptable and only then start training. This corpus supports treating that as load-bearing
rather than as fiddling: the strongest finding behind exercise variety is intrinsic motivation
rather than growth, and adherence is the asymmetric cost.

What shape that preference takes is constrained by `/protocol-training`'s
`ranking-exercise-variants` note, which is explicit: factor 1 (a variant being generally
better) has an ordering but **no magnitude**, factor 2 (personal fit) has evidence for a
*different outcome* than growth, and blending them lets an invented weight on a null effect
override a real preference. Its recommendation is filter, then order.

**Options.**

### A — A per-user score on an exercise, blended with the catalogue's rank
- The user rates exercises; the generator sorts on the combination.
- **Pros:** One mechanism, expressive, and it is what most training apps do.
- **Cons:** Exactly the blend the note argues against. Every weight in the combination would be
  invented, and the invented one sits on top of a variable the evidence nulls. It also invites
  the UI to present the score as quality, which is the claim `TD-015` forbids.

### B — Exclusions plus a preferred variant per movement pattern
- Two lists. "Never prescribe this exercise", and "for this movement pattern with this
  equipment, prefer this row". Selection removes the excluded rows and, among what remains,
  honours the preferred one before the catalogue's `preference_rank`.
- **Pros:** Filter then order, exactly as the note recommends. Both halves are things a user can
  state truthfully — "I hate this" and "I do it this way" — and neither asks them to invent a
  number. Nothing claims one exercise grows more.
- **Cons:** Cannot express a mild preference; everything is on or off. A user who slightly
  prefers one variant has to either exclude the other or accept it.

### C — Exclusions only
- Just the blocklist.
- **Pros:** The smallest thing that solves the observed case: exclude barbell overhead press
  and the dumbbell one is what remains.
- **Cons:** Solves it by accident. Excluding is a blunt way to say "I prefer the other", and it
  removes an exercise from consideration everywhere, including from sessions where it was the
  only thing covering a muscle.

**Recommendation.** B — it matches the composition the knowledge note actually recommends, and
its two halves map onto the two things the engineer said, which are different statements.

**Decision.** B

**Notes.** What a preference may and may not override is **not** decided here — it is a
training judgement and needs a `TD` with research behind it (standard 15). This record decides
only the shape the preference is stored in.

One consequence is worth stating because it will look like a bug: substituting a barbell
overhead press for a dumbbell one changes the `order_class`, and `order_class` is what carries
the repetition range, the proximity to failure and the rest interval (`TD-009`, `TD-010`,
`TD-011`). The prescription therefore changes with the exercise — 8-12 at 2 RIR rather than
6-10 at 3 RIR. That is correct and it should be visible, not hidden.

**Revisions.**
- _(none)_
