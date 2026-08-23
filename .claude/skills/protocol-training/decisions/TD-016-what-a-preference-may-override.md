---
id: TD-016
title: A preference filters and reorders the draw pool, and never touches the volume arithmetic
status: active
knowledge: [references/self-selected-exercise-and-autonomy.md, references/indirect-only-volume-and-the-coverage-floor.md, references/ranking-exercise-variants.md]
decided: 2026-08-23
---

**Decision.**

| A preference **may** override | A preference **may not** override |
|---|---|
| Which catalogue row fills a slot | Whether the slot exists — slot count is `TD-012`'s minutes arithmetic |
| `preference_rank`'s draw order, entirely and without exception | The repetition range (`TD-009`), proximity to failure (`TD-010`) or rest (`TD-011`) |
| Removal of an exercise from the draw pool, **unconditionally** — including the last one that trains a muscle | The weekly target or floor (`TD-014`, `TD-008`) — an exclusion produces a shortfall against the arithmetic, it does not relax it |
| Which of several equally-covering exercises is chosen | `order_class` ordering within a session (`TD-007`) |

One sentence: **a preference is a filter on the draw pool and a reordering of it, never an input
to the volume arithmetic.**

**When an exclusion starves a muscle**, in order:

1. **Substitute within what remains, without asking.** That is not an override — it is the
   generator doing its ordinary job over a smaller catalogue. The user excluded an exercise, not
   a muscle.
2. **When the remaining pool cannot reach the floor, honour the exclusion and surface the
   shortfall per muscle, with the number.** "Rear delts reach 2.0 of 6.0 this week" is
   arithmetic. "Your programme is inadequate" is a growth claim with nothing behind it, and it
   would be the fourth instance of the rule `TD-003`, `TD-007` and `TD-015` already state.

**There is no threshold on how much a user may exclude**, and that is deliberate — see below.

**Why this and not what the literature would suggest.** The literature does not suggest anything
here: no trial has randomised exercise *selection* against assignment with adherence as an
outcome. What exists is a general autonomy effect that reaches device-measured behaviour
(d = 0.29 across 73 SDT interventions) carried across two levels of abstraction, and two
resistance-training choice trials that moved **perceived autonomy** hard and **enjoyment not at
all**. So the case for honouring a preference is that it costs nothing measurable, not that it
grows more muscle or is more fun.

The three rejected options for the starvation case are worth keeping:

- **Refuse the exclusion.** Rejected on auditability, which is the strongest argument available
  and is mechanism rather than evidence: a refused exclusion becomes an **unlogged skip**. That
  converts a shortfall the system can count into one it cannot, and under root standard 7 the
  history then records a plan that was never executed, with every later analysis computed
  against fiction.
- **Override the preference silently at the starved muscle.** The worst option. It does the thing
  the user forbade, precisely where they will notice, and the one construct these trials show the
  manipulation reliably moves is perceived autonomy — exactly what a covert override destroys. It
  also breaks `TD-008`'s never-silent property in the single case that property exists for.
- **Relax the floor.** The shortfall is real and concave-curve-expensive; hiding it makes the
  number meaningless.

**Why preference stops at the exercise and does not reach load, repetitions or rest.** This is
the one place self-selection has a *measured* price: left to choose their own load, trainees pick
**53% of 1RM** (18 studies, 368 participants), with little moderation by experience. Choosing
which exercise is free; choosing how hard is not.

**What it costs.**

- **A user can knowingly build a week that misses the floor**, and the system will let them. That
  is the trade taken: an honest shortfall the user caused and can undo, over a covert correction
  they did not ask for.
- **The exchange rate is unknown and unknowable from the literature.** How many fractional sets
  one unit of preserved adherence is worth has never been estimated in any population, and both
  halves of it are separately unpriced. This record is a bet with its direction argued, not a
  calculation.
- **The starvation case is narrow, which cuts both ways.** For muscles reachable through
  compounds the evidence that indirect-only volume is worse is contested and possibly absent — so
  most exclusions cost nothing. The cost concentrates on side delts, rear delts and calves, which
  no compound covers. The rule is being asked to be right about a rare case rather than a common
  one, which is reassuring and also means it will rarely be tested.
- **No threshold means no protection against a user who excludes almost everything.** Accepted,
  because "fraction of catalogue excluded" is not a meaningful quantity: excluding half a
  catalogue costs nothing if the half is redundant variants and costs a muscle group if it is the
  only three rows loading rear delts. The quantity that carries information — how many muscles
  are below 4.0 and by how much — is one the generator already computes. An invented threshold
  would replace a real number with a worse one.

**How it shows up in code.**

- Preference is applied as a filter over the draw pool and a reordering of it, before selection —
  never multiplied into a score (`ADR-011`), and never reaching `TrainingPrescription`.
- The starvation path reuses the existing shortfall channel rather than adding one: a muscle
  starved by an exclusion and a muscle starved by a missing machine are the same report, because
  they are the same arithmetic (`TD-008`).
- No constant in the generator expresses "too many exclusions". If one appears, it was invented.
- Any user-visible sentence about coverage names the muscle and the number. The wording
  discipline is `TD-015`'s.

**When to revisit.**

- **Training history import lands.** Then the system can compare a stated exclusion against
  behaviour, and the note's distinction becomes testable: an exclusion is a report of past affect
  — the one construct with a demonstrated forward link to behaviour — while an intention is not.
- **A trial randomises exercise selection with adherence as the outcome.** It would replace the
  largest inference in this record.
- **The catalogue grows enough that exclusions stop biting.** The starvation case is narrow now
  because the catalogue is small; more rows per muscle make it narrower still.
- **A user actually excludes enough to starve something.** That is the first real test of this
  record, and what they do next is the evidence it currently lacks.
