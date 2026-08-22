---
id: ADR-004
title: The training profile captures a goal and availability, not an experience level
status: active
binds: [cross-cutting]
decided: 2026-08-22
---

**Context.** The generator needs to know what it is programming for and what it may spend. The
first sketch of the profile had four fields — experience level, session duration, rest between
sets, weekly frequency. Two of them did not survive examination. A goal alone cannot produce a
week: it says nothing about whether the person has three days of forty minutes or five of
fifty, and those produce different programmes for the same goal. Experience level, meanwhile,
is a proxy: what actually matters is how the person responds to a stimulus, which this system
will observe rather than ask about — and it cannot observe anything yet.

**Options.**

### A — Goal, weekly frequency, session duration
- Three fields: what it is for, and what is available.
- **Pros:** Every field changes the output directly. Availability is collected rather than
  assumed, which is what stops the system from prescribing five ninety-minute sessions to
  someone with three forty-minute ones. Experience level is left out until the system can
  either observe response or justify a calibration.
- **Cons:** With no level, volume and intensity must be decided from goal and availability
  alone. The first week is therefore the same for a beginner and a veteran with the same
  availability — a real cost, taken knowingly, and the subject of the cold-start question the
  milestone leaves open.

### B — Goal, experience level, weekly frequency, session duration
- Level retained as a field.
- **Pros:** Volume can be scaled by level from the first week.
- **Cons:** Level would be measured as "how long have you really been training", which the
  literature discriminates poorly — training age and training status are not the same variable.
  Forces the research for what level *means* onto the critical path, for a field the system is
  meant to replace with observation.

### C — Availability only, goal fixed by decision
- **Pros:** The smallest possible profile.
- **Cons:** Removes the only input that says what the person trains for, and a goal fixed
  invisibly is a training judgement with no field and no question behind it.

**Recommendation.** A

**Decision.** A

**Notes.** Goal is collected as a field but `M1` programmes for one of its values; the others
are surfaced as unavailable until a decision record covers them. That keeps the schema right
from the first migration (standard 10 makes them forward-only) without multiplying the research
on the critical path. Which value `M1` supports is an open question in the milestone plan.

Whether the system needs an initial calibration in place of the level it no longer asks for is
open, and is a training judgement rather than a schema one: it goes through
`/protocol-training`, not through a revision here.

**Revisions.**
- 2026-08-22 — the goal `M1` supports is **hypertrophy**. Weight loss and every other goal are
  out of scope until a decision record covers them, and the field rejects them with
  `GoalNotSupported` rather than programming something unresearched. Chosen because it is the
  goal the described use actually is, and the one with the densest literature to research
  against.
- 2026-08-22 — the profile has three fields and no fourth: rest between sets is not collected.
  See `ADR-007`.
