---
id: TD-004
title: M1 programmes for a barbell-and-cable commercial gym, and assumes no selectorised machines
status: superseded-by TD-019
knowledge: [references/exercise-selection-within-a-movement-pattern.md]
decided: 2026-08-23
---

**Decision.**

`M1` generates for a gym containing: a barbell with plates, a rack, an adjustable bench;
dumbbells across a usable range; an adjustable cable station with a lat pulldown; and a pull-up
bar.

It assumes **no selectorised machines** — no leg extension, no seated or lying leg curl, no hack
squat, no pec deck, no chest press, no hip thrust machine, no calf machine.

The user is not asked about equipment in `M1` and the catalogue is not filtered at runtime. This
record is the assumption, written down so that `M2` supersedes something rather than discovering
it.

**Why this and not what the literature would suggest.** The literature is indifferent, and that
indifference is what makes the choice safe rather than arbitrary. Haugen et al. (2023) found no
hypertrophy difference between free weights and machines (SMD -0.055, p=0.751); Lopes et al.
(2019) found elastic resistance comparable to conventional for strength. Equipment modality is
not a growth variable, so assuming *less* equipment forfeits convenience, not hypertrophy —
while assuming *more* forfeits performability.

The tie is broken by root standard 7, not by training evidence, and the argument is worth
keeping because it generalises:

- **Assume rich and be wrong:** the user cannot perform the prescription, silently improvises,
  and the logged workout diverges from what was generated. That corrupts the append-only history
  every later analysis stands on, and nothing surfaces it.
- **Assume lean and be wrong:** the session is performable and merely feels less than ideal.

The first failure is invisible and permanent. The second is visible and free. When an assumption
about the world has to be made, prefer the one whose failure is loud.

**What it costs.**

- **`knee_flexion` has no good option, and this is a real hole.** Without a leg curl machine
  there is no direct hamstring knee-flexion exercise that accepts a load and RIR prescription
  cleanly — Nordic curls and slider curls do not. Hamstrings are therefore covered by the hinge
  pattern alone, which loads them at the hip and under-serves the short head of biceps femoris.
  Named here deliberately rather than left for a future session to find.
- **`hip_abduction` and `calf_raise` are thin** — bodyweight or dumbbell versions only.
- **The lengthened-position tie-break is partly unavailable.** Both of
  `references/muscle-length-and-exercise-variant.md`'s strongest trials used machines this
  record assumes away (seated leg curl, cable overhead extension — the cable survives, the leg
  curl does not).
- **A bodyweight-only or minimal home user gets a plan they cannot perform.** They are out of
  scope in `M1`. The generator should be honest about that rather than silently substituting —
  the same posture `TD-002` takes with `FrequencyOutOfRange`.

**How it shows up in code.**

- The seeded catalogue (`S1.6`) contains only exercises performable with the equipment above.
  The `equipment` attribute is populated on every row from the first migration even though
  nothing filters on it yet, so `M2` is a predicate over an existing column rather than a
  migration plus a retag of every row.
- No generator branch reads equipment in `M1`. The assumption lives in the seed data, and this
  record is where a reader finds out why the catalogue looks the way it does.

**When to revisit.**

- **`M2`, which models equipment.** This record is what it supersedes.
- **Training history import lands.** This is the better answer and worth recording as the
  intended direction: the user's Hevy history already names every exercise they have actually
  performed, so the available equipment set can be *derived* rather than assumed or asked. That
  is strictly more accurate, costs no user input, and does not conflict with `TD-001` —
  equipment would be observed, not a status inferred. Prefer it over an equipment questionnaire
  when the choice is live.
- **A user reports a prescription they cannot perform.** That is the loud failure this record
  chose; treat it as the signal working, not as a bug.
