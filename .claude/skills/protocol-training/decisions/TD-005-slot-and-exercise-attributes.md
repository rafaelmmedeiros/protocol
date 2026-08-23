---
id: TD-005
title: What a slot is, and the attributes an exercise must carry for selection to be possible
status: active
knowledge: [references/exercise-selection-within-a-movement-pattern.md, references/muscle-length-and-exercise-variant.md, references/per-muscle-training-frequency.md]
decided: 2026-08-23
---

**Decision.**

**A slot is a position in a session holding one exercise and the prescription attached to it.**
Concretely: an ordinal position within a session, an exercise drawn from the catalogue, and the
sets, repetition range, proximity to failure and rest that `S1.4` prescribes into it. A slot is
not a muscle group and not a movement pattern — it is the pairing of one chosen exercise with
one prescription, and it is the unit `S1.4` prescribes into and `S1.5` cuts.

**Selection fills slots by arithmetic, not by judgement.** The question the generator asks is
never "does this session need an isolation exercise" but "does every modelled muscle group reach
its weekly fractional target" (`TD-006`). Exercises are drawn from a small fixed catalogue in
`preference_rank` order; the same profile always produces the same exercises (`ADR-005`), and no
rotation is applied.

**The attributes.** `ADR-002` already establishes that the catalogue is ours and carries the
attributes selection needs; this record fixes what they are and what values they take.

| Attribute | Kind | Values |
|---|---|---|
| `movement_pattern` | enum | see below |
| `mechanic` | enum | `compound`, `isolation` |
| musculature | relation | `(exercise, muscle_group, role)`, `role` in `primary`, `secondary` |
| `equipment` | enum, single-valued | `barbell`, `dumbbell`, `machine`, `cable`, `smith_machine`, `bodyweight`, `bodyweight_loadable`, `band`, `kettlebell`, `other` |
| `order_class` | enum | `compound_primary`, `compound_secondary`, `isolation` |
| `laterality` | enum | `bilateral`, `unilateral` |
| `preference_rank` | integer | tie-break within `(movement_pattern, equipment)` |

`movement_pattern`, 20 values — deliberately finer than squat/hinge/push/pull, because a coarse
taxonomy cannot express that a lateral raise and an overhead press are different slots, which is
the single most important thing selection needs from this field:

- *Lower:* `squat`, `hinge`, `lunge`, `knee_extension`, `knee_flexion`, `hip_extension`,
  `hip_abduction`, `calf_raise`
- *Upper push:* `horizontal_push`, `vertical_push`, `horizontal_adduction`, `lateral_raise`,
  `elbow_extension`
- *Upper pull:* `horizontal_pull`, `vertical_pull`, `horizontal_abduction`, `elbow_flexion`,
  `shrug`
- *Trunk:* `trunk_flexion`, `anti_extension`, `anti_rotation`

`muscle_group`, 16 values: `chest`, `front_delts`, `side_delts`, `rear_delts`, `lats`,
`upper_back`, `biceps`, `triceps`, `forearms`, `quads`, `hamstrings`, `glutes`, `calves`, `abs`,
`spinal_erectors`, `adductors`.

Four choices inside that table are judgements rather than transcriptions, and each is here to be
argued with rather than silently inherited:

1. **The deltoid is three groups, not one.** A push day delivers large indirect front-delt
   volume and near-zero side-delt volume; collapsing to `shoulders` makes the fractional count
   wrong in exactly the direction the selection note documents, and the error is invisible. Note
   that this rests on mechanism and EMG, not on a training trial — the selection note is
   explicit that no trial tests it.
2. **`role` is an enum, not a float.** The 0.5 weight is a training judgement (`TD-006`) and
   belongs in one constant citing one record, not scattered across catalogue rows where it
   cannot be revised or audited.
3. **`equipment` is single-valued, and barbell bench press and dumbbell bench press are two
   rows.** This keeps `M2`'s filter trivial and keeps the muscle map honest, since the two are
   not identical maps.
4. **`equipment` is populated in `M1` even though nothing filters on it** (`TD-004`).

**Deliberately omitted, with the reason**, because an absent column is invisible and a future
session will otherwise add these as oversights:

- **`carry`** as a movement pattern — no hypertrophy role in this corpus, and it does not accept
  a sets/reps/RIR prescription cleanly, so `S1.4` would have to special-case it.
- **`lengthened_position` / `rom_bias`** — `references/muscle-length-and-exercise-variant.md` is
  contested and the effect is plausibly ES 0.04-0.09. A column invites a branch. The tie-break
  it would serve lives in `preference_rank` instead, as catalogue ordering, revisable without
  touching the generator.
- **`stability_demand`** and **`fatigue_cost`** — no evidence supports a value. Both would be
  invented and then branched on, and `fatigue_cost` is worse: a number with no source that a
  later session would treat as measured. `order_class` already carries what ordering needs.
- **`difficulty` / `skill_level`** — `TD-001` refuses training status; a skill attribute would
  reintroduce it through the catalogue's back door.

**Why this and not what the literature would suggest.** The literature does not specify a
schema, and on selection itself it returns nulls: compound versus isolation, free weights versus
machines, unilateral versus bilateral, and varied versus fixed exercises are all
indistinguishable for whole-muscle growth once volume is equated. That is what licenses a small
fixed catalogue and a deterministic draw. The schema exists to make volume arithmetic
computable, not to encode a hierarchy of exercises the evidence does not support.

**What it costs.**

- **`secondary` assignment is the soft spot of the entire design.** It must mean "meaningfully
  loaded through a substantial range," not "anything that contracts" — erectors are secondary on
  a squat and a row, not on a leg press. If two sessions tag the catalogue differently, every
  volume number the product produces moves, and unlike a wrong constant it will not be visible
  in a diff. The rule is stated here; the catalogue must be fixed once and changed deliberately.
- **Edge cases in `movement_pattern` and `mechanic` are judgement calls** — deadlift and hip
  thrust (`hinge` versus `hip_extension`), pullover, upright row. Decided once in the catalogue,
  never re-derived.
- **`order_class` is stored rather than derived from `mechanic`**, because the split between
  primary and secondary compounds is exactly the judgement. That is a column that can drift from
  its own definition.
- **A 20-value movement taxonomy is more than `M1` strictly needs** and will look
  over-engineered until the catalogue is large. The alternative silently merges slots.

**How it shows up in code.**

- The `Exercise` entity and the `exercise_muscle` relation in `S1.6`, with our own primary key
  and Hevy's `exercise_template_id` beside it (`ADR-002`, standards 8 and 9).
- `Training/WeekGenerator` selects by `(movement_pattern, muscle_group, order_class)` and breaks
  ties on `preference_rank` — never on title (standard 9), never on insertion order or `id`,
  because `ADR-005` requires the tie-break be auditable.
- `laterality` is stored because a unilateral set costs two sets of *time* (`S1.5`) and because
  Hevy logs per side, so the import mapping and the volume count must agree on what one
  prescribed set means. It is not stored because it affects growth; it does not.

**When to revisit.**

- **`M2` and equipment.** `equipment` becomes a filter; nothing else here should need to move.
- **The catalogue grows past the point where `preference_rank` is maintainable by hand.**
- **A muscle group turns out to be mis-modelled** — most likely candidates are the deltoid split
  (mechanism, not trial) and whether `upper_back` and `lats` are usefully separable in practice.
- **Anything wants to branch on an omitted attribute.** Read the omission list first: the
  omission was a decision, and reversing it is a new record, not an oversight being corrected.
