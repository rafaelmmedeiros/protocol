# M1 — A training week from a training profile

## Objective

A signed-in user states what they train for and what they have available — a goal, days per
week, and how long a session can last — and the system builds a week of sessions from it: the
split, the exercises, and the sets, repetitions and rest for each one. The week is stored as
generated and read back in the app. Every number in it traces to a training decision record;
none of them was recalled.

## Capabilities

Verbatim from `docs/ROADMAP.md`:

- Capture a training profile: the goal, how many days a week the user will train, and how long
  a session can last
- Turn a weekly frequency into a split, using a template the literature supports
- Generate a week of sessions from a training profile
- Prescribe sets, repetitions and rest for every exercise in a generated session
- Show a generated week in the app, session by session, before it exists anywhere else

The goal `M1` supports is hypertrophy (`ADR-004`, revision of 2026-08-22). Other goals are
collected by the field and rejected with a code.

## Open questions

_None._

Three were open when this plan was first written. Two were settled — the supported goal
(`ADR-004`) and whether rest is a profile field (`ADR-007`) — and the third, what stands in for
experience when the profile no longer asks, became `S1.1`: a scheduled research step rather
than an unscheduled unknown. Everything still unknown below is unknown because it has not been
researched yet, and the step that researches it precedes the step that consumes it.

## Steps

### S1.1 — Research: training status and the cold start

**Description:** What "beginner", "intermediate" and "advanced" actually denote in the
literature, whether they are measurable rather than self-reported, and what a system with no
training history can honestly assume about a new user.

**Technical actions:**

1. Research through `/protocol-training`: what training status means, what it is measured by
   (performance, response to a stimulus, years, none of them), and how well it predicts what a
   programme should differ in (standard 15)
2. Research what the evidence supports for a first block when status is unknown — a
   conservative start that escalates, a calibration protocol, or a self-report used knowingly
   as a proxy
3. Record the decision as a `TD-###`: what `M1` assumes about a user it has never observed, and
   what that assumption costs a user at either extreme

**Tests:** _(none — this step produces records, not code)_

**Depends on:** none

**Acceptance criteria:**

- The note says plainly whether self-reported level is a usable variable or a folk category,
  and marks its confidence tier.
- "No calibration; the first week is deliberately conservative for everyone" is a valid
  outcome — but it is written as a decision with its cost, not left as an omission.

---

### S1.2 — Research: the split

**Description:** What weekly frequency implies for a split, and how often each muscle group is
trained within it.

**Technical actions:**

1. Research and record which split templates the literature supports at 2, 3, 4, 5 and 6
   sessions a week (standard 15)
2. Research and record the per-muscle weekly frequency the evidence supports for hypertrophy
   (`ADR-004`)
3. Record a `TD-###` per decision, naming the simplification and what it costs

**Tests:** _(none)_

**Depends on:** `S1.1` — how much a split should differ by training status is part of what
`S1.1` settles

**Acceptance criteria:**

- Every frequency the profile can hold has a split decided for it, or is explicitly rejected as
  unsupported with a reason and an error code.
- Each note carries a confidence tier and a stated boundary.

---

### S1.3 — Research: exercise selection and ordering

**Description:** Which exercises fill a slot, in what order, and under what assumption about
the gym — since equipment is not described until `M2`. This step is what defines a *slot*, and
the next one prescribes into it.

**Technical actions:**

1. Research and record how exercises are selected for a movement pattern and a muscle group
   (standard 15)
2. Research and record ordering within a session — including the case the engineer described,
   where a small muscle is placed last deliberately to exploit accumulated fatigue
3. Record the attributes an exercise must carry for selection to be possible: movement pattern,
   musculature, compound or isolation, equipment demand, ordering class. This list is the input
   to `S1.6`'s schema
4. Record the gym `M1` programmes for as its own decision, so `M2` supersedes a written
   assumption rather than an implicit one (`ADR-002` — the catalogue is ours)

**Tests:** _(none)_

**Depends on:** `S1.2`

**Acceptance criteria:**

- A slot is defined concretely enough that `S1.4` can prescribe into it: what it is, where it
  sits, and what it demands.
- The assumed gym is written down.

---

### S1.4 — Research: the prescription per slot

**Description:** Sets, repetition range, proximity to failure, and rest — all four as
properties of a slot, because they are not independent of each other and none of them is a
property of the person.

**Technical actions:**

1. Research and record weekly set volume per muscle group for hypertrophy (`ADR-004`;
   standard 15)
2. Research and record repetition ranges per slot, and their relationship to the slot's
   position and to whether it is compound or isolation
3. Research and record proximity to failure — repetitions in reserve — as a prescribed
   variable, not an implicit one
