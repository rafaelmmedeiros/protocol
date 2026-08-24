---
name: protocol-training
description: "The strength-training domain for this repo: what the literature supports (knowledge notes) and what this product does about it (training decision records). Invoke before writing any code that makes a training judgement — a rep range, a progression step, a volume threshold, a readiness call — and to add to the corpus when a feature asks a question it does not yet answer."
argument-hint: "[<the training question, or a decision to record>]"
---

The domain knowledge home for `protocol`. Everything here exists because a feature needed it;
nothing is here because it is true.

Two kinds of file, kept apart on purpose:

- **`references/<topic>.md` — knowledge notes.** What the literature supports. Sourced, dated,
  bounded. Not ours: it changes when the science changes or when we read more of it.
- **`decisions/TD-###-<slug>.md` — training decision records.** What this product does. Ours,
  often a deliberate simplification of the notes it cites, because complexity has a usability
  cost.

Keeping them apart is what lets a future reader answer the only question that matters later:
*did we do it this way because the evidence says so, or because we chose to?* Mixed into one
file, every simplification reads as ignorance — and a later session will helpfully "fix" a
choice it mistakes for a gap.

The directories appear with their first file. An empty one would be a promise, not a corpus.

## Index

Keep this table current — it is the whole reason invoking this skill is cheap. A session reads
the table, then only the notes its question actually touches.

### Knowledge

