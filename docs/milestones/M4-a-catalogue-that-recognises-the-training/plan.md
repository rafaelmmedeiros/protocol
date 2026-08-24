# M4 — A catalogue that recognises the training

## Objective

The generator stops programming a quarter of the week. `M3`'s first import measured the gap: of
5,186 logged exercises, **3,798 are movements this catalogue does not model** — 126 distinct ones,
dominated by the selectorised machines `TD-004` excluded by assumption. This milestone widens the
catalogue from what was actually logged, gives equipment a vocabulary specific enough to say which
machines a gym has, fixes what an imported load means so both sides of the loop count volume the
same way, and reports the remaining gap as a number rather than a feeling. It also adds the one
development affordance the iteration needs: erasing everything of one user's, so the loop can be
run from the start without anyone opening `psql`.

## Capabilities

Verbatim from `docs/ROADMAP.md`:

- Widen the exercise catalogue from what has actually been logged, ordered by how often each
  movement is trained
- Name equipment at the granularity an individual machine needs, so a session can prescribe one
- Record the convention imported load is expressed in, and count volume the same way on both sides
  of the loop
- Report what the catalogue still cannot explain, as the measure of how far it is from the training
- Erase everything belonging to one user on request, leaving the shared catalogue untouched

## Open questions

**None.** Three stood when this plan was written, and all three are closed.

**What earns a catalogue row.** The answer began as "holes only" and the engineer sharpened it into
something checkable, by catching a real case: `Shoulder Press (Dumbbell)` is the seated version of
our standing `Overhead Press (Dumbbell)`, and seated is what they actually train. The rule that
replaces the phrase:

> A movement earns a row when it differs from every existing row in something **the model
> represents** — its movement pattern, its implement, or the equipment items it requires. It does
> not when it differs only in its title, or in an attribute `TD-005` deliberately omitted.

That rule decides the list without a judgement call per entry. Seated dumbbell shoulder press earns
a row because it requires a bench with a back and the standing row requires only dumbbells
(`ADR-013`) — **not** because it is more stable, since `TD-005` omitted `stability_demand` on the
grounds that the value would be invented and then branched on, and reversing that omission is a new
record rather than an oversight being corrected. By the same rule a close grip, a rope attachment,
an alternating curl and a wide grip earn nothing: grip and attachment are not in the model.

**`Walking` and `Plank` are out.** Neither is resistance training as the rest of the model means it
— walking has no muscle attribution that could be credited without inflating volume, and a plank
has no repetitions. They stay out of the catalogue and are named as permanent gaps rather than left
mute in the report, so the coverage number is not read as a failure to curate.

**The assumed gym gains machines.** `TD-004`'s machine-free default no longer matches a catalogue
that contains machines, and the `knee_flexion` hole it has carried since `M1` closes for a user who
never opens the equipment screen. `S4.1` records what the new default contains and what it costs
when it is wrong — assuming a gym has a leg curl is still an assumption, and the record says so.

_(Execution does not start while this section is non-empty.)_

## Steps

### S4.1 — What a default gym contains

**Description:** supersede `TD-004` with a record that matches a catalogue containing machines, and
decide what a user who never describes their gym is programmed against.

**Technical actions:**

1. Invoke `/protocol-training`. `TD-004` is superseded, not reinterpreted — its assumed gym scoped a
   catalogue that this milestone changes (root standard 16).
2. Read `references/exercise-selection-within-a-movement-pattern.md` before deciding: it already
   holds that machines against free weights is null for whole-muscle growth, so the question is what
   a gym *contains*, never which implement is better.
3. Record what the new default is and what it costs, including whether the `knee_flexion` hole
   closes for a user who never opens the equipment screen.
4. No code. A record and nothing else.

**Tests:**

| Artifact | Layer | Test file |
|----------|-------|-----------|
| The decision itself | — | no tests; `node scripts/check-docs.mjs` proves the citation resolves |

**Depends on:** none

**Acceptance criteria:**

- `TD-004` reads `superseded-by`, and nothing else in it is edited.
- The new record says what a default gym contains and what that assumption costs when it is wrong.
- Every later step citing an assumed gym cites the new record.

### S4.2 — Equipment specific enough to name a machine

**Description:** the vocabulary grows the machine items the new catalogue rows require, and the
equipment screen groups rather than lengthens.

**Technical actions:**

1. Add one `EquipmentItem` per machine the catalogue needs, and no speculative ones (per `ADR-022`,
   `ADR-013`).
2. Keep the enum stored as text, never an ordinal — training history is append-only and an inserted
   value must not change what an old row means (root standard 7).
3. Group the equipment screen by where a movement is trained, so thirty checkboxes read as sections
   (standard 13, and the `M2` complaint that the screen is limited).
4. Every new item is a translated string in both dictionaries (standard 2).

**Tests:**

| Artifact | Layer | Test file |
|----------|-------|-----------|
| Every vocabulary value is offered by the API and translated in both locales | Unit | `frontend/lib/i18n/__tests__/locales.test.ts` |
| A new item round-trips through the equipment endpoint | Integration | `backend/Protocol.Api.Tests.Integration/Training/EquipmentEndpointsTests.cs` |
| Ticking a machine widens what the generator may draw | Unit | `backend/Protocol.Api.Tests.Unit/Training/EquipmentFilterTests.cs` |

