---
id: TD-013
title: What gets cut when the prescription does not fit the time available
status: active
knowledge: [references/cutting-training-volume-under-a-time-constraint.md, references/session-time-cost-of-a-set.md, references/warm-up-cost-before-resistance-training.md]
decided: 2026-08-23
---

**Decision.**

When `TD-014`'s weekly volume will not fit the minutes `TD-012` computes, the generator relaxes
in this order and stops as soon as the week fits:

| # | Cut | Time bought | What it costs |
|---|---|---|---|
| 1 | Rest toward the `TD-011` floor: 180→120, 150→120, then all→90 | **+2 slots at any duration** | Repetitions within the set. The penalty concentrates below 2 min, so 180→120 is near-free and 120→90 is where it begins |
| 2 | Consolidate: fewer slots, more sets each, weekly volume held constant | 60 s per slot removed | Nothing measurable — exercise variety is null for growth |
| 3 | Sets per slot 3 → 2, **evenly across all slots** | ~2.5 min per slot | One third of weekly volume. Spread, never concentrated |
| 4 | Drop slots from the **end** of the session order (`TD-007`) | 7.5 min per slot | A muscle may lose its only direct work — 3 sets to 0, the steepest region of the curve |
| 5 | **Refuse**: surface a shortfall rather than emit a week that misses the floor | — | Nothing. A silent under-delivery is worse |

**Never cut:**

- **Rest below 90 seconds** (`TD-011`) — the one value the acute evidence argues against.
- **Frequency.** It is the user's stated availability, not ours to change. The evidence agrees:
  22 of 23 trained women preferred one 46-minute session to two short ones, and in a
  522,994-user cohort longer duration was associated with *better* adherence among frequent
  trainers. **The intuition that shorter sessions protect adherence is unsupported and points the
  wrong way.**
- **Repetition range as a time lever** — worth ~2% of a session, and the "rep range is free"
  finding is conditional on near-failure effort, which `TD-010` does not satisfy.

**Before any of this runs**, two things are already true from `TD-012` and cost nothing: no
general warm-up is generated, and ramping sets appear only on the first `compound_primary` slot.

**Supersets are not adopted in `M1`.** They are the largest single lever available — ~37% of
session duration at a pooled hypertrophy SMD of -0.05 — and they are declined anyway. Three
reasons: only 3 of 19 studies in that meta-analysis examined chronic outcomes, so a near-zero SMD
computed largely from acute mechanics is not a demonstration of equal long-term hypertrophy; they
raise RPE by 1.3 points and discomfort by 1.0, which lands on the first 28 days that
`references/cold-start-first-block.md` says predict retention; and they require holding two
stations at once, which `TD-004`'s gym cannot promise. Recorded as declined rather than
unconsidered — it is the obvious next lever if the budget proves too tight.

**Why this and not what the literature would suggest.** The literature has never tested a cut
ordering. Every step above has a source; **the ordering between them does not**, and that is why
the note behind this is `thin`. What orders them is one measured proportion and one curve shape:
rest is 74-79% of a session's clock, and the volume dose-response is concave, so the first lever
should be the one with the most time and least cost, and the last should be the one that moves a
muscle down the steepest part of the curve.

Concavity is also why step 3 spreads a set cut rather than concentrating it. The arithmetic
advantage is real and small — 0.3% at plausible volumes — and it is taken because it is free, not
because it is large. The strong version of the argument is only about **taking a muscle to zero**,
which is what step 4 risks and why step 4 is last before refusal.

**What it costs.**

- **Step 1 is not an edge case.** At 3 sessions of 40 minutes the ladder runs to step 1 in full on
  an entirely ordinary configuration — see `TD-014`. A user with modest availability trains at
  floor rest as a matter of course, not as an exception.
- **Step 4 can silently starve a muscle.** Dropping the last slot removes whichever muscle
  `TD-007` placed last, and under `TD-006` that is usually a small muscle reaching its target
  through direct work only. The generator must check the per-muscle floor after every drop, not
  at the end.
- **Refusal delivers nothing to a user who asked for something.** That is the `TD-002` posture
  taken deliberately: a clear shortfall the user can act on beats a week that looks complete and
  is not.
- **Declining supersets costs the 30-minute user the most.** They are the population the lever
  would have helped, and they are served a thinner week instead.

**How it shows up in code.**

- The ladder is an ordered sequence of relaxations in `Training/WeekGenerator`, citing `TD-013` at
  the line, applied until the week fits or the ladder is exhausted.
- A shortfall is surfaced as data on the generated week, not as an exception and not as display
  text (root standard 3) — the frontend owns the sentence.
- After every step, per-muscle fractional volume (`TD-006`) is rechecked against `TD-014`'s floor.
- `WeekGeneratorTests` asserts that a tight budget triggers the ladder in this order, and that no
  generated week ever silently sits below the floor without a surfaced shortfall.

**When to revisit.**

- **The budget proves too tight in practice.** Supersets are the declined lever and the first
  thing to reconsider.
- **A trial compares cut strategies.** None exists; one would move this off inference entirely.
- **`TD-012`'s transition constant is calibrated against real logged sessions.** The whole ladder
  is driven by a time model with one invented term.
- **`TD-014`'s target changes.** The ladder engages sooner or later as a direct consequence.