| Topic | Confidence | Bearing — does this change what we would build, at our scale? |
|-------|-----------|---------------------------------------------------------------|
| [`training-status`](references/training-status.md) | contested | Confirms `ADR-004`: status has no validated definition, and hypertrophy prescriptions barely differ by level in a first block's range |
| [`cold-start-first-block`](references/cold-start-first-block.md) | thin | Favours a conservative escalating start over a week-one calibration; makes early adherence a generator signal |
| [`per-muscle-training-frequency`](references/per-muscle-training-frequency.md) | settled | Frequency has no detectable effect on hypertrophy once weekly volume is equated — the split is a scheduling decision, not a training one. Requires fractional set counting |
| [`split-templates-by-frequency`](references/split-templates-by-frequency.md) | thin | The frequency-to-split templates are practitioner convention resting on that settled null; pick for schedulability and never claim a split grows more |
| [`exercise-selection-within-a-movement-pattern`](references/exercise-selection-within-a-movement-pattern.md) | contested | Compound vs isolation, machines vs free weights, unilateral vs bilateral and varied vs fixed are all null for whole-muscle growth — selection is arithmetic over fractional volume, not a hierarchy of exercises |
| [`muscle-length-and-exercise-variant`](references/muscle-length-and-exercise-variant.md) | contested | Long-muscle-length training: two meta-analyses disagree, effect small at best. A free tie-break between equivalent exercises, never a selection rule |
| [`exercise-order-within-a-session`](references/exercise-order-within-a-session.md) | contested | Order is null for hypertrophy (ES 0.03); whatever goes first gains most strength at that task. Ordering is a quality-and-safety convention, and a small muscle last costs nothing |
| [`weekly-set-volume-for-hypertrophy`](references/weekly-set-volume-for-hypertrophy.md) | settled | The volume curve is a square root with no plateau — the cheap growth is at the bottom, and above ~12 sets the meta-analyses stop agreeing. Supplies the number `TD-001` refused to pick |
| [`muscle-group-specific-volume-requirements`](references/muscle-group-specific-volume-requirements.md) | thin | Per-muscle volume tables have almost no evidence behind them; no dose-response is stratified by muscle group. A uniform target is what the literature supports |
| [`repetition-range-and-load-for-hypertrophy`](references/repetition-range-and-load-for-hypertrophy.md) | settled | Rep range is free for growth across ~5-30 reps *when sets are near failure*; choose it for RIR accuracy and joint demand. The conditional matters — our first block is not near failure |
| [`proximity-to-failure-and-hypertrophy`](references/proximity-to-failure-and-hypertrophy.md) | contested | Growth rises as sets approach failure, but reaching failure adds nothing detectable (ES 0.15-0.19) and costs 24h of fatigue. Varying RIR by exercise type is untested |
| [`inter-set-rest-and-hypertrophy`](references/inter-set-rest-and-hypertrophy.md) | contested | Rest is near-free for growth; what it buys is repetitions, and the whole effect is in the step from 1 to 2 minutes. A 60-second slot is not supported |
| [`session-time-cost-of-a-set`](references/session-time-cost-of-a-set.md) | thin | The minutes-to-slots arithmetic, calibrated against two trials that measured session duration. **Rest is 74-79% of a session's clock** |
| [`warm-up-cost-before-resistance-training`](references/warm-up-cost-before-resistance-training.md) | contested | General warm-up can be omitted and ramping restricted to the first heavy compound, for ~3 min/session. The trials measured repetitions, never injury |
| [`cutting-training-volume-under-a-time-constraint`](references/cutting-training-volume-under-a-time-constraint.md) | thin | Each lever has a source; the ordering between them has none. Shorter sessions do **not** protect adherence — the association runs the other way |
| [`exercise-variant-and-implementation`](references/exercise-variant-and-implementation.md) | contested | Four within-movement trials null for growth, one with BF<0.01 in trained lifters. "Constant tension" was tested directly and did not win. The muscle map changes by variant, so it cannot live on a parent |
| [`load-increment-granularity-and-progression`](references/load-increment-granularity-and-progression.md) | thin | The smallest load step varies 10x and breaks ACSM's 2-10% rule on light isolation work. Rep progression is non-inferior. **Load does not transfer across variants; hypertrophy does** |
| [`ranking-exercise-variants`](references/ranking-exercise-variants.md) | thin | A general variant ranking survives only as performability, never growth. Personal fit is an adherence finding, and Damas 2019 puts the variance in the person, not the person-by-exercise pairing |
| [`self-selected-exercise-and-autonomy`](references/self-selected-exercise-and-autonomy.md) | thin | Autonomy reaches device-measured behaviour (d=0.29) but the two resistance-training choice trials moved autonomy and **not** enjoyment. Self-selected *load* has a measured price: 53% of 1RM |
| [`indirect-only-volume-and-the-coverage-floor`](references/indirect-only-volume-and-the-coverage-floor.md) | contested | Starving a muscle is cheap and contested where compounds cover it, expensive where nothing does. That asymmetry, not a threshold, says which exclusions cost something |
| [`progression-and-periodization-for-hypertrophy`](references/progression-and-periodization-for-hypertrophy.md) | contested | **Do not build periodization** — null across 4 reviews, and every positive result is a strength result. But progression itself got stronger: a within-subject trial roughly doubled growth. Stalls and deloads are convention, not findings |
| [`volume-progression-across-a-block`](references/volume-progression-across-a-block.md) | contested | Adding sets failed in two trials that both started above 20 weekly sets. At `TD-014`'s 6.0 we are far below that, on the steepest part of the curve — defensible here, indefensible at 22 |
| [`inferring-proximity-to-failure-from-logged-sets`](references/inferring-proximity-to-failure-from-logged-sets.md) | thin | RIR **cannot** be recovered from weight and reps; device-free inference is off by 3-6 reps. Self-report is accurate to ~1 rep, and experience does not affect it. Asking beats inferring |
| [`graded-versus-uniform-proximity-to-failure`](references/graded-versus-uniform-proximity-to-failure.md) | thin | No graded-versus-uniform trial exists and none is expected — but the gradient's rationale collapsed: RIR accuracy does **not** vary by exercise type and is *better* under heavy load, and ACSM 2026 prescribes one uniform 2-3 target |
| [`progression-trigger-under-constant-effort-execution`](references/progression-trigger-under-constant-effort-execution.md) | thin | Nobody names the set the double-progression rule reads — ACSM is silent, NSCA says the **last** set. At constant RIR reps fall 25-33% across sets even at 3 RIR with 4 min rest, so a last-set trigger never fires and never says so |
| [`separating-execution-modes-from-a-bare-log`](references/separating-execution-modes-from-a-bare-log.md) | thin | **Impossible, not merely hard.** A fixed rep target censors the observation, and external load maps one-to-many onto internal load by the field's own framework. Kills the flat-sequence flag outright; argues for a range terminated on effort, and for a first-set trigger that fails safe under both modes |
| [`muscle-specific-repetition-drop-off-and-fibre-type`](references/muscle-specific-repetition-drop-off-and-fibre-type.md) | thin | No per-muscle drop-off table exists and the mechanism runs backwards — chest and back are indistinguishable in fibre type, and within-muscle between-person variance is ~3x the between-muscle spread. Session position alone moves an exercise's reps ~25%. Any baseline must key on the **slot**, never the muscle |
| [`grip-and-forearm-involvement-in-elbow-flexion`](references/grip-and-forearm-involvement-in-elbow-flexion.md) | contested | The brachioradialis is loaded in a supinated curl too, and in two of three studies **more** than in a neutral one. Grip cannot separate a hammer curl from a bicep curl in a muscle map — and EMG is not growth anyway |
| [`rir-based-rpe-scale-anchors`](references/rir-based-rpe-scale-anchors.md) | contested | Hevy's RPE **is** the RIR-based scale, so it reads directly as reps in reserve — but `RIR = 10 - RPE` is exact only at 7/8/9/10. Half points are intervals, never half a rep, and Hevy's own labels disagree with the published table at 9.5 and 6 |

