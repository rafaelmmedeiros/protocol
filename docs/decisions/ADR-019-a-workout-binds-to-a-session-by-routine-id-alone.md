---
id: ADR-019
title: A logged workout binds to a prescribed session by routine_id alone, and unbound history still counts
status: active
binds: [backend]
decided: 2026-08-23
---

**Context.** Comparing what was prescribed against what was performed is the product's reason to
exist, and it needs a join. A live experiment settled what is available: a routine was created
through the API, trained from, and the resulting workout came back carrying that routine's
`routine_id`. **The association is Hevy's own.** The same experiment established two boundaries —
the workout inherited the routine's **title**, and the routine's per-exercise **notes did not
propagate at all**.

A join that binds the wrong workout to a session does not fail loudly. It produces a comparison
that looks exactly like a correct one and is wrong, which then feeds `M4`'s progression. Of every
decision in this milestone, this is the one whose failure mode is least visible.

**Options.**

### A — `routine_id` only
- A workout binds to a session when its `routine_id` matches one this system pushed
  (`ADR-015`). A workout with no `routine_id`, or with one we did not create, is imported as
  history and bound to no session.
- **Pros:** exact, and it rests on an identifier the vendor populates rather than on anything of
  ours smuggled into a display field. The unbound case degrades honestly: the workout still counts
  toward fractional volume (`TD-006`) and toward equipment inference (`ADR-020`), which is where
  progression reads anyway (`load-increment-granularity-and-progression`). Nothing is lost except
  the session-level comparison, which genuinely did not happen.
- **Cons:** training freestyle — the ordinary case of walking in and lifting — produces history
  that never compares against a prescription.

### B — `routine_id`, falling back to the title
- When `routine_id` is absent, match the inherited title against the pushed routine's title.
- **Pros:** recovers some workouts whose routine link is missing.
- **Cons:** standard 9 — a title is display only and is never matched, grouped, keyed or compared
  on. It is also a string the user can edit and that the product intends to translate, so the
  fallback would break the day the app speaks `pt-BR`. Recovering a few matches is not worth a
  join that a rename silently severs.

### C — `routine_id`, falling back to a date-and-exercise-overlap heuristic
- Bind an unmatched workout to the session scheduled nearest it whose exercises overlap enough.
- **Pros:** binds the most workouts. Handles the user who trains the prescribed session without
  opening the routine.
- **Cons:** there is no threshold to defend and no evidence to set one from, so the number would be
  invented — the failure standard 15 exists to prevent, wearing engineering clothes. A wrong bind
  is invisible and corrupts the comparison. Push-day sessions legitimately share most of their
  exercises, so overlap is weakest exactly where the sessions are hardest to tell apart.

**Recommendation.** A — the exact join is available because the experiment made it available, and
both fallbacks trade an invisible correctness risk for coverage the unbound path already handles
at the level progression actually reads.

**Decision.** A

**Consequences.**

- **The title is written and never read.** `ADR-015` gives routines useful titles for a human
  scrolling Hevy; nothing in the import consults them. The inherited title is at most a second
  confirmation for a person debugging, never an input.
- **Unbound history is first-class, not a leftover.** It is what makes `ADR-020` work at all, and
  it is the majority case for any user with a training history predating this system.
- **A bound workout is bound to a specific pushed week**, which is why `ADR-017` refuses to rewrite
  routines once a week has been trained from.
- **Coverage is measurable.** The proportion of imported workouts that bind is a number the system
  can report, and if it is low in practice that is evidence for revisiting this record — with
  data, rather than with a heuristic chosen in advance.

**Revisions.**

- 2026-08-24 — **The join was exercised outside a controlled experiment for the first time, and it
  bound.** A generated week was pushed as four routines; one session was trained by opening the
  pushed routine in the Hevy app; the sync returned it carrying that routine's id, matching
  session 1 (`Upper`, Monday). One of one. Nothing in the decision changes — this is the evidence
  the last consequence above asked for, and it points the same way the record does. Two things it
  also settled: the engineer's account had **no routines at all** before the push (one leftover
  test routine, zero folders), so the `routine_id: null` on all of their prior history was never a
  statement about how they start a workout; and the workout inherited the routine's title
  verbatim, exactly as the first consequence predicts.
- 2026-08-24 — **The coverage figure this record calls measurable is currently misleading, and
  that is a defect in the measurement rather than in the decision.** `WeekComparisonBuilder`
  reports bound over *every* imported workout — 1 of 759 — but 758 of those predate the first push
  and could never have bound. The rate this record wants is over workouts that had a routine to
  bind to. Fixing the denominator belongs to whichever step next touches the comparison.