**Depends on:** S4.1

**Acceptance criteria:**

- A gym with a leg press and no leg curl is expressible, and the generator prescribes only the first.
- No vocabulary value exists that no catalogue row requires.
- The equipment screen reads as grouped sections rather than one list.

### S4.3 — The catalogue widens

**Description:** the movements settled in the open questions get a row each, curated by hand, in C#.

**Technical actions:**

1. Apply the earns-a-row rule to all 51 movements before writing any of them: a movement earns a
   row only when it differs from every existing row in movement pattern, implement, or required
   equipment items (`ADR-013`, `TD-005`). Grip, attachment, tempo and stability are not in the
   model, and `TD-005` omitted the last of them on purpose.
2. Add one `Make(...)` per qualifying movement with its real `exercise_template_id` from the import,
   so the mapping is a lookup and never a title match (`ADR-002`, standards 8 and 9).
3. Attribute primary and secondary muscles under `TD-005`'s existing rule — "meaningfully loaded
   through a substantial range", not "anything that contracts". This is the soft spot `TD-005`
   names, and it is applied rather than re-decided.
4. Declare each row's equipment requirements in the requirements table; a row without them throws at
   startup rather than seeding an unperformable movement (`ADR-013`).
5. Split the catalogue file by movement pattern (per `ADR-023`).
6. Order the rows' `preference_rank` within each pattern, remembering it claims performability and
   never growth (`TD-015`).
7. Seeding stays idempotent by external template id, so a re-seed never duplicates a row nor
   rewrites an identifier a stored week already references.
8. Leave `Walking` and `Plank` out, and name them as permanent gaps rather than letting them sit
   mute in the coverage report — neither is resistance training as this model means it, and a
   coverage number that counts them reads as a failure to curate.

**Tests:**

| Artifact | Layer | Test file |
|----------|-------|-----------|
| Every row has requirements, a primary muscle and a distinct external id | Integration | `backend/Protocol.Api.Tests.Integration/Training/ExerciseCatalogueTests.cs` |
| No two rows share an `exercise_template_id` | Integration | `backend/Protocol.Api.Tests.Integration/Training/ExerciseCatalogueTests.cs` |
| Generation stays deterministic with the larger catalogue | Integration | `backend/Protocol.Api.Tests.Integration/Training/GeneratedWeekEndpointsTests.cs` |
| A gym with machines can reach every muscle group | Unit | `backend/Protocol.Api.Tests.Unit/Training/WeekGeneratorTests.cs` |
| No two rows share a movement pattern, implement **and** requirement set | Integration | `backend/Protocol.Api.Tests.Integration/Training/ExerciseCatalogueTests.cs` |

**Depends on:** S4.1, S4.2

**Acceptance criteria:**

- Every movement in scope resolves to a catalogue row when imported, and stops appearing as a gap.
- No row duplicates another in everything the model represents — a second row exists only where the
  pattern, the implement or the required equipment differs.
- A week generated for a gym with machines contains at least one direct `knee_flexion` movement.
- Generating repeatedly still writes one week — the guard `M3` had to repair.

### S4.4 — What a logged load means

**Description:** imported weight is a total, recorded once and used consistently wherever volume is
counted.

**Technical actions:**

1. Cite `ADR-024` where volume-load is computed, so the meaning travels with the arithmetic rather
   than living in a reader's head.
2. Compute volume-load from working sets only, on the same rule `TD-006` already applies to set
   counting.
3. Leave the unilateral case alone and say so at the line — `ADR-024` defers it to the record that
   consumes load, and modelling it here would be deciding a preference before its consumer exists.

**Tests:**

| Artifact | Layer | Test file |
|----------|-------|-----------|
| Volume-load counts working sets and ignores warm-up, drop and failure sets | Unit | `backend/Protocol.Api.Tests.Unit/Hevy/ImportedVolumeTests.cs` |
| A barbell and a dumbbell set at the same weight count the same | Unit | `backend/Protocol.Api.Tests.Unit/Hevy/ImportedVolumeTests.cs` |

**Depends on:** none

**Acceptance criteria:**

- Two sets at 30 kg count as the same load whatever the implement.
- Nothing in the code doubles or halves a load by inspecting equipment.

### S4.5 — How far the catalogue still is

**Description:** the gap is reported as a proportion of logged exercises, not only as a list.

**Technical actions:**

1. Report how many logged exercises the catalogue explains and how many it does not, alongside the
   named gaps `ADR-020` already surfaces.
2. Keep the named list bounded and ordered by how often each movement is trained, with a count of
   the rest (per `ADR-020`).
3. Answer in codes and numbers; the frontend owns every sentence (standard 3).

**Tests:**

| Artifact | Layer | Test file |
|----------|-------|-----------|
| The proportion is computed from current readings only, ignoring tombstoned workouts | Unit | `backend/Protocol.Api.Tests.Unit/Hevy/DerivedEquipmentTests.cs` |
| The screen shows the proportion and the bounded list | E2E | `frontend/e2e/equipment.spec.ts` |