### Decisions

| ID | Decision | Status | Rests on |
|----|----------|--------|----------|
| [`TD-001`](decisions/TD-001-cold-start-assumes-nothing.md) | No status inferred or asked, no week-one calibration, everyone starts in the lower half of the effective volume range | active | `training-status` (contested), `cold-start-first-block` (thin) |
| [`TD-002`](decisions/TD-002-supported-training-frequencies.md) | 2-6 training days a week supported; 1 and 7 rejected with `FrequencyOutOfRange` — 1 on evidence, 7 as a product bound | active | `per-muscle-training-frequency` (settled), `split-templates-by-frequency` (thin), `cold-start-first-block` (thin) |
| [`TD-003`](decisions/TD-003-frequency-to-split-mapping.md) | One split template per frequency, Monday-anchored and weekly-repeating, chosen for schedulability rather than growth — **superseded by `TD-023`**, which turns the mapping into a set with a default and replaces the week with a cycle | superseded-by TD-023 | `split-templates-by-frequency` (thin), `per-muscle-training-frequency` (settled) |
| [`TD-004`](decisions/TD-004-assumed-gym.md) | `M1` assumes a barbell-and-cable commercial gym with no selectorised machines — **superseded by `TD-019`**, which keeps the assumption and unscopes the catalogue from it | superseded-by TD-019 | `exercise-selection-within-a-movement-pattern` (contested) |
| [`TD-005`](decisions/TD-005-slot-and-exercise-attributes.md) | What a slot is, and the exercise attributes selection needs — including what is deliberately omitted and why | active | `exercise-selection-within-a-movement-pattern` (contested), `muscle-length-and-exercise-variant` (contested) |
| [`TD-006`](decisions/TD-006-indirect-sets-count-half.md) | An indirect set counts 0.5 toward a muscle's weekly volume, as one named constant | active | `per-muscle-training-frequency` (settled), `exercise-selection-within-a-movement-pattern` (contested) |
| [`TD-007`](decisions/TD-007-within-session-ordering.md) | Order by `order_class` then `preference_rank`; pre-exhaustion rejected; a small muscle last is allowed and not a benefit | active | `exercise-order-within-a-session` (contested) |
| [`TD-008`](decisions/TD-008-weekly-volume-target.md) | 8.0 fractional sets per muscle per week — **target superseded by `TD-014`**; its floor 4.0, cap 11.0 and 3 sets per slot still stand | superseded-by TD-014 | `weekly-set-volume-for-hypertrophy` (settled), `muscle-group-specific-volume-requirements` (thin), `cold-start-first-block` (thin) |
| [`TD-009`](decisions/TD-009-repetition-ranges-per-slot.md) | Reps by `order_class`: 6-10 / 8-12 / 10-15; nothing above 15 while RIR stays conservative | active | `repetition-range-and-load-for-hypertrophy` (settled), `proximity-to-failure-and-hypertrophy` (contested) |
| [`TD-010`](decisions/TD-010-proximity-to-failure.md) | RIR 3 / 2 / 2 by `order_class` — **superseded by `TD-018`**; its "never to failure, never 0 RIR" still stands | superseded-by TD-018 | `proximity-to-failure-and-hypertrophy` (contested), `cold-start-first-block` (thin) |
| [`TD-011`](decisions/TD-011-rest-per-slot.md) | Rest per slot: 180 / 150 / 90 seconds, hard floor 90; rest is cut before a set is | active | `inter-set-rest-and-hypertrophy` (contested), `weekly-set-volume-for-hypertrophy` (settled) |
| [`TD-012`](decisions/TD-012-minutes-to-slots.md) | A slot costs 7.5 min; sessions run 25-120 min (`DurationOutOfRange`); no general warm-up, ramping on the first compound only | active | `session-time-cost-of-a-set` (thin), `warm-up-cost-before-resistance-training` (contested) |
| [`TD-013`](decisions/TD-013-cut-ordering.md) | Cut ladder: rest → consolidate → sets → drop slots → refuse. Frequency is never cut; supersets declined for `M1` | active | `cutting-training-volume-under-a-time-constraint` (thin), `session-time-cost-of-a-set` (thin) |
| [`TD-014`](decisions/TD-014-weekly-volume-target-revised.md) | **6.0** fractional sets per muscle per week, superseding `TD-008`'s 8.0 — a target every supported configuration can actually reach | active | `weekly-set-volume-for-hypertrophy` (settled), `session-time-cost-of-a-set` (thin), `cold-start-first-block` (thin) |
| [`TD-015`](decisions/TD-015-catalogue-stays-flat.md) | Catalogue stays flat — a variant is a row, not a child. `preference_rank` claims performability, never growth. `load_increment_kg` deferred with its reason | active | `exercise-variant-and-implementation` (contested), `ranking-exercise-variants` (thin), `load-increment-granularity-and-progression` (thin) |
| [`TD-016`](decisions/TD-016-what-a-preference-may-override.md) | A preference filters and reorders the draw pool and never touches the volume arithmetic. An exclusion is honoured unconditionally and the shortfall is surfaced per muscle. No threshold on how much may be excluded. **One sentence in it is false and cannot be edited out** — it calls slot count `TD-012`'s minutes arithmetic, when slot count is the volume arithmetic bounded by the clock (`TD-021`); the decision itself is unaffected | active | `self-selected-exercise-and-autonomy` (thin), `indirect-only-volume-and-the-coverage-floor` (contested), `ranking-exercise-variants` (thin) |
| [`TD-017`](decisions/TD-017-rpe-to-rir-mapping.md) | RPE is Hevy's, RIR is ours. Inbound `RIR = 10 - ceil(RPE)` — discard the "maybe", resolving toward less reserve; outbound `RPE = 10 - RIR`, exact. The domain represents no uncertainty about effort: no fractional RIR, no interval | active | `rir-based-rpe-scale-anchors` (contested), `inferring-proximity-to-failure-from-logged-sets` (thin), `proximity-to-failure-and-hypertrophy` (contested) |
| [`TD-020`](decisions/TD-020-grip-does-not-change-what-a-curl-trains.md) | Grip stays out of the model: `Forearms` is secondary on every curl, whatever the hand does. The forearm gets a wrist curl instead — a joint action no curl reaches — and starts competing for slots | active | `grip-and-forearm-involvement-in-elbow-flexion` (contested), `exercise-variant-and-implementation` (contested) |
| [`TD-018`](decisions/TD-018-uniform-proximity-to-failure.md) | **2 repetitions in reserve for every exercise**, superseding `TD-010`'s 3/2/2 — the gradient's accuracy argument runs backwards and ACSM 2026 is uniform. Still never to failure, never 0 RIR | active | `graded-versus-uniform-proximity-to-failure` (thin), `proximity-to-failure-and-hypertrophy` (contested), `progression-trigger-under-constant-effort-execution` (thin) |
| [`TD-023`](decisions/TD-023-a-frequency-offers-a-set-of-templates.md) | Each frequency offers a **set** of templates with `TD-003`'s answer as the default; nine templates, every one measured to give every muscle group at least 2x per cycle. `Upper/Lower` at two sessions fails at 1x and is not offered. A template repeats over a **cycle**, not a week (`ADR-027`) | active | `split-templates-by-frequency` (thin), `per-muscle-training-frequency` (settled) |
| [`TD-024`](decisions/TD-024-the-dose-window-is-the-cycle.md) | The target, ceiling and floor keep their values and attach to a **cycle** instead of a week. A cycle holds exactly the declared frequency's sessions (`TD-023`), so it *is* the user's declared week; the Monday week stays the measurement window (standard 6). A rolling window was rejected because it would make generation depend on future pace, breaking `ADR-005` | active | `weekly-set-volume-for-hypertrophy` (settled), `per-muscle-training-frequency` (settled), `cold-start-first-block` (thin) |
| [`TD-021`](decisions/TD-021-session-duration-buys-volume-up-to-a-ceiling.md) | The weekly target becomes a band: **6.0** guaranteed, **8.0** where the minutes exist — **its phase-2 slot size superseded by `TD-022`**, because a three-set slot overshot the ceiling it was meant to land on. The band, its edges and the two-phase fill all still stand | superseded-by TD-022 | `weekly-set-volume-for-hypertrophy` (settled), `volume-progression-across-a-block` (contested), `cold-start-first-block` (thin), `session-time-cost-of-a-set` (thin) |
| [`TD-022`](decisions/TD-022-a-slot-bought-above-the-target-carries-two-sets.md) | A slot drawn above the guaranteed target carries **2 sets**, so 6.0 + 2.0 lands exactly on the ceiling instead of 1.0 past it. Two sets now means the opposite of what it means in `TD-013`, and the constants stay separate for that reason | active | `weekly-set-volume-for-hypertrophy` (settled), `volume-progression-across-a-block` (contested), `cold-start-first-block` (thin), `session-time-cost-of-a-set` (thin) |
| [`TD-019`](decisions/TD-019-the-catalogue-is-not-scoped-to-the-assumed-gym.md) | The catalogue models every movement including machines; the **assumed gym is unchanged and stays lean**, and machines reach a user by derivation or description, never by assumption | active | `exercise-selection-within-a-movement-pattern` (contested), `indirect-only-volume-and-the-coverage-floor` (contested) |

