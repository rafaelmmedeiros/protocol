# M5 — progress

Status: `in-progress`

One entry per step of `plan.md`, in the plan's linearised order. The `Observations` line is the
point: git carries what changed, this carries what a future session would otherwise rediscover.

### S5.1 — Research: which splits a frequency may offer
- **Status:** completed
- **Tests:** no tests — this step produced records
- **Observations:**
  - **`ScopeOf(Push)` and `ScopeOf(Pull)` union to exactly `ScopeOf(Upper)`**, which is why
    `Upper / Lower / Push / Pull / Legs` lands on precisely 2x for every upper-body group and not
    on a ragged 1x-3x. Nothing states that anywhere in `SplitTemplate` and nothing enforces it: a
    future session moving one muscle between Push and Pull — rear delts are the obvious candidate
    — silently drops that template below the floor this record measured it against.
  - **`Upper / Lower` at two sessions was measured rather than assumed, and it fails uniformly**:
    1x to all sixteen groups. `TD-003` mapped two days to full body without recording the
    arithmetic, so the two-session row now has evidence under it rather than a convention.
  - **The `P12` gate paid immediately.** The nine per-muscle figures in `TD-023` came from walking
    each candidate cycle through the real `ScopeOf` in a throwaway probe, not from reasoning about
    the table. Writing them by hand would have been plausible and unverified — which is exactly
    how `TD-021` was written the day before and superseded within the hour.
  - **`TD-003`'s rest distribution died with `ADR-027` and nothing replaced it.** That record
    refused to stack sessions Mon-Tue-Wed with four days off; there are no weekdays now, so the
    user's own spacing decides it. `TD-023` records the loss rather than papering over it, and the
    corpus cannot price it — every trial equates volume and reports frequency per week.

### S5.2 — Research: the dose window when the plan is a queue
- **Status:** completed
- **Tests:** no tests — this step produced records
- **Observations:**
  - **The question was decided by `ADR-005`, not by the literature.** Both alternatives — a
    rolling seven-day window and a Monday-anchored dose window — require the generator to know how
    fast the user will train or which sessions land in which calendar week. Both are future
    behaviour, and a generator that reads them stops being a pure function of profile and
    catalogue. The corpus could not have settled this: it says plainly that **volume as prescribed
    is not volume as performed** and that no trial models a user who does not complete it.
  - **`TD-023` had already answered it and nobody noticed, including the plan.** A cycle holds
    exactly as many sessions as the declared frequency in every row of that table, so a cycle *is*
    the declared week and no number had to move — only what the numbers attach to. The plan framed
    this as choosing between three windows; the real work was seeing that one of them was already
    there.
  - **That property is load-bearing and unenforced.** If a future template ever holds a session
    count different from its frequency, `TD-024`'s central claim quietly stops being true and
    nothing fails. `S5.8` carries the test.
  - **The compressed case is the sharp one and it was nearly missed.** The stretched cycle —
    eleven days, ~3.8 sets a week — is the obvious worry and it is merely slower progress. The
    mirror is worse: a two-session template consumed three times in a calendar week reaches 18
    fractional sets, above the ~12 where the meta-analyses stop agreeing, and it means `TD-022`'s
    "8.0, exceeded nowhere" no longer bounds a calendar week. Reported rather than blocked,
    because blocking it is a decision that should be taken against an observed case.

### S5.3 — Research: what happens to volume a missed session did not deliver
- **Status:** completed
- **Tests:** no tests — this step produced records
- **Observations:**
  - **The step's own premise was wrong and the plan carried the error.** It asks what happens to
    volume a missed session "did not deliver", which presumes volume was lost. Under `ADR-027`
    nothing is lost: the untrained session is carried forward and its volume arrives late. The
    real question was never repay-or-report — it was noticing there is nothing to repay, and that
    the deficient quantity is the **rate**, which `TD-024` had already made reportable the step
    before.
  - **The queue's effect on the engineer's own case is a change of kind, not of degree.** With a
    week regenerated every Monday, a user completing three of four sessions starves whichever
    session sits fourth — permanently, invisibly, and concentrated on those muscles. A queue
    rotates it (`S1 S2 S3` / `S4 S1 S2` / `S3 S4 S1`), so every muscle reaches the full cycle dose
    at three quarters of the declared pace. A systematic per-muscle deficit becomes a uniform rate
    deficit.
  - **A gap in the plan surfaced and is not filled here: skipping is undefined.** `S5.9`'s actions
    describe a session completing by binding or by an explicit mark, and neither is skipping.
    `S5.10`'s acceptance criterion assumes a session that "never completes" over four cycles —
    which a strict queue cannot produce, because it would simply stall there and nothing else
    would ever be trained. Either the queue can be advanced past a session or that criterion is
    unreachable, and `TD-025` is explicit that its central claim only holds while sessions are
    completed in order. **Reported rather than decided**: it changes what `S5.9` builds, and the
    skill says a wrong plan is revised through `/protocol-milestone`, not patched mid-build.
  - **The evidence pointed the right way for the wrong reason and the record says so.** Enes 2024
    and Barsuhn 2024 both argue against adding sets, but both started above 20 weekly sets and we
    prescribe 6.0 — the corpus itself calls that "a null in a region we never enter". What
    actually decides it is that a repayment is a catch-up above target for someone who has just
    demonstrated less capacity than they declared, which is `cold-start-first-block`'s
    over-prescription failure exactly.

### S5.4 — What a prescribed slot says
- **Status:** pending

### S5.5 — Per-muscle volume against the week's own target
- **Status:** pending

### S5.6 — The week screen explains itself
- **Status:** pending

### S5.7 — The split becomes a choice
- **Status:** pending

### S5.8 — The plan becomes a queue
- **Status:** pending

### S5.9 — A session is done, and the queue advances
- **Status:** pending

### S5.10 — What a muscle has actually accumulated
- **Status:** pending

### S5.11 — The ladder, containerized
- **Status:** pending
