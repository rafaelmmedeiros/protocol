---
id: TD-009
title: Repetition ranges by order_class, chosen for RIR accuracy and joint demand rather than growth
status: active
knowledge: [references/repetition-range-and-load-for-hypertrophy.md, references/proximity-to-failure-and-hypertrophy.md]
decided: 2026-08-23
---

**Decision.**

| `order_class` | Repetition range |
|---|---|
| `compound_primary` | **6-10** |
| `compound_secondary` | **8-12** |
| `isolation` | **10-15** |

Nothing is prescribed above 15 repetitions in `M1`, and nothing below 5.

**Why this and not what the literature would suggest.** The literature suggests no rep range at
all, and that is the finding. Lopez et al. (2021), a network meta-analysis, found no hypertrophy
difference between low, moderate and high load; ACSM 2026, across 137 reviews, states that loads
from 30% to 100% of 1RM all produced hypertrophy **provided sets were taken close to failure**.
Rep range is free for growth across roughly 5-30 repetitions.

So this table is convention that a genuine null permits, and its justification is not growth:

- **`compound_primary` is kept under ~12 repetitions for one evidence-linked reason.** Halperin
  et al. (2022), 414 participants, found RIR prediction accuracy improves as repetitions to
  failure fall, and is statistically indistinguishable below about 12 repetitions per set. The
  primary compound is the slot where a missed RIR costs most — the load is heaviest, the
  technical demand highest, and the fatigue consequence largest. A lower rep range makes the RIR
  prescription in `TD-010` more likely to be hit.
- **The gradient across the three classes is convention.** Nothing distinguishes 8-12 from 10-15
  for growth. The gradient tracks descending load and joint demand, and it makes later slots
  cheaper in time and in discomfort.
- **The ceiling at 15 rather than 30 is deliberate**, and it is the one place this record departs
  from the permitted band on purpose. See the cost below.

**What it costs.**

- **The ceiling at 15 forfeits a range the evidence permits.** Sets of 20-30 grow muscle when
  taken near failure. They are excluded because `TD-010` prescribes 2-3 RIR, and **the
  rep-range null is conditional on near-failure effort** — a 15-25 rep set at 2-3 RIR with a
  light load is the least-evidenced cell in the entire prescription, tested by none of the
  meta-analyses behind it. Excluding it is conservative, and it costs a user who would prefer
  higher-rep work the option.
- **The lower bound at 5 forfeits heavy low-rep work**, which grows muscle equally well and
  builds more strength. It is excluded because the goal is hypertrophy (`ADR-004`) and because
  low-rep work raises joint and technical demand for a user the system has never observed
  (`TD-001`).
- **Ranges rather than fixed numbers push a decision to the user.** Someone told "8-12" chooses
  within it, and the system does not know which they picked until the set is logged. A single
  number would be more deterministic and less usable; this is a usability choice with an
  accounting cost.
- **The table is unfalsifiable at our scale.** With rep range free for growth, nothing the
  product observes will show that these bands were right or wrong.

**How it shows up in code.**

- A range per `order_class` in `Training/`, citing `TD-009` at the line (root standard 15), read
  by the generator when it prescribes into a slot (`TD-005`).
- Stored on the prescription as a minimum and maximum, not as free text — the frontend renders
  "8-12" and the backend never sends display text (root standard 3).
- No generated prescription may assert a rep range was chosen for growth. If the UI explains the
  range at all, the honest reason is effort accuracy and joint demand.

**When to revisit.**

- **A trial tests moderate-RIR high-rep work.** That is the missing cell, and it is what the 15
  ceiling is hedging against. Evidence there would widen the band.
- **`TD-010` changes.** The two are coupled: the rep ceiling exists *because* the RIR target is
  conservative. Moving RIR toward failure would make higher-rep sets defensible again.
- **A goal other than hypertrophy is supported.** Load is not free for strength — Lopez found
  high and moderate load clearly superior — so this table would not survive the change.
- **The user reports the ranges do not suit them.** Preference is an adherence variable
  (`references/cold-start-first-block.md`), and here the evidence gives us room to move.