## Consulting

1. Read the index. If a note covers the question, read it — starting with **what it does not
   settle**, which is where an overreaching claim gets caught.
2. If a decision already covers it, follow the decision, not the note. The note is the input;
   the decision is the ruling.
3. If neither exists, the question is unanswered: research it (below) before writing the code
   that assumes an answer.

Never fill a gap from memory. A number recalled and shipped is indistinguishable in the source
from a number that was researched — that is exactly the failure this corpus exists to prevent.

## Adding a knowledge note

The `training-researcher` agent does this work: it reads sources in its own context and returns
a draft, so the feature session gets the note rather than the reading. Review the draft before
it lands — what enters the corpus is a decision, not a fetch.

```markdown
---
topic: <slug>
confidence: settled | contested | thin
bearing: <one line — does this change anything we would build, at our scale?>
sources:
  - <citation, with a link that resolves>
last-reviewed: <YYYY-MM-DD>
---

**What is claimed.**

**What the evidence actually shows.** Effect size, population, and how far it generalises.

**What it does not settle.** The boundary of the claim.

**Where it touches the product.** Which decisions depend on it.
```

The gates, and none of them is optional:

- **Sources that resolve.** Recalled knowledge does not open a note. The literature and reviews
  of it, fetched.
- **An honest confidence tier.** `settled` is broad agreement across meta-analyses; `contested`
  is competent people disagreeing, and we will have to choose anyway; `thin` is mechanism or
  practitioner consensus with no direct evidence. `thin` is writable — unmarked `thin` is not.
