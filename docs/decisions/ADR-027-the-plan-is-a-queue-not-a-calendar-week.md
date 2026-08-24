---
id: ADR-027
title: The plan is an ordered queue of sessions; the calendar week stays as the measurement window
status: active
binds: [backend, frontend]
decided: 2026-08-24
---

**Context.** `TD-003` assigns every session a weekday and `ADR-008` anchors a generated week to
a week whose days have not yet passed. Both rest on one argument, and it is an analysis argument
rather than a training one: a week that does not align to the calendar week makes "which week
did this session belong to" unanswerable, and every later analysis stands on that question.

That argument conflates two things which separate cleanly — the **window volume is measured in**
and the **shape the prescription takes**. Root standard 6 constrains the first and says nothing
about the second: what was performed can still be bucketed into Monday-anchored weeks while the
plan advances as a queue.

The constraint forcing the choice is not comfort. The generator receives no record of what was
performed, so a fixed-weekday split whose fourth session is the one life keeps taking produces a
**permanent per-muscle deficit**: the same target is declared every Monday as though the missed
session had happened, and no later week repairs it. On the training side there is nothing to
defend the weekday assignment with — `per-muscle-training-frequency` is graded `settled` and
finds that with weekly volume fixed, how it is distributed across days does not change growth.

**Options.**

### A — An ordered queue with no day assigned
- A generated plan is a list of sessions with positions and no dates. The next session is
  whichever is next unfinished. The calendar week remains the window that performed volume is
  bucketed into, for every analysis and every report.
- **Pros:** Removes the category of "expired session" entirely, and with it the deficit above. A
  session missed on Tuesday is simply the next one. Honest on screen: nothing shows a date the
  system does not honour. `ADR-008`'s anchoring problem disappears rather than being tuned.
- **Cons:** The weekly volume target (`TD-014`) is defined per week and now has no week to
  attach to — a dose window has to be decided as a training judgement before this can be built.
  `TD-003`'s distribution of rest days stops being enforced, so a user may stack five sessions
  in five consecutive days. `ADR-015` and `ADR-017` push a folder named for a week start, which
  needs re-reading.

### B — A queue with the weekday kept as a suggestion
- Sessions keep `Monday`, `Tuesday` and so on as display, while the queue advances on execution.
- **Pros:** Familiar to read, and `TD-003`'s rest distribution survives as guidance. Smaller
  change: `week_start_date` keeps its meaning.
- **Cons:** The screen shows a date the system does not act on, which is precisely the class of
  number this milestone exists to remove. It also invites the reader to believe the suggestion is
  load-bearing, and nothing distinguishes a suggestion honoured from one ignored.

### C — Keep fixed weekdays and only report the deficit
- No model change. The accumulated shortfall is surfaced and the user decides.
- **Pros:** Cheapest by a wide margin. `ADR-003`, `ADR-008`, `ADR-015` and `ADR-017` are all
  untouched, and the milestone shrinks to its legibility half.
- **Cons:** Leaves the strongest finding of the feedback session in place. The system would
  measure a deficit it causes, every week, and name it without fixing it — which reads as
  diagnostics rather than programming.

**Recommendation.** A — the deficit is systematic, has a mechanism, and B only relabels it. The
cost that matters is real and is paid once: the dose window becomes a training judgement, and
that judgement has to be recorded anyway the moment anything reads volume across weeks.

**Decision.** A

**Consequences.**

- **Root standard 6 is untouched and load-bearing.** Performed volume is still bucketed into
  Monday-anchored weeks. What is removed is the *prescription's* dependence on the calendar, not
  the analysis's.
- **`ADR-008` is superseded by this record.** Anchoring a plan to a week it can fill answers a
  question that no longer exists.
- **A dose window must be decided before the generator changes.** `TD-014`'s target is weekly and
  a queue has no weeks; that is a training judgement and is a research step of `M5`, not a choice
  made here.
- **Rest distribution stops being enforced and becomes the user's.** `TD-003` valued it, and
  nothing in the corpus prices what losing it costs — `per-muscle-training-frequency` equates
  volume by design and is silent on scheduling.
- **The Hevy push has to be re-read, not assumed.** `ADR-015` pushes a folder of one routine per
  session named for a week start, and `ADR-017` refuses to rewrite routines once a week has been
  trained from. Both are still coherent — a queue is still a finite list of sessions — but the
  folder's name is a week's and needs a decision this milestone must not skip.
