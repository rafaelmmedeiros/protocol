---
id: TD-025
title: A deficit is reported and never repaid, because a queue delays volume rather than losing it
status: active
knowledge: [references/volume-progression-across-a-block.md, references/weekly-set-volume-for-hypertrophy.md, references/cold-start-first-block.md]
decided: 2026-08-24
---

**Decision.**

Volume a session did not deliver is **reported and never repaid**. No cycle is enlarged, no set is
added, and no target is raised because an earlier session did not happen.

The sentence a user can be shown: **"Nothing is added to your next cycle — the sessions you have
not trained are still ahead of you. This number is how far behind your declared pace you are."**

**Why this is not merely the conservative default.** Because under `ADR-027` **there is nothing to
repay.** A queue carries an untrained session forward; the volume is delayed, not lost. What is
deficient is the *rate*, and `TD-024` already made the realised weekly rate a reported quantity
beside the per-cycle figure.

**The mechanism, as arithmetic.** With fixed weekdays and a week regenerated every Monday, a user
training four sessions but completing three starves one specific session permanently: the fourth
never runs, and next Monday's week declares the same target as though it had. If that session is
the leg day, quadriceps, hamstrings and glutes are short **every cycle, forever**, and no screen
ever says so. Under a queue the same user rotates through it:

```
week 1   S1 S2 S3        week 2   S4 S1 S2        week 3   S3 S4 S1
```

Every session runs. Every muscle reaches the cycle's full dose. What falls is the rate — three
sessions a week against four declared, so three quarters of the intended pace, **uniformly across
every muscle group** rather than concentrated on whichever one the calendar sacrificed. A
systematic per-muscle deficit becomes a uniform rate deficit, and only the second is a number a
user can act on.

**Why this and not what the literature would suggest.** Repaying would mean adding sets on top of
a cycle that is going to deliver its full dose anyway, which is the manoeuvre
`volume-progression-across-a-block` covers and it does not support it. Enes et al. (2024) added 4
or 6 sets a week every two weeks and bought **strength and not size**; Barsuhn/Enes et al. (2024)
increased weekly volume by 30% or 60% against maintaining, and thickness ran **1.07 cm for
maintenance against 0.76 and 0.70** — the higher-volume arms did worst.

**That evidence is weaker for us than it looks and it still points the same way.** Both trials
started above 20 weekly sets and `TD-014` prescribes 6.0, which is a different part of the curve
by the model's own construction — the note is explicit that their null is "a null in a region we
never enter". So it is a poor argument against volume progression here. What makes it decisive
is not the trials but the target: a repayment is not progression up the curve, it is a **catch-up
above the target for someone who has just demonstrated less capacity than they declared**. The
one thing `cold-start-first-block` establishes is that over-prescription costs adherence, and
prescribing more to a user who is already behind is the exact shape of that failure.

**How the figure is expressed.** As arithmetic, never as a verdict — the pattern `TD-016` already
sets for shortfall. "Quads reached 4.5 of 6.0 this cycle" is a number the user can act on.
"You are undertraining" is a growth claim with nothing behind it.

**What it costs.**

- **A user who repeatedly avoids one specific session is still starved, and the report is the only
  defence.** The rotation above holds when sessions are completed in order. If the queue can be
  advanced past an unwanted session, the old systematic deficit returns — and **whether it can be
  is not decided anywhere**, which is a gap this record names rather than fills.
- **The deficit never resolves on its own.** It is a running total against a declared pace, so a
  user who trains at three quarters of what they declared sees it grow forever. That is honest and
  it is also demoralising, and nothing in the corpus says which of those wins. Re-declaring a lower
  frequency is the user's fix, and the report should make that legible rather than reading as an
  accusation.
- **A month of the engineer's own case, concretely:** four declared, three trained, four weeks.
  Twelve sessions completed against sixteen declared — three full cycles instead of four. Every
  muscle group is one cycle behind, which is 6.0 fractional sets, and nothing is added to catch up.
- **Two numbers now describe the same training.** Per-cycle volume says the plan was delivered;
  realised weekly rate says it was delivered slowly. A reader who sees only the first concludes
  everything is fine.

**When to revisit.**

- **Skipping is decided.** If a session can be passed over rather than only completed, this record's
  central claim — that volume is delayed rather than lost — stops holding for that path, and the
  starvation case needs its own answer.
- **`M6` progresses volume.** Progression and repayment are different operations that both add
  sets, and the record that introduces the first must say why it is not the second.
- **A user is observed abandoning after seeing a growing deficit.** That is the adherence cost this
  record accepts without evidence, and it is the only thing that would price it.