- **A stated boundary.** A claim with no "what it does not settle" is unfinished.
- **An honest bearing.** A note whose bearing is "real, and too small to matter for us" is a
  useful note: it closes the question and stops it being reopened. Not all of the science
  applies, and saying so is the note's job.

## Recording a decision

```markdown
---
id: TD-###
title: <what was decided>
status: active | superseded-by TD-###
knowledge: [references/<topic>.md, ...]
decided: <YYYY-MM-DD>
---

**Decision.**

**Why this and not what the literature would suggest.** Named explicitly when they differ.

**What it costs.** The accuracy or generality given up for usability.

**How it shows up in code.** The rule, threshold or number, and where it lives.

**When to revisit.** The signal that would reopen it.
```

- **A decision may rest on `contested` or `thin` knowledge — and must say which.** Waiting for
  settled evidence on a contested question is not neutrality; it is shipping nothing.
- **Records are append-only. A decision changes by a new record superseding the old, never by
  editing it.** Root standard 7 forces this: programs this system generated were generated
  under the decision in force at the time, and the history recording them is append-only. A
  record edited in place makes every program produced under its earlier version unexplainable.
  Set the old record's `status` to `superseded-by`, and leave everything else in it untouched.
- **Every training judgement in code cites its record** — root standard 15. The comment is the
  link; there is no second index to keep in sync.
- **A number with checkable arithmetic is measured before the record is committed, and the
  record says what was measured.** Not after, and not by reading the generator — by running it
  across the supported grid and reading the result. `TD-021` declared a band of 6.0 to 8.0
  fractional sets and was superseded within the hour: a slot credits 3.0 to its primary muscle,
  so the band was narrower than one slot and no muscle could finish inside it. That was
  derivable from constants the record itself cited, and three further readings of it failed in
  implementation afterwards. The measurement takes about two minutes. **Records are append-only,
  so a falsified one is not corrected but outlived** — it stays in the corpus permanently,
  asserting something no generated week ever satisfied.

## Growth

One question at a time, and only a question a feature is actually asking. The trainer needs to
pick a scheme, so that question gets researched, one or two notes get written, one record
decides — and nothing else in exercise science enters that session.

The corpus is a sediment of features built. Its shape at any moment is a map of what the
product has actually had to reason about, and that is the correct shape for it to have.
