---
id: ADR-032
title: A session can be skipped, a skip is recorded, and skipped volume is reported apart from deferred volume
status: active
binds: [backend, frontend]
decided: 2026-08-25
---

**Context.** `ADR-027` made the plan a queue and `ADR-028` gave it two ways to advance: a workout
binds to a session, or the user marks it done. Neither is skipping, and a strict queue without
skipping **stalls**: a user who will not train legs never reaches anything behind legs, which is
worse than the fixed-weekday behaviour it replaced.

`S5.10`'s acceptance criterion — four cycles in which the same session never completes — is
unreachable without this, which is how the gap surfaced.

`TD-025` named it as its own revisit trigger. Its central argument is that a queue **delays**
volume rather than losing it, and that argument holds only while sessions are completed in order.
A skipped session is volume that never arrives.

**Options.**

### A — A session can be skipped, and the skip is stored
- Skipping advances the queue and records that it happened, with the time. Nothing is written
  into imported history.
- **Pros:** The queue cannot stall. It also keeps the one distinction that matters downstream:
  volume that has not arrived *yet* and volume that will never arrive are different failures, and
  a report that adds them together is misleading in the direction that flatters the system.
- **Cons:** A third completion route beside binding and marking, which every later reader has to
  hold. It also makes an unflattering number permanent and visible, which is the point and is
  still a cost.

### B — A session can be skipped and the skip is not recorded
- The queue advances and nothing remembers why.
- **Pros:** No column, no state, nothing to migrate.
- **Cons:** A skipped session and a session that simply has not happened become indistinguishable
  the moment the queue moves past it, so the deficit report cannot say whether the volume is
  coming. That is precisely the "shortfall the system can count becoming one it cannot" that
  `TD-016` rejected when it refused to override an exclusion silently.

### C — No skipping; reorder instead
- The user may pull a later session to the front.
- **Pros:** Nothing is ever lost, so `TD-025`'s argument survives untouched.
- **Cons:** It does not solve the case. A user who will not train legs reorders around it forever
  and the queue still never passes it, so the stall returns wearing a different interface — and
  now the system claims the volume is merely deferred while it is not.

**Recommendation.** A — B destroys the distinction the deficit report exists to make, and C
renames the stall rather than removing it.

**Decision.** A

**Consequences.**

- **`TD-025`'s decision is unchanged and one of its arguments narrows.** Volume is still reported
  and never repaid: the reasons against repaying are unaffected — a catch-up above target for
  someone who has just demonstrated less capacity than they declared is the over-prescription
  failure `cold-start-first-block` establishes. What no longer holds on the skip path is "there is
  nothing to repay". There is; we still do not.
- **Two deficits, reported apart.** *Deferred* volume sits in sessions still ahead in the queue
  and will arrive. *Skipped* volume will not. Both are numbers against a target and neither is a
  verdict (`TD-016`'s pattern), but only the first resolves by training.
- **A skip writes nothing into `performed_workouts`** (root standard 7). It is a statement about
  the plan, not about history, exactly as `ADR-028`'s mark is.
- **A skip is not a refusal.** `TD-016`'s exclusion removes an exercise from every future draw; a
  skip passes over one session of one plan and changes nothing about what is generated next. A
  user skipping the same session every cycle is telling us something, and what to do about that is
  not decided here — it is the signal a future record would read.
- **What is stored is what cannot be derived.** Binding is a join on `routine_id` (`ADR-019`) and
  stays derived; the mark and the skip are declarations and are stored. That is `ADR-029`'s test
  applied to session state.