4. Research and record rest between sets **per slot**, as a function of repetition range,
   movement demand and position (`ADR-007` — the user does not answer this; the record does)
5. Record how `S1.1`'s cold-start answer modifies any of the above, if it does

**Tests:** _(none)_

**Depends on:** `S1.1`, `S1.3`

**Acceptance criteria:**

- A set count, a repetition range, a proximity-to-failure target and a rest interval exist for
  every kind of slot `S1.3` defined, each traceable to a record.
- The prescription can express a session whose first slot rests for over two minutes and whose
  last rests for one — if the research supports it, and with a record saying why.
- Where a record rests on `contested` or `thin` knowledge, it says so.

---

### S1.5 — Research: the time budget

**Description:** How a session's available minutes convert into a number of slots, and what
gets cut when the volume the research prescribes does not fit the time the user has.

**Technical actions:**

1. Research and record the time a set actually costs, including its prescribed rest and the
   transition between exercises (standard 15)
2. Record the rule for what is sacrificed first when the budget does not close — total volume,
   exercise count, or rest — naming the cost of that choice

**Tests:** _(none)_

**Depends on:** `S1.4`

**Acceptance criteria:**

- Three sessions of forty minutes and five of fifty produce different, defensible weekly
  volumes rather than the same volume redistributed.
- The cut rule is a record, not an implementation detail discovered while writing `S1.8`.

---

### S1.6 — The exercise catalogue

**Description:** Our exercise entity, carrying the attributes selection needs and Hevy's
identifier beside our own, seeded without a network call.

**Technical actions:**

1. Add the `Exercise` entity under `Training/` with our own primary key and
   `external_template_id` holding Hevy's `exercise_template_id` (per `ADR-002`; standards 8
   and 9)
2. Add the attributes `S1.3` named
3. Add the EF Core migration (standard 10 — forward-only; `backend/CLAUDE.md` for the command)
4. Seed the catalogue through a hosted service alongside `DatabaseMigrator`, never between
   `builder.Build()` and `app.Run()` (`backend/CLAUDE.md` invariant)
5. Keep titles as they arrive, in English, display-only (standard 9)

**Tests:**

| Artifact | Layer | Test file |
|----------|-------|-----------|
| `Exercise` mapping and the seed | Integration: the catalogue is present and every row carries an external template id | `Protocol.Api.Tests.Integration/Training/ExerciseCatalogueTests.cs` |

**Depends on:** `S1.3`

**Acceptance criteria:**

- Every seeded exercise has a non-null `external_template_id`.
- No code matches, groups or compares on a title (standard 9).

---

### S1.7 — The training profile

**Description:** The profile entity, its migration, and the endpoints that read and write it.
Three fields, decided last on purpose.

**Technical actions:**

1. Add the `TrainingProfile` entity — goal, days per week, and `session_duration_seconds`
   (per `ADR-004`; standard 4 — canonical units in the field name)
2. No rest column (`ADR-007`)
3. Add the migration (standard 10)
4. Add `GET` and `PUT` endpoints under `Training/`, camelCase JSON, returning error codes and
   never display text (standard 3; `backend/CLAUDE.md` invariant)
5. Reject a goal other than hypertrophy with `GoalNotSupported` rather than generating
   something unresearched (`ADR-004`, revision of 2026-08-22)

**Tests:**

| Artifact | Layer | Test file |
|----------|-------|-----------|
| Profile validation | Unit: rejects a frequency or duration outside the supported range, no I/O | `Protocol.Api.Tests.Unit/Training/TrainingProfileTests.cs` |
| Profile endpoints | Integration: round-trips through the API, 401 unauthenticated, `GoalNotSupported` on any other goal | `Protocol.Api.Tests.Integration/Training/TrainingProfileEndpointsTests.cs` |

**Depends on:** `S1.2` (which frequencies are supported), `S1.5` (which durations are)

**Acceptance criteria:**

- A profile written through the API is read back identically, in seconds.
- An unsupported goal returns its code; the frontend is what turns it into a sentence.

---

### S1.8 — The generator

**Description:** The pure domain service: a profile and a catalogue in, a week of prescribed
sessions out. No I/O, deterministic, every number citing its record.

**Technical actions:**

1. Add `Training/WeekGenerator` as a pure service inside `Protocol.Api` (per `ADR-006`)
2. Derive the split from the profile's frequency (per the `S1.2` records)
3. Fill each session's slots from the catalogue and order them (per the `S1.3` records)
4. Prescribe sets, repetitions, proximity to failure and rest per slot (per the `S1.4` records)
5. Fit the result to the session's time budget, applying the cut rule (per the `S1.5` records)
6. Apply whatever `S1.1` decided about a user the system has never observed
7. Anchor the week to Monday (standard 6)
8. Cite the record id at the line for every number the code carries (standard 15)
9. Produce the same week for the same profile and catalogue (per `ADR-005`)

