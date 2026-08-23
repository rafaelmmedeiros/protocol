---
id: ADR-016
title: A pushed routine carries repetitions, a rep range and rest, but no load and no effort
status: active
binds: [backend]
decided: 2026-08-23
---

**Context.** A prescription holds an exercise, a set count, a repetition range (`TD-009`),
repetitions in reserve (`TD-010`) and a rest interval (`TD-011`). Hevy's routine set schema
accepts `type`, `weight_kg`, `reps`, `distance_meters`, `duration_seconds`, `custom_metric` and
`rep_range`; the routine exercise accepts `exercise_template_id`, `rest_seconds`, `notes` and
`superset_id`.

Three of those fields land cleanly: `rep_range` takes `TD-009`'s ranges natively, `rest_seconds`
takes `TD-011`, and `weight_kg` is nullable, which is what `M3` needs because no load is
prescribed until `M4`.

**One does not exist. A routine set has no `rpe` field** — the API carries `rpe` only on a *logged
workout's* set. So `TD-017`'s outbound conversion is correct as a mapping and has nowhere to
write. Proximity to failure is the one prescription variable the user acts on set by set, and it
is the one Hevy will not accept as data. **See the revision below before reading that absence as a
shortcoming.**

**Options for the prescribed reserve.**

### A — One line of text in the routine exercise's `notes`
- The push composes a short sentence per exercise — "3 reps in reserve — stop with 3 good reps
  left" — in the user's language.
- **Pros:** visible in Hevy at the moment it is acted on, which is the only moment it matters.
  Costs one field that already exists. It is unambiguously **display**, never a channel: the same
  experiment that proved `routine_id` populates also proved a routine's exercise notes do **not**
  survive into the workout, so nothing can later be tempted to read this back as data.
- **Cons:** it is a translated, user-visible string composed by the backend — the first one in the
  system — so the push has to carry the user's locale (standard 2). And it is prose in a field a
  user may edit or clear, so it is advisory by nature.

### B — Do not send it
- The routine carries sets, repetitions and rest only.
- **Pros:** the purest boundary. Nothing of ours crosses that Hevy will not model.
- **Cons:** the user trains a week whose prescribed effort they were never told, which empties
  `TD-010` in practice. A prescription the lifter cannot see is not a prescription.

### C — Encode it numerically in `custom_metric`
- Put `10 - RIR` in the spare numeric field.
- **Pros:** machine-readable, and it round-trips.
- **Cons:** exactly the failure standard 17 exists to prevent, in the other direction — smuggling
  our meaning into a field of theirs that means something else. It also surfaces in Hevy's own UI
  as an unexplained number attached to every set.

**Recommendation.** A — the note is the only place the information can reach the user, and the
proven non-propagation of notes makes it safe against being mistaken for data later.

**Decision.** A

**What else the push does and does not send.**

- **Working sets only, as `type: "normal"`.** No `warmup` sets are pushed. `TD-012` budgets time
  for a ramp on the first compound without prescribing it as sets, and the import filters
  `warmup` on the way back (`TD-006`) — sending none keeps the two directions symmetric.
- **`weight_kg` is null.** Not zero, and not a guess. `M3` prescribes no load, the user chooses it
  in the gym, and that choice is the observation `M4` needs (`TD-001` — observe, do not ask).
- **`superset_id` is null.** `TD-013` declined supersets.
- **`exercise_template_id` comes from our catalogue's external key** (`ADR-002`, standard 8). An
  exercise without one cannot be pushed, and that is a loud failure rather than a silent
  substitution.

**Revisions.**

- 2026-08-23 — **The missing field is correct modelling, not a gap, and this record was first
  written as though it were one.** `rpe` is *feedback*: it exists after a set, reported by the
  person who performed it. A routine is a plan, and a plan does not carry an observation. Hevy
  placing the field only on a logged workout is the right shape, and the decision above (A) is
  therefore permanent rather than a workaround waiting for the API to improve.

  This matters because the two framings produce opposite future behaviour. Read as a shortcoming,
  a later session would watch for `rpe` to appear on routine sets and wire the outbound conversion
  into it — at which point a workout would inherit our prescribed number and the import would read
  **our own prescription back as the user's report**. `TD-017` names the distance between
  prescribed and performed reserve as the signal the whole loop exists to produce; that distance
  would silently collapse to zero, and the result would look like perfect adherence.

  **So: never write a prescribed target into a field that means feedback**, in this integration or
  any other, and no future record should treat `rpe`-on-routines as a feature to adopt. The rule
  is about the field's meaning rather than about proximity to failure — prescribing effort is
  itself legitimate and well established (Helms et al. 2018 tested an RPE-prescribed programme
  directly, and `TD-010` prescribes reserve here), which is exactly why the confusion is available
  to be made.

  Nothing about the decision changes. What changes is that option B is now rejected for a second
  and stronger reason, and that `TD-017`'s outbound half has no consumer in this integration by
  design rather than by circumstance.
