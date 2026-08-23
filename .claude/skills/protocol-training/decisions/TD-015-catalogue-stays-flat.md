---
id: TD-015
title: The catalogue stays flat — a variant is a row, not a child — and preference_rank claims performability, never growth
status: active
knowledge: [references/exercise-variant-and-implementation.md, references/ranking-exercise-variants.md, references/load-increment-granularity-and-progression.md]
decided: 2026-08-23
---

**Decision.**

Three rulings, from one question: should "Preacher Curl" be a movement with barbell, dumbbell and
machine as children?

**1. No. The catalogue stays flat.** One row per performable exercise, as `TD-005` already
specifies. `Preacher Curl (Barbell)`, `Preacher Curl (Dumbbell)` and `Preacher Curl (Machine)` are
three rows. `TD-005`'s clauses are reaffirmed, not superseded.

**2. `preference_rank` may claim performability, never growth.** The permitted wording, fixed here
so it survives paraphrase:

> The catalogue prefers this variant because it is **performable and progressible in the assumed
> gym**, not because it produces more muscle. On growth, the catalogue asserts nothing.

Forbidden: "best exercise for X", "more effective for X", any ordering presented as a growth
ranking. Permitted: "our default choice for this slot". `TD-003` forbids the same claim for splits
and `TD-007` for ordering; this is the third instance of one rule.

**3. `load_increment_kg` is a documented omission, not a column — for now.** `M1` prescribes no
load at all, so nothing consumes it. Under `TD-005`'s own discipline an absent column must be
explained, and this is the explanation.

**Why this and not what the engineer proposed.** The proposal was good and the reasoning behind it
is sound — the same movement really is applied through barbell, cable, dumbbell and machine, and
those really do differ. What the research changes is *what they differ in*.

**The mechanism has a near-exact trial, and it goes the other way.** Nunes et al. (2020) compared
cable against barbell **preacher curl** — same movement, same bench, one holding tension where the
other unloads. Biceps thickness 7% versus 8%, p=0.346. The only significant difference favoured
the **barbell**, at the extended-elbow angle it loads hardest (+39% vs +30% peak torque at 20
degrees, p=0.046). Larsen et al. (2025) then reproduced the null in lifters averaging 7.1 years of
training, with a Bayes factor **below 0.01** — "extreme" evidence *for* no difference. Attarieh et
al. (2025) matched resistance profile between two variants and found nothing left.

**But the argument that actually decides it is structural, not evidential:**

- **The "movement" level already exists in the schema — as `movement_pattern`.** Barbell curl,
  cable curl and preacher curl already group: all carry `elbow_flexion`. `TD-005` already draws
  within `(movement_pattern, equipment)`. Materialising that grouping as a row adds a table without
  adding information.
- **The parent cannot carry the attribute that would justify it.** The point of a parent is
  inheritance, and the attribute one would most want to inherit is the muscle map — but the map
  demonstrably changes by variant. Chaves et al. (2020) found incline pressing gained 0.62 cm more
  upper-pectoralis thickness than flat (p=0.003); `TD-005` already asserts barbell and dumbbell
  bench are not identical maps. `order_class` fails the same way: a back squat and a goblet squat
  are not the same class.
- **Count the attributes: eight of eleven would live on the variant.** The parent would hold
  `movement_pattern` and `mechanic` — and `movement_pattern` is already a column. That is two
  tables, a join, and a two-place consistency burden to normalise one enum.
- **Hevy removes the last argument.** A live probe of Hevy's catalogue found the variant encoded
  *only in the title string* (which standard 9 forbids parsing), cable and Smith and selectorised
  all collapsed into `equipment: "machine"`, all three deltoid heads collapsed into `shoulders`,
  and `secondary_muscle_groups` empty on most isolation templates. **Every attribute is curated by
  hand regardless of shape**, so a parent saves retyping one enum and costs keeping two tables
  consistent on `secondary` — the assignment `TD-005` already names the design's soft spot.

**What it costs.**

- **The engineer's own gym case is not closed, and this record should not pretend it is.** His
  comparison is a *selectorised preacher machine* against a barbell, and **no trial found compares
  those two.** The nearest evidence is the cable contrast (null) and Haugen's pooled machine null.
  Every mechanism that can be named for the preference has been tested and returned nothing for
  size; the specific comparison has not been run. That is weaker than "disproven" and the
  distinction is kept deliberately.
- **Regional hypertrophy is left on the table.** Chaves and Kassiano both found real regional
  differences by variant. A flat catalogue with a mixed selection probably captures this
  incidentally, but nothing here verifies that, and the product makes no claim about it.
- **Grouping alternatives is deferred, and something will eventually want it.** Substitution
  ("give me another exercise for this slot") and history import both need to know which rows are
  alternatives. When that arrives the answer is a nullable `movement_group` **tag on the flat row**
  — not a foreign key to a table with attributes — which is a one-line migration then (standard 10
  is forward-only, so this costs nothing to defer). Adding it now would be speculative.
- **Deferring `load_increment_kg` means re-curating later.** The values are known and objective
  (barbell 2.5, dumbbell 4.0, cable 5.0, bodyweight null), so the cost is a migration plus a seed
  pass, not new judgement.
- **A two-factor rank is declined for now, and the engineer wanted it.** What survives is a single
  curated order. Personal fit is real but belongs later and derived, not now and asked.

**How it shows up in code.**

- `S1.6` seeds one flat `Exercise` table exactly as `TD-005` and `ADR-002` specify. No `Movement`
  entity, no parent table.
- `preference_rank` remains an integer draw order within `(movement_pattern, equipment)`, with a
  comment citing `TD-015` for what it may and may not claim.
- No `load_increment_kg`, no `resistance_profile`, no `stability_demand`, no `movement_group`. Each
  is a decision, not an oversight — a reader adding one should arrive here first.
- Any user-visible string describing why an exercise was chosen must satisfy the permitted wording
  above. The frontend owns the sentence (root standard 3), so the constraint is on the dictionary,
  not on the API.

**When to revisit.**

- **Substitution or history import lands.** That is what genuinely needs "these rows are
  alternatives", and `movement_group` is the answer — as a tag, not a table.
- **Progression lands.** `load_increment_kg` acquires a consumer, and
  `references/load-increment-granularity-and-progression.md` carries the scheme it should use:
  double progression on ACSM's 2-10% rule, with repetition progression where the increment exceeds
  the band, and **no load carried across variants**. Note first whether the problem tracks
  `order_class` — the coarse-increment cases are almost all `isolation` — in which case no column
  is needed at all.
- **A trial compares a selectorised machine against a free-weight variant of the same movement.**
  That is the specific gap, and it is the engineer's case.
- **Regional hypertrophy acquires a practical meaning**, which would make variant selection a
  training decision rather than an ergonomic one, and would reopen all three rulings.
