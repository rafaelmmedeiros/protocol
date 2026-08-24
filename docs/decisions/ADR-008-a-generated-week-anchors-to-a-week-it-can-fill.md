---
id: ADR-008
title: A generated week anchors to the next week it can actually fill
status: superseded-by ADR-027
binds: [backend]
decided: 2026-08-23
---

**Context.** `WeekGenerator` anchors a week to the Monday of the reference date's own week
(root standard 6). Nothing decided what happens when the reference date is not a Monday, and
the answer fell out of the arithmetic rather than out of a choice.

The engineer generated a week on Sunday 2026-08-23 and received a week starting 2026-08-17 —
five sessions, of which four days were already in the past. No test caught it: every unit test
passes a fixed reference date, and none of them asked what happens mid-week.

The constraint that makes this more than cosmetic is the volume target. `TD-014` sets a
**weekly** figure and `TD-008` a weekly floor; a week whose sessions cannot all still happen
under-delivers against its own floor by construction. The generator would be emitting a week it
already knows fails.

**Options.**

### A — Always the current week's Monday
- What it does today.
- **Pros:** Simplest. "This week" means the calendar week, with no rule to explain.
- **Cons:** Produces a week that is mostly or entirely in the past whenever it is generated
  after Monday. On a Sunday it produces a one-day week. Every such week silently misses the
  weekly volume floor, and the shortfall the generator reports would be blamed on the time
  budget rather than on the calendar.

### B — Next Monday once a fixed weekday threshold has passed
- Current week through Wednesday, next week from Thursday.
- **Pros:** Predictable, one constant.
- **Cons:** The threshold is invented. It is right for a three-day split and wrong for a
  six-day one, because it takes no account of which days the template actually uses.

### C — The current Monday only when every session of the template still lies ahead
- Derive it from the split (`TD-003`) rather than from a guess: if any day the template
  assigns has already passed, anchor to the next Monday instead.
- **Pros:** Uses data the generator already has, so there is no new constant to defend.
  Guarantees the week it emits can hold the volume it prescribes, which is what makes the
  floor and the shortfall report mean anything. Generating on Monday morning still gives the
  current week; generating on Tuesday with a Monday session gives the next one.
- **Cons:** Strict. A user generating on Tuesday loses the current week entirely even though
  Thursday and Friday remain — the system declines to give them a partial week they might have
  wanted. It also means the answer depends on the profile, so two users generating at the same
  moment can get different weeks.

### D — Let the user choose the starting week
- A control on the screen.
- **Pros:** No rule to be wrong about.
- **Cons:** Asks the user to answer a question the system has the data to answer, which is the
  posture `ADR-004` rejected for experience level. It is also a preference field with nothing
  behind it until someone actually wants a week that starts mid-cycle.

**Recommendation.** C — it is the only option whose rule comes from something already decided
rather than from a new number, and the only one that guarantees a generated week can meet the
target it was generated against.

**Decision.** C

**Notes.** The partial-week case C declines is a real thing someone will eventually ask for —
"give me what is left of this week". That is a different capability, not a different anchor,
and it needs its own record: a deliberately partial week has to be exempt from the weekly floor
or it will report a shortfall that is not one.

**Revisions.**
- _(none)_
