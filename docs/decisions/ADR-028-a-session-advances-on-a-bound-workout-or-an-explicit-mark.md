---
id: ADR-028
title: The queue advances on a bound workout, and on an explicit mark when nothing bound
status: active
binds: [backend, frontend]
decided: 2026-08-24
---

**Context.** `ADR-027` makes the plan a queue, and a queue needs to know when a session
happened. `ADR-019` already decided how a workout binds to the session that prescribed it:
`routine_id` alone, no title matching, no exercise-overlap heuristic.

**What was unknown until now was whether that ever fires in practice**, and the repo carried a
misreading of its own evidence. Every workout in the live account came back with
`routine_id: null`, which had been read as a signal about how the engineer starts a workout. It
was not: the account held **zero routine folders and one routine** — a leftover test — so there
was nothing to bind to. The training is logged free-form with a typed title, and titles like
`Push`, `Legs` and `Walking - Routine` look like routine names and are not (root standard 9, one
level up).

**The experiment has now been run outside the controlled case.** A generated week was pushed as
four routines, one session was trained by opening the pushed routine in the Hevy app, and the
sync returned it bound to session 1 — recorded as a `Revisions` bullet on `ADR-019`. The
mechanism fires in the real flow. What is still unknown is the **rate**: whether a user reaches
for the routine every time, including on a rushed day.

**Options.**

### A — Bound workout advances it; an explicit mark is the fallback
- A session is done when a workout binds to it. When none has, the user can mark it done on the
  week screen, and the queue advances.
- **Pros:** Works on day one at any binding rate, including zero, which is the state a user who
  has not yet pushed anything is actually in. Adds no second matching mechanism — the mark is a
  statement by the user, not an inference. Divergence between marked and imported stays visible,
  and `WeekComparison` already reports prescribed against performed for exactly that.
- **Cons:** A manual action in the cases that did not bind, and two ways for a session to be
  done, which every later reader of the model has to hold.

### B — Bound workout only
- No mark. A session that did not bind stays at the head of the queue.
- **Pros:** One mechanism, and it pushes the habit that makes the rest of the loop work.
- **Cons:** A workout opened empty on a rushed day never advances the queue, and the plan then
  diverges from what was trained with no way back short of pushing and training again. It also
  makes the product unusable before the first push, which is every new user's first week.

### C — Infer the session from exercise overlap
- Match an unbound workout to whichever prescribed session its exercises best cover.
- **Pros:** No manual action, no dependence on Hevy's field.
- **Cons:** A second matching mechanism beside `ADR-019`, which that record rejected on exactly
  this ground. It fails silently and often: on an Upper/Upper split both sessions share most of
  their exercises, so the wrong session advances and the error is invisible.

**Recommendation.** A — the experiment proved the join works, so binding carries the normal case
and the mark exists for the exception. The fallback is a declaration rather than a guess, which
is the property `ADR-019` protected when it refused C.

**Decision.** A

**Consequences.**

- **`ADR-019` is unchanged and still the only matching rule.** The mark is not matching; it moves
  a queue position and asserts nothing about which workout corresponds to what.
- **The binding rate becomes measurable for the first time**, since a marked-but-unbound session
  is exactly the case that was invisible before. That is the evidence `ADR-019` named as what
  would justify revisiting it.
- **A mark is a user statement and history is not rewritten by it** (root standard 7). Nothing is
  written into `performed_workouts`; a session carries its own completion, and a later import that
  binds a workout to it does not contradict the mark, it explains it.
- **`WeekComparison`'s coverage figure is wrong today and must be fixed by whichever step touches
  it.** It reports bound over every imported workout — 1 of 759 in the live database — while 758
  of those predate the first push and could never have bound. Recorded on `ADR-019` as well.