**Tests:**

| Artifact | Layer | Test file |
|----------|-------|-----------|
| `WeekGenerator` | Unit: a week per supported frequency, asserted whole | `Protocol.Api.Tests.Unit/Training/WeekGeneratorTests.cs` |
| `WeekGenerator` | Unit: the same profile twice produces an identical week (`ADR-005`) | same file |
| `WeekGenerator` | Unit: forty minutes and ninety minutes at the same frequency produce different volumes, per the cut rule | same file |
| `WeekGenerator` | Unit: rest differs between slots within one session, per the `S1.4` records | same file |
| `WeekGenerator` | Unit: the week starts on Monday regardless of locale (standard 6) | same file |

**Depends on:** `S1.5`, `S1.6`, `S1.7`

**Acceptance criteria:**

- No number in the generator is without a `TD-###` at the line.
- The service compiles with no dependency on the database or on HTTP.

---

### S1.9 — Persisting a generated week

**Description:** Store the week as generated, immutable, with the profile it came from, and
expose it.

**Technical actions:**

1. Add the `GeneratedWeek`, `GeneratedSession` and prescription entities, with the profile's
   values snapshotted onto the week (per `ADR-003`)
2. Store `week_start_date` on Monday (standard 6) and every timestamp in UTC (standard 5)
3. Store rest as `rest_seconds` and any load as `weight_kg` (standard 4)
4. Add the migration (standard 10)
5. Add `POST` to generate and persist, and `GET` to read the current week — regeneration writes
   a new row and never edits one (per `ADR-003`)

**Tests:**

| Artifact | Layer | Test file |
|----------|-------|-----------|
| Generation endpoint | Integration: generating twice leaves two weeks, the first unchanged | `Protocol.Api.Tests.Integration/Training/GeneratedWeekEndpointsTests.cs` |
| Generation endpoint | Integration: a profile edited after generation does not alter the stored week | same file |
| Read endpoint | Integration: 401 unauthenticated, and a user never reads another user's week | same file |

**Depends on:** `S1.8`

**Acceptance criteria:**

- A stored week is unchanged after the profile behind it changes.
- Assertions go through the API, not against tables (`backend/CLAUDE.md` testing rule).

---

### S1.10 — The Profile section

**Description:** A new top-level section where the user fills in the profile.

**Technical actions:**

1. Add `app/(app)/profile/` inside the route group, so the session guard cannot be forgotten
   (per `ADR-001`; `frontend/CLAUDE.md` layout)
2. Add the nav entry with its `data-testid`, beside Equipment (per `ADR-001`)
3. Add every string to `lib/i18n/dictionaries/en-US.ts` and `pt-BR.ts` (standard 2)
4. Write through a Server Function or the proxy route, never calling the API from a component
   (`frontend/CLAUDE.md` invariant)
5. Turn the backend's error codes into sentences in the dictionary, never displaying its text
   (standard 3)
6. Render duration in minutes and store seconds — convert at the render edge (standard 4)
7. Labelled inputs, keyboard reachable, no component naming a colour (standard 13;
   `frontend/CLAUDE.md` invariants)

**Tests:**

| Artifact | Layer | Test file |
|----------|-------|-----------|
| Minutes/seconds conversion | Unit (Vitest, `lib/` only) | `frontend/lib/__tests__/duration.test.ts` |
| The profile screen | E2E: fill, save, reload, values persist | `frontend/e2e/profile.spec.ts` |

**Depends on:** `S1.7`

**Acceptance criteria:**

- Every visible string resolves in both locales; a missing `pt-BR` key fails the typecheck.
- Playwright selects on `data-testid`, never on translated text.

---

### S1.11 — The generated week on screen

**Description:** The week read back session by session — the first screen in this product that
shows something it decided itself.

**Technical actions:**

1. Add the week view inside the route group (per `ADR-001`)
2. Render sessions in Monday-first order (standard 6)
3. Show sets, repetitions and rest per exercise; format seconds and kilograms at the edge
   (standard 4)
4. Give it an empty state that says what will land there (`frontend/CLAUDE.md`)
5. Use the existing `ui/` components; anything new appears on `/template` in the same commit

**Tests:**

| Artifact | Layer | Test file |
|----------|-------|-----------|
| The week screen | E2E: profile → generate → the week renders with the expected session count | `frontend/e2e/week.spec.ts` |

**Depends on:** `S1.9`, `S1.10`

