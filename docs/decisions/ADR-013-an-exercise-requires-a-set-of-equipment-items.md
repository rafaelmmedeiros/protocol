---
id: ADR-013
title: An exercise requires a set of equipment items, and an item is as specific as a machine
status: active
binds: [backend, frontend]
supersedes: ADR-010
decided: 2026-08-23
---

**Context.** `ADR-010` modelled availability as a per-user set over the `Equipment` enum the
catalogue already carried, filtering exercises whose single `equipment` value was in the set.
It was decided and not implemented, which is the cheapest moment to find it wrong.

It is wrong in two ways, both raised by the engineer against a real gym.

**An exercise needs several things at once, not one.** `Bench Press (Barbell)` carries
`equipment: Barbell` and also needs a bench. Someone with a barbell, plates and a rack but no
bench cannot perform it, and `ADR-010`'s filter says they can. The single-valued column comes
from `TD-005`, where its job is to *discriminate variants* — a barbell curl from a dumbbell
curl — and that is a different question from what a movement requires. `ADR-010` treated one
answer as if it settled both.

**A machine is an individual object, not a category.** `Iso-Lateral Chest Press` — which this
account has actually logged — is one specific machine that happens to train the same movement
pattern as a barbell bench press. `Equipment.Machine` cannot tell it from a leg extension, so a
user who owns a hack squat and nothing else is either offered every machine exercise or none.

The gyms this has to describe are concrete: a commercial gym with most things; a home gym with
one barbell, one bench and one hack squat, which still performs a great deal.

**Options.**

### A — A per-user set over the existing `Equipment` enum
- `ADR-010`'s decision.
- **Pros:** No catalogue change. Ten checkboxes. A one-column filter.
- **Cons:** Cannot express that an exercise needs two things, so it offers movements the user
  cannot perform — the silent failure `TD-004` chose its assumption specifically to avoid.
  Cannot distinguish one machine from another.

### B — A requirement set per exercise, matched against a set of items the user owns
- `exercise_requirements(exercise, item)`; an exercise is performable when its requirements are
  a subset of what the user has. `EquipmentItem` is granular enough to name an individual
  machine — `FlatBench`, `SquatRack`, `CableStation`, `HackSquatMachine`, `LegCurlMachine` —
  rather than a class of them.
- **Pros:** Says what is actually true, and is the only option that answers the home-gym case
  correctly. `equipment` on the exercise keeps its real job (discriminating variants for
  `preference_rank`) instead of doing two jobs badly. Adding a machine later is a row, not a
  schema change.
- **Cons:** Every catalogue row needs its requirements curated — 36 today, and it grows with
  the catalogue. The setup screen has more to say than ten checkboxes, and needs grouping to
  stay usable.

### C — B plus loadable ranges: which plates, which dumbbells, which bar lengths
- The engineer raised these directly.
- **Pros:** The only model that can eventually answer "can this user actually make 47.5 kg",
  which a load prescription will need.
- **Cons:** Nothing prescribes load. `M1` prescribes sets, repetitions, proximity to failure
  and rest and says nothing about weight, so every field beyond the set would be written and
  never read. It is the right model for the milestone that prescribes load, and speculative
  before it — the same reasoning by which `S1.9` declined a `weight_kg` column.

### D — The user tags exercises they can do
- **Pros:** No model at all.
- **Cons:** Pushes the derivation onto the person for every row, forever, and gets worse as the
  catalogue grows. `ADR-010` already rejected per-exercise availability for this reason.

**Recommendation.** B — it is the smallest model that stops offering unperformable exercises,
and the granularity is set by what a person can actually answer about their gym.

**Decision.** B

**Divergence.** `ADR-010` chose A. It was not wrong about wanting a cheap filter; it was wrong
that one column could express a requirement. Nothing was built on it, so the cost of the change
is the curation of 36 rows and no migration over existing data.

**Notes.** Loadable ranges are deliberately out, and the boundary is worth stating: this record
answers **"can this person perform this movement at all"**, not **"what weight can they make"**.
The second question arrives with load prescription, and option C is where to return.

The default for a new user stays `TD-004`'s assumed gym, expressed as items, so a user who never
opens the screen still gets `M1`'s week — which the milestone asserts byte for byte.

Curating requirements is real, recurring work, and it is the same curation `TD-015` already
accepted for the muscle map: no exercise API supplies it, because Hevy's `equipment` field is
one coarse value per template and collapses cable, Smith and selectorised into `machine`.

**Revisions.**
- _(none)_
