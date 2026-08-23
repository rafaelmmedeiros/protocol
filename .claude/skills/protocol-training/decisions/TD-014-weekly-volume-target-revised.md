---
id: TD-014
title: Six fractional sets per muscle group per week, superseding TD-008's eight
status: active
knowledge: [references/weekly-set-volume-for-hypertrophy.md, references/muscle-group-specific-volume-requirements.md, references/cold-start-first-block.md, references/session-time-cost-of-a-set.md, references/cutting-training-volume-under-a-time-constraint.md]
decided: 2026-08-23
supersedes: TD-008
---

**Decision.**

| Quantity | Value | Change from `TD-008` |
|---|---|---|
| Weekly target, every modelled muscle group | **6.0 fractional sets** | was 8.0 |
| Hard floor per muscle group | **4.0 fractional sets** | unchanged |
| Per-session cap per muscle group | **11.0 fractional sets** | unchanged |
| Sets per slot | **3** | unchanged |
| Differentiation by muscle group | **none** | unchanged |

Everything `TD-008` decided other than the target stands. This record exists because one number
moved, and records are append-only (`/protocol-training`, root standard 15's sibling discipline).

**Why this and not `TD-008`'s eight.** `TD-008` named 6.0 as its standing alternative and said the
target was the first thing likely to be revisited. The revisit arrived one step later, from a step
with no stake in the question, and it brought an argument `TD-008` did not have.

**The new argument is arithmetic, not preference.** `TD-012` converts minutes into slots. Taking
~4.5 fractional credits per slot and ~11 modelled muscle groups, a weekly target of 8.0 needs
about **20 slots**; a target of 6.0 needs about **15**. Against the plan's own example
configurations:

| | 3 x 40 min | 5 x 50 min |
|---|---|---|
| Slots at prescribed rest | 9 | 20 |
| Slots at the 90 s rest floor | 15 | 30 |
| Per-muscle at prescribed rest | **3.7** — below the 4.0 floor | 8.2 |
| Per-muscle at floor rest | 6.1 | 12.3 (capped at target) |

At a target of 8.0, a user training three times for forty minutes **can never reach the target**,
in any arrangement of their time — the full cut ladder only lifts them to 6.1, which clears the
floor and permanently under-delivers. At 6.0 that same user reaches the target exactly once rest
is cut to the floor. A target an ordinary, entirely reasonable configuration cannot reach is a
target set in the wrong place.

**Everything that argued for 6.0 before still argues for it.** `TD-001` binds the first block to
the lower half of 4-12 and `references/cold-start-first-block.md` says "near the bottom of the
effective range" — 8.0 was the top of the permitted half, taken on concavity alone.
`TD-008` recorded that tension rather than hiding it, which is what made this supersession
cheap.

**What the concavity argument still costs us, honestly.** `TD-008` was not wrong about the shape.
Moving from 6 to 8 fractional sets does buy real growth, and this record gives it up. At Pelland's
per-set estimate the difference is a fraction of a percent of muscle thickness over a block. That
is the price of a target every supported configuration can actually reach, and of a first block
that errs toward the adherence side of the asymmetry `references/cold-start-first-block.md`
establishes.

**What it costs.**

- **A trained user with plenty of time is under-dosed by more than before.** Against Baz-Valle's
  12-20 for trained men, 6.0 is now two tiers light rather than one. `TD-001` accepted this class
  of cost; this record deepens it. The complaint will be that the week feels easy, and it will be
  correct.
- **At generous availability the generator now stops early.** A user with 5 x 50 minutes has room
  for 20 slots and will be given about 15. **Leaving prescribed minutes unused is the intended
  behaviour** — the target binds, not the clock — but it will look like a bug to anyone who has
  not read this record.
- **The floor did not move, so the shortfall case did not go away.** A user below roughly 75-85
  weekly minutes still cannot reach 4.0 in any arrangement, and `TD-013` step 5 surfaces that.
- **Two records now describe the volume target.** `TD-008` remains readable and marked superseded;
  a reader who finds it first must follow the pointer. That is the cost of append-only, paid
  deliberately: every week this system generates was generated under one target or the other, and
  editing `TD-008` in place would make the earlier ones unexplainable.

**How it shows up in code.**

- The weekly-target constant in `Training/` cites **`TD-014`**, not `TD-008` (root standard 15).
  The floor, cap and sets-per-slot constants continue to cite `TD-008`, which still decides them.
- No other generator behaviour changes. This is one number.
- `WeekGeneratorTests` asserts that 3 x 40 min reaches the 4.0 floor after the `TD-013` ladder,
  and that 5 x 50 min stops at the target with minutes left over.

**When to revisit.**

- **The credits-per-slot figure, computed from the real catalogue.** The 4.5 estimate behind this
  record's arithmetic was not derived from `S1.6`'s data, which did not exist when it was written.
  The conclusion holds across 4.0-6.0 credits per slot, but **the first thing `S1.8` should do is
  recompute it** — and if it lands outside that range, this record is the one to reopen.
- **Escalation lands.** A second generated week makes 6.0 a starting point rather than the whole
  prescription, which is the world `TD-001` was always waiting for and would weaken the case for
  a conservative fixed target considerably.
- **The user reports the week is too easy.** That is the expected complaint and the signal this
  record traded for.
- **`TD-012`'s time constants are calibrated against logged sessions.** The whole argument above
  rests on a time model with one invented term.