**Acceptance criteria:**

- A user with no week sees the empty state, not an error.
- The number of sessions matches the profile's frequency, and the first is Monday's.

---

### S1.12 — The ladder, containerized

**Description:** The whole thing green where it actually ships.

**Technical actions:**

1. Climb the verification ladder in order (`/protocol-feature` step 5)
2. Rebuild the images before concluding — a container keeps serving old code (`CLAUDE.md`)
3. Comment any Docker trap found at the line that would otherwise look arbitrary (win `W3`)

**Tests:**

| Artifact | Layer | Test file |
|----------|-------|-----------|
| Everything | E2E in Docker | `docker compose -f docker-compose.test.yml run --rm --build e2e` |
| Everything | Backend suites in Docker | `docker compose -f docker-compose.test.yml run --rm --build backend-tests` |

**Depends on:** `S1.11`

**Acceptance criteria:**

- Rungs 1–10 pass, with the test stack up alongside the development one.
- The development database's account count is unchanged by the run (win `W6`).

## Specifications

### Data model

Every table carries our own primary key; Hevy identifiers are external columns beside it
(standard 8). Units are in the field names (standard 4); timestamps are UTC (standard 5).

| Table | Holds | Notes |
|-------|-------|-------|
| `exercises` | our catalogue | `external_template_id` = Hevy's `exercise_template_id`; title display-only; selection attributes from `S1.3` |
| `training_profiles` | one per user | `goal`, `days_per_week`, `session_duration_seconds`; no rest column (`ADR-007`) |
| `generated_weeks` | one row per generation | `week_start_date` (Monday), `generated_at` (UTC), the profile's values snapshotted; never updated |
| `generated_sessions` | the days of a week | ordered position within the week |
| `generated_prescriptions` | a slot in a session | exercise reference, position, set count, repetition range, proximity to failure, `rest_seconds` |

### API contracts

| Endpoint | Purpose |
|----------|---------|
| `GET /training/profile` | the current user's profile, or 404 |
| `PUT /training/profile` | create or replace it |
| `POST /training/weeks` | generate and persist a week from the current profile |
| `GET /training/weeks/current` | the most recently generated week |

JSON is camelCase (`backend/CLAUDE.md`). Every response is codes and data, never display text
(standard 3).

### Error catalog

| Code | HTTP | Trigger |
|------|------|---------|
| `ProfileNotFound` | 404 | generating or reading with no profile saved |
| `GoalNotSupported` | 400 | any goal other than hypertrophy (`ADR-004`) |
| `FrequencyOutOfRange` | 400 | days per week outside what `S1.2` decided |
| `DurationOutOfRange` | 400 | session duration outside what `S1.5` decided |

The frontend owns every sentence these become.

## Dependency order

```
S1.1 (training status, cold start)
└── S1.2 (split)
    └── S1.3 (selection, ordering, the slot, the assumed gym)
        └── S1.4 (prescription per slot: sets, reps, RIR, rest)   ← also needs S1.1
            └── S1.5 (time budget and the cut rule)

S1.6 (catalogue) — needs S1.3
S1.7 (profile)   — needs S1.2, S1.5
S1.8 (generator) — needs S1.5, S1.6, S1.7
S1.9 (persistence) — needs S1.8
S1.10 (profile screen) — needs S1.7
S1.11 (week screen) — needs S1.9, S1.10
S1.12 (ladder) — needs S1.11
```

Linearised: `S1.1` → `S1.2` → `S1.3` → `S1.4` → `S1.5` → `S1.6` → `S1.7` → `S1.8` → `S1.9` →
`S1.10` → `S1.11` → `S1.12`.

The first five steps are research and produce no code. That order is the point: a generator
written before them is a generator built from recalled numbers, which is indistinguishable
afterwards from a researched one (standard 15).

## Deliverables

- [ ] S1.1 — Research: training status and the cold start
- [ ] S1.2 — Research: the split
- [ ] S1.3 — Research: exercise selection and ordering
- [ ] S1.4 — Research: the prescription per slot
- [ ] S1.5 — Research: the time budget
- [ ] S1.6 — The exercise catalogue
- [ ] S1.7 — The training profile
- [ ] S1.8 — The generator
- [ ] S1.9 — Persisting a generated week
- [ ] S1.10 — The Profile section
- [ ] S1.11 — The generated week on screen
- [ ] S1.12 — The ladder, containerized
- [ ] The verification ladder from `/protocol-feature`, green
- [ ] Every capability bullet above covered by at least one step
- [ ] Every number in the generator carries a `TD-###` at the line (standard 15)
- [ ] `/protocol-training`'s index lists every note and record this milestone produced
