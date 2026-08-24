---
id: ADR-031
title: A queue pushes one folder per generated plan, named for when the plan was generated
status: active
binds: [backend]
decided: 2026-08-24
---

**Context.** `ADR-015` pushes a generated week as a folder of one routine per session, titled
`Protocol · {week start}`. `ADR-027` has just removed the week start from the plan: a plan is an
ordered queue of sessions with no dates. The folder's name no longer has a source, and `M5`'s
`S5.8` cannot be built until it does.

Two properties of the existing records have to survive whatever replaces it. `ADR-017` refuses to
re-push a plan that has been trained from, because replacing a routine a logged workout points at
is invisible corruption and creating fresh ones orphans the workout that already matched. And
`ADR-019` binds a workout to a session by `routine_id` alone, so a routine identifier stored
beside a session is the join the whole loop stands on.

**Options.**

### A — One folder per generated plan, titled for the generation timestamp
- A plan is pushed once, as a folder holding one routine per session in queue order. The folder
  is named for when the plan was generated rather than for a week it belongs to.
- **Pros:** Everything already decided survives untouched. `ADR-009` makes a regenerated plan a
  new row, so it takes the create path and gets its own folder exactly as today; `ADR-017`'s
  refusal keeps meaning what it means, and a queue reaches it no more often than a week does,
  because a whole cycle is pushed at once and there is nothing to re-push mid-cycle. Folders
  accumulate at the same rate they do now — one per plan, where it was one per week.
- **Cons:** The title looks identical in shape to today's and means something different, which a
  user scrolling Hevy could misread as the week the plan is for. It also says nothing about queue
  position, so two plans generated on the same day are distinguishable only by time.

### B — One standing folder, routines replaced as the queue advances
- A single `Protocol` folder. The upcoming sessions live in it and are refreshed as sessions
  complete.
- **Pros:** Nothing accumulates, and the Hevy app stays tidy indefinitely. It reads the way a
  queue actually behaves.
- **Cons:** It is the case `ADR-017` rejected, reached deliberately instead of by accident. A
  refreshed routine that a logged workout already points at leaves that workout describing a
  prescription which did not exist when it was performed — and under root standard 7 the history
  then records a plan nobody executed. The corruption is silent and arrives on the loop's own
  join.

### C — Push only the next few sessions, folder per plan
- As A, but only the head of the queue is mirrored, topped up as sessions complete.
- **Pros:** Less clutter than A, and closer to how a queue is consumed.
- **Cons:** Topping up means writing into a folder belonging to a plan that has been trained
  from, which is `ADR-017`'s refusal again in a narrower form — a new routine in an old folder is
  safe, but the rule that distinguishes it from an unsafe rewrite has to be invented and
  maintained. It also makes the pushed state a moving subset of the plan, so "what is in Hevy" is
  no longer answerable from the plan alone.

**Recommendation.** A — it is the only option that changes nothing but a string. B is the failure
`ADR-017` exists to prevent, and C buys tidiness by making the mirror partial, which costs the one
property that makes the push debuggable: what is in Hevy is exactly what the plan holds.

**Decision.** A

**Consequences.**

- **`ADR-015` and `ADR-017` are unchanged in substance.** One is a folder of one routine per
  session; the other refuses a re-push of something trained from. Only the folder's title loses
  its week and takes the generation timestamp instead.
- **The title stays display-only, on both sides.** Nothing reads it — `ADR-019` matches on
  identifiers and root standard 9 forbids a title being an identity. A user misreading the date is
  a legibility cost and never a correctness one.
- **A cycle is pushed whole, which is why the refusal stays rare.** The queue advances through
  sessions that are already mirrored; a push happens when a plan is generated, not when a session
  completes.
- **Folder accumulation is unchanged and still unmanaged.** `ADR-017` already records that nothing
  is ever deleted from Hevy by this system, and that the user tidies. A queue does not make that
  worse, but it does mean the first user complaint about clutter is a real signal rather than a
  preference — and the answer to it is not B.
