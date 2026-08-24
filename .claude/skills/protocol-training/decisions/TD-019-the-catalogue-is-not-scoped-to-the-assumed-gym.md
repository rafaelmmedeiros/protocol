---
id: TD-019
title: The catalogue models every movement; the assumed gym stays lean, and machines arrive by derivation
status: active
supersedes: TD-004
knowledge: [references/exercise-selection-within-a-movement-pattern.md, references/indirect-only-volume-and-the-coverage-floor.md]
decided: 2026-08-24
---

**Decision.** Two things `TD-004` held together are separated here, and only one of them changes.

**The catalogue is no longer scoped to the assumed gym.** It models the movements this product
reasons about, including selectorised machines, whether or not a given user can perform them. What
a user may be prescribed is decided by their equipment set at draw time (`ADR-013`), not by what
the seed contains.

**The assumed gym is unchanged.** A user who has neither described their gym nor synced any history
is still programmed for a barbell with plates, a rack, an adjustable bench, dumbbells, an adjustable
cable station with a lat pulldown, and a pull-up bar. **No selectorised machines.**

**Machines reach a user by derivation or by description, never by assumption.** `ADR-020` already
suggests equipment from logged training and asks the user to confirm it. Until now that path could
suggest no machine at all, for a circular reason: a logged machine exercise implied nothing because
no machine existed in the catalogue to carry a requirement. Widening the catalogue breaks the circle
without widening any assumption.

**Why this and not what the engineer first chose.** The instinct was to widen the default, and
`TD-004`'s own argument is what changed it — an argument that survives intact and is worth
restating rather than merely cited:

> Assume rich and be wrong: the user cannot perform the prescription, silently improvises, and the
> logged workout diverges from what was generated. That corrupts the append-only history every later
> analysis stands on, and nothing surfaces it. Assume lean and be wrong: the session is performable
> and merely feels less than ideal. **The first failure is invisible and permanent. The second is
> visible and free.**

Nothing about that reasoning weakened. What changed is that `TD-004`'s own *"when to revisit"*
named the better answer and said to prefer it **when the choice is live** — deriving the equipment
set from history rather than assuming or asking it. `M3` built the derivation; `M4` gives it
something to derive. The choice is live now, so the record's own instruction is followed.

The literature is still indifferent and that is still what makes this safe rather than arbitrary:
machines against free weights is null for whole-muscle growth
(`references/exercise-selection-within-a-movement-pattern.md`). Nothing here claims a machine is
better or worse. It claims only that we do not know what is in a stranger's gym, and that guessing
generously is the expensive direction to be wrong in.

**What it costs.**

- **`knee_flexion` stays open for a user who has neither synced nor described their gym.** That is
  the same hole `TD-004` named in `M1`, unchanged, and it is now the *only* population it affects.
  For that user it is honest: we genuinely do not know whether they have a leg curl.
- **The default is now further from the observed reality than it was.** The one real account this
  project has read trains machines constantly — 3,798 logged exercises outside the old catalogue,
  led by seated leg curl 162 times. Keeping the default lean means the product's out-of-the-box
  week is not what its only user actually trains. That is accepted deliberately: **n=1 is evidence
  about one gym, not about gyms.**
- **A user who never syncs and never opens the equipment screen gets less than they could have.**
  The path from history to machines is one sync and one tap, and the product has to make that path
  obvious rather than assume its way past it.
- **The seed no longer proves performability.** Under `TD-004` every catalogue row was performable
  in the assumed gym, so a seeded row could not be undrawable. That is no longer true, and the
  guarantee moves to the equipment filter — which is where `ADR-013` always intended it.

**How it shows up in code.**

- `ExerciseCatalogue.AssumedGym` is **unchanged**. Any commit that adds a machine to it contradicts
  this record.
- `ExerciseCatalogue.All` grows past `AssumedGym`, so any test asserting every catalogue row is
  performable in the assumed gym asserts something this record withdrew, and is rewritten rather
  than deleted quietly.
- Nothing new branches. The generator already draws only what a user's equipment set covers; the
  behaviour change is entirely in what the seed contains.

**When to revisit.**

- **A second real account.** The default rests on an assumption about gyms, and the only evidence
  this project has is one of them. A handful of real equipment sets would replace an assumption
  with a distribution — and would be the first time this question had data rather than reasoning.
- **The derivation path proving weak in practice.** If users sync and still do not end up with the
  machines they own, the failure is in the suggestion flow rather than in the default, and this
  record is the wrong place to fix it — but it is where someone will look first, so it is named
  here.
- **A user reports a prescription they cannot perform.** Still the loud failure `TD-004` chose, and
  still the signal working rather than a bug.
