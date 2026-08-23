---
id: TD-007
title: Within a session, exercises are ordered by order_class — a convention chosen for quality and safety, not for growth
status: active
knowledge: [references/exercise-order-within-a-session.md]
decided: 2026-08-23
---

**Decision.**

Slots within a session are ordered by `order_class` (`TD-005`), then by `preference_rank`:

1. `compound_primary` — heavy loaded bilateral patterns
2. `compound_secondary` — everything else multi-joint
3. `isolation` — single-joint accessories

**Pre-exhaustion is rejected.** No session pairs an isolation exercise immediately before a
compound for the same muscle.

**Small muscles trailing the session is allowed and expected.** Arms and delts landing last is a
consequence of this ordering, and it is explicitly not a defect.

**Why this and not what the literature would suggest.** The literature suggests nothing about
ordering *for hypertrophy*, and that is the finding rather than a gap. Nunes et al. (2021), the
only meta-analysis on the question — 11 studies, 268 participants — found ES = 0.03, p = 0.862.
A point estimate at zero.

So this ordering is **ours, and its justification is not growth**. It is adopted for technique
quality under fatigue, injury risk in loaded spinal and overhead patterns, and load preservation
in the exercises where load is hardest to recover. **None of those three is measured in any
source in this corpus.** That reasoning is `thin` and it is recorded here rather than in the
knowledge note precisely so it cannot be mistaken for evidence — the same separation `TD-003`
maintains for split templates.

The one thing the evidence does say about order is a specificity result that is routinely
misread: whatever goes first gains most strength *at that task*. Multi-joint-first improved
multi-joint strength (ES = 0.32); single-joint-first improved single-joint strength
(ES = -0.58, the larger of the two). ACSM 2009's "large before small, multi-joint before
single-joint" is a strength and session-quality rule whose own stated rationale is preserving
exercise intensity — not a hypertrophy rule. **It must not be cited as one.**

Pre-exhaustion is the one scheme with evidence pointing against it: Hermann et al. (2025), 48
trained participants, found traditional order slightly better for muscle size with acknowledged
uncertainty, and Trindade et al. (2019) found pre-exhaustion lowers the compound's load without
a hypertrophy payoff. It is rejected for absence of reason to build it, not because it is
harmful.

**What it costs.**

- **The exercise placed last gains less strength in itself** — ES = -0.58 in the reverse
  direction. For a hypertrophy goal this is not a cost we are optimising against, but it is a
  real effect and a user training for strength in a specific accessory would be served worse.
- **The engineer's instinct — placing a small muscle last to exploit accumulated fatigue — is
  accommodated but not endorsed.** The evidence says it costs nothing detectable. It does not
  say it adds anything, and this record must not be read as claiming a benefit from
  pre-fatigue.
- **The convention is unfalsifiable at our scale.** With order free for growth, nothing the
  product observes will ever tell us this ordering was right or wrong. It is a choice we are
  stuck with until the literature moves.
- **Ordering across a week is not addressed at all.** Whether a muscle should lead one session
  and trail another is unstudied and unhandled.

**How it shows up in code.**

- `Training/WeekGenerator` sorts a session's slots by `(order_class, preference_rank)`, citing
  `TD-007` at the line. Total and deterministic (`ADR-005`).
- The ordering is computed from stored attributes, never hardcoded per split template — so when
  this record is superseded, one sort changes and no template does.
- No superset or pre-exhaustion pairing structure exists in the generated week.

**When to revisit.**

- **A better-powered ordering meta-analysis lands.** The current null rests on 268 participants
  with mixed measurement quality.
- **A goal other than hypertrophy is supported.** The specificity finding becomes load-bearing
  immediately: for strength, what goes first is a real decision.
- **Session length forces a cut that changes order** (`S1.5`). If cutting the tail systematically
  removes the same muscle group every week, the interaction between the cut rule and this
  ordering needs its own record.
- **The engineer wants a specific muscle prioritised.** That is a specialisation feature, and it
  would supersede this record rather than configure it.