**Depends on:** S4.3

**Acceptance criteria:**

- The proportion is visible and falls when the catalogue widens.
- A movement that got a row in `S4.3` no longer appears as a gap.

### S4.6 — Erasing everything of mine

**Description:** one deliberate action clears a user's own data, in development only, leaving the
shared catalogue untouched.

**Technical actions:**

1. Remove that user's profile, equipment, preferences, declined suggestions, generated weeks,
   imported workouts and snapshots, and their Hevy connection (per `ADR-025`).
2. Never touch `exercises` or the Data Protection key ring, and say why at the line (per `ADR-025`,
   `ADR-014`).
3. Gate it behind a configuration switch only the local stack sets, in the same shape as
   `Hevy:UseFake` (per `ADR-025`).
4. Require an explicit confirmation in the screen; it is never a side effect of anything else.
5. Log the run with its counts (standard 12) — afterwards, erased and never-imported look identical.

**Tests:**

| Artifact | Layer | Test file |
|----------|-------|-----------|
| Everything of the user's goes, and the catalogue and another user's data stay | Integration | `backend/Protocol.Api.Tests.Integration/Training/EraseUserDataTests.cs` |
| The endpoint is absent when the switch is off | Integration | `backend/Protocol.Api.Tests.Integration/Training/EraseUserDataTests.cs` |
| Erase, then generate and sync again from nothing | E2E | `frontend/e2e/erase.spec.ts` |

**Depends on:** none

**Acceptance criteria:**

- After an erase, the account still signs in and every screen reads as a fresh user's.
- The exercise catalogue row count is unchanged, and another user's data is untouched.
- With the switch off, the endpoint answers 404 rather than refusing politely.

### S4.7 — The ladder, containerized

**Description:** the full verification ladder green, in Docker.

**Technical actions:**

1. Climb all eleven rungs from `/protocol-feature`.
2. Confirm the widened catalogue seeds into the existing development database without duplicating a
   row or rewriting an identifier a stored week references.
3. Read `git status` and confirm nothing is left uncommitted (rung 11, standard 19).

**Tests:**

| Artifact | Layer | Test file |
|----------|-------|-----------|
| The whole suite | All | existing |

**Depends on:** S4.3, S4.5, S4.6

**Acceptance criteria:**

- Rungs 1 through 11 green.
- The development database gains the new catalogue rows and loses none of its existing data.

## Specifications

**Equipment vocabulary added** (per `ADR-022`; the final list follows the movements settled in the
open questions):

| Item | Named because |
|---|---|
| `LegCurlMachine` | closes the `knee_flexion` hole `TD-004` has carried since `M1` |
| `LegExtensionMachine`, `LegPressMachine`, `HackSquatMachine` | the most-trained lower-body movements outside the catalogue |
| `HipAbductionMachine` | 132 logged sessions, no equivalent free-weight row |
| `ChestPressMachine`, `PecDeckMachine` | machine horizontal push and adduction |
| `SeatedRowMachine`, `LatPulldownMachine` | machine horizontal and vertical pull |
| `ShoulderPressMachine`, `PreacherCurlMachine` | machine vertical push and elbow flexion |
| `CalfRaiseMachine`, `AbdominalMachine`, `BackExtensionBench` | the remaining named holes |

**Error codes** (standard 3):

| Code | Meaning |
|---|---|
| `UnknownEquipmentItem` | already exists; the widened vocabulary reuses it |
| `EraseNotAvailable` | the erase endpoint is off in this deployment |

**What an erase removes and spares** (per `ADR-025`):

| Removed | Spared |
|---|---|
| `training_profiles`, `user_equipment`, `exercise_exclusions`, `preferred_variants` | `exercises` — a global seed shared with every user |
| `declined_equipment_suggestions` | `DataProtectionKeys` — every other user's key depends on it |
| `generated_weeks` and everything below them | `AspNetUsers` — the account survives, the data does not |
| `performed_workouts` and everything below them, `hevy_workout_snapshots` | |
| `hevy_connections` — re-entering the key is part of exercising the loop | |

## Dependency order

```
S4.1 ──> S4.2 ──┐
                ├──> S4.3 ──> S4.5 ──┐
S4.4 ───────────┘                    ├──> S4.7
S4.6 ────────────────────────────────┘
```

Linearised: **S4.1 → S4.2 → S4.3 → S4.4 → S4.5 → S4.6 → S4.7**

`S4.4` and `S4.6` depend on nothing and could run at any point; they sit here so the catalogue work
stays contiguous.

## Deliverables

- [ ] S4.1 — what a default gym contains, recorded, superseding `TD-004`
- [ ] S4.2 — equipment specific enough to name a machine
- [ ] S4.3 — the catalogue widened, every row curated under `TD-005`
- [ ] S4.4 — an imported load means one thing, everywhere it is counted
- [ ] S4.5 — the remaining gap reported as a proportion
- [ ] S4.6 — erasing everything of one user's, in development only
- [ ] S4.7 — the verification ladder from `/protocol-feature`, green
- [ ] every capability bullet above covered by at least one step
