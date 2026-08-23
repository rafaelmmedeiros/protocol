# M2 — A week the user can live with

## Objective

The user describes the gym they actually train in, says which exercises they will not do and
which variant they prefer, swaps a prescribed exercise for another that trains the same thing,
and sees how long each session is expected to take. `M1` proved the week could be defended;
this milestone is about it being performable and acceptable to the person reading it — which
this corpus's own evidence says is what decides whether they train at all.

## Capabilities

Verbatim from `docs/ROADMAP.md`:

- Describe the equipment actually available, replacing the single assumed gym `M1` programmes
  against
- State a preference between variants of the same movement, and have the generator honour it
  where honouring it does not cost coverage
- Substitute one exercise in a generated week for another that trains the same thing
- Show how long a generated session is expected to take, before it is trained

## Open questions

_None._

Four were open when planning started. Three were settled into records — how equipment is
modelled (`ADR-010`), what shape a preference takes (`ADR-011`), and how a substitution avoids
editing an immutable week (`ADR-012`). The fourth, **what a preference may override before it
stops being adherence and starts being a worse programme**, is a training judgement rather than
a schema one and became `S2.1`: a scheduled research step in front of the step that consumes it.

## Steps

### S2.1 — Research: what a preference may override

**Description:** A user excluding exercises can starve a muscle. Where the line sits between
honouring a preference (adherence, which the corpus treats as the asymmetric cost) and emitting
a programme that is worse for it, and what the generator does at that line.

**Technical actions:**

1. Research through `/protocol-training`: what the evidence says about self-selected versus
   imposed exercise on adherence, enjoyment and outcome — extending
   `references/ranking-exercise-variants.md`, which already establishes filter-then-order and
   that individual response variance lives in the person rather than the person-by-exercise
   pairing (standard 15)
2. Research whether honouring a variant preference costs anything measurable, given that
   `references/exercise-variant-and-implementation.md` nulls implementation for whole-muscle
   growth in four direct trials
3. Record a `TD-###`: what a preference may override, what it may not, and what the generator
   does when an exclusion drops a muscle below `TD-008`'s floor — including whether the
   exclusion is refused, honoured with a surfaced shortfall, or partially honoured

**Tests:** _(none — this step produces records, not code)_

**Depends on:** none

**Acceptance criteria:**

- The record says plainly whether a preference may cost coverage, and marks the confidence of
  the knowledge under it.
- "A preference is always honoured and the shortfall is surfaced" is a valid outcome — but it
  is written as a decision with its cost, not left to fall out of the implementation.

---

### S2.2 — Available equipment

**Description:** The user's gym as a set over the enum the catalogue already carries, defaulting
to what `TD-004` assumed so a user who never opens the screen behaves exactly as in `M1`.

**Technical actions:**

1. Add the equipment set to the training profile as its own entity or column set (per
   `ADR-010`)
2. Default a new user's set to `TD-004`'s assumed gym, so the assumption becomes a default
   rather than disappearing (per `ADR-010`)
3. Add the migration (standard 10 — forward-only)
4. Filter catalogue selection in `WeekGenerator` by the set, leaving the rest of selection
   untouched (per `ADR-010`)
5. Surface a muscle the equipment set cannot train at all through the existing
   `UncoveredMuscles` channel rather than a new one (`TD-008`)

**Tests:**

| Artifact | Layer | Test file |
|----------|-------|-----------|
| Equipment filtering | Unit: a set without cables never yields a cable exercise; the `TD-004` default reproduces `M1`'s week exactly | `Protocol.Api.Tests.Unit/Training/EquipmentFilterTests.cs` |
| Equipment endpoints | Integration: round-trips, 401 unauthenticated, a new user starts with the default set | `Protocol.Api.Tests.Integration/Training/EquipmentEndpointsTests.cs` |

**Depends on:** none

**Acceptance criteria:**

- A user whose set matches `TD-004` gets byte-identical weeks to `M1`, proven by comparing
  generated content.
- A machine-only exercise never appears for a user without machines.

---

### S2.3 — Preference: exclusions and preferred variants

**Description:** The two lists `ADR-011` decided, and the generator honouring them under
whatever `S2.1` ruled.

**Technical actions:**

1. Add exclusions and preferred variants as per-user records (per `ADR-011`)
2. Add the migration (standard 10)
3. Apply as a filter before ordering, never as a score blended into `preference_rank` (per
   `ADR-011`)
4. Apply `S2.1`'s ruling when an exclusion drops a muscle below the floor (per that `TD-###`)
5. Add `GET`/`PUT` endpoints returning codes, never display text (standard 3)

**Tests:**

| Artifact | Layer | Test file |
|----------|-------|-----------|
| Preference in selection | Unit: an excluded exercise never appears; a preferred variant wins over catalogue rank; a preference never reorders `order_class` | `Protocol.Api.Tests.Unit/Training/PreferenceTests.cs` |
| Starved coverage | Unit: excluding every exercise for a muscle behaves exactly as `S2.1` decided | same file |
| Preference endpoints | Integration: round-trips, 401, a user never reads another's preferences | `Protocol.Api.Tests.Integration/Training/PreferenceEndpointsTests.cs` |

**Depends on:** `S2.1`, `S2.2`

**Acceptance criteria:**

- Excluding `Overhead Press (Barbell)` yields the dumbbell variant, and the prescription changes
  with the `order_class` rather than being carried over.
- No code path multiplies a preference into a score.

---

### S2.4 — Substituting one exercise

**Description:** A swap that writes a new week identical but for the replaced slot, leaving the
previous one readable.

**Technical actions:**

1. Add `POST /training/weeks/current/prescriptions/{id}/substitute`, writing a new week with one
   prescription replaced (per `ADR-012`, `ADR-003`)
2. Compute candidates from the same `movement_pattern` and `primary` muscle, filtered by the
   equipment set — no new column (per `ADR-012`, `ADR-010`)
3. Re-derive the prescription from the replacement's `order_class` rather than copying the old
   one (`TD-009`, `TD-010`, `TD-011`)
4. Recompute the week's shortfall on the new week rather than inheriting it (`TD-008`)
5. Add an endpoint listing the candidates for a slot

**Tests:**

| Artifact | Layer | Test file |
|----------|-------|-----------|
| Substitution | Integration: the previous week is unchanged and a new one exists; only the named slot differs | `Protocol.Api.Tests.Integration/Training/SubstitutionTests.cs` |
| Prescription follows the exercise | Integration: swapping across `order_class` changes reps, RIR and rest | same file |
| Shortfall | Integration: a swap that starves a muscle surfaces it on the new week | same file |
| Candidates | Integration: candidates share the movement pattern and primary muscle, and respect equipment | same file |

**Depends on:** `S2.2`

**Acceptance criteria:**

- Substituting changes one slot and nothing else, verified by comparing the two weeks.
- A candidate the user's gym cannot perform is never offered.

---

### S2.5 — Estimated session duration

**Description:** The number the time model already computes, shown before the session is
trained — which also makes `TD-012`'s invented transition constant falsifiable in use.

**Technical actions:**

1. Expose the estimated duration per session and for the week, computed on read from
   `SessionTimeBudget` and **not** stored (the reasoning `S1.9` used to decline `cut_applied`:
   a derived column can disagree with its source)
2. Cite `TD-012` at the line, including that the transition and warm-up terms are engineering
   estimates rather than evidence
3. Render at the edge in minutes (standard 4), translated in both locales (standard 2)

**Tests:**

| Artifact | Layer | Test file |
|----------|-------|-----------|
| Duration estimate | Unit: a session's estimate equals the sum of its slot costs plus warm-up | `Protocol.Api.Tests.Unit/Training/SessionEstimateTests.cs` |
| On screen | E2E: each session shows an estimate, and it is under the profile's session duration | `frontend/e2e/week.spec.ts` |

**Depends on:** none

**Acceptance criteria:**

- Every generated session shows an estimate no greater than the profile's stated duration.
- The estimate is absent from the database.

---

### S2.6 — The equipment and preference screens

**Description:** Where the user says what they have and what they will not do — the setup phase
that `references/cold-start-first-block.md` says decides retention.

**Technical actions:**

1. Add the screens inside the `(app)` route group so the session guard cannot be forgotten
   (`ADR-001`)
2. Reuse the existing `Equipment` section rather than adding a third nav entry
   (`frontend/CLAUDE.md` layout)
3. Every string in both dictionaries; a missing `pt-BR` key fails the typecheck (standard 2)
4. Write through a Server Function, resolving the backend's codes into sentences there
   (standard 3, and the `S1.10` finding that a function cannot cross into a Client Component)
5. Anything new on `/template` in the same commit (`frontend/CLAUDE.md`)

**Tests:**

| Artifact | Layer | Test file |
|----------|-------|-----------|
| Equipment screen | E2E: select, save, reload, values persist | `frontend/e2e/equipment.spec.ts` |
| Substitution on screen | E2E: swap an exercise, the week updates, the rest is unchanged | `frontend/e2e/week.spec.ts` |

**Depends on:** `S2.2`, `S2.3`, `S2.4`, `S2.5`

**Acceptance criteria:**

- Playwright selects on `data-testid`, never on translated text.
- A user who never opens the equipment screen gets `M1`'s week.

---

### S2.7 — The ladder, containerized

**Description:** The whole thing green where it ships.

**Technical actions:**

1. Climb the verification ladder in order (`/protocol-feature` step 5)
2. Rebuild the images before concluding — a container keeps serving old code, hit four times in
   `M1`
3. Comment any Docker trap found at the line that would otherwise look arbitrary

**Tests:**

| Artifact | Layer | Test file |
|----------|-------|-----------|
| Everything | E2E in Docker | `docker compose -f docker-compose.test.yml run --rm --build e2e` |
| Everything | Backend suites in Docker | `docker compose -f docker-compose.test.yml run --rm --build backend-tests` |

**Depends on:** `S2.6`

**Acceptance criteria:**

- Rungs 1–11 pass.
- The development database's account count is unchanged by the run (`W6`).

## Specifications

### Data model

| Table | Holds | Notes |
|-------|-------|-------|
| `user_equipment` | the set a user has | one row per user per `Equipment` value; absent means the `TD-004` default |
| `exercise_exclusions` | "never prescribe this" | user plus our exercise key (standard 8) |
| `preferred_variants` | "for this pattern, this row" | user, `movement_pattern`, our exercise key |

No new column on `exercises`: substitution candidates come from `movement_pattern` plus the
`primary` muscle, both of which `S1.6` already stores (`ADR-012`).

### API contracts

| Endpoint | Purpose |
|----------|---------|
| `GET` / `PUT /training/equipment` | the user's equipment set |
| `GET` / `PUT /training/preferences` | exclusions and preferred variants |
| `GET /training/weeks/current/prescriptions/{id}/candidates` | what a slot can be swapped for |
| `POST /training/weeks/current/prescriptions/{id}/substitute` | swap, writing a new week |

### Error catalog

| Code | HTTP | Trigger |
|------|------|---------|
| `EquipmentSetEmpty` | 400 | a set with nothing in it |
| `ExerciseNotFound` | 404 | excluding or preferring an exercise that is not ours |
| `NotACandidate` | 400 | substituting for an exercise that does not train the same thing |
| `PrescriptionNotFound` | 404 | substituting a slot that is not in the current week |

## Dependency order

```
S2.1 (research: what a preference may override)
S2.2 (equipment)                    — independent
S2.5 (session estimate)             — independent
    S2.3 (preference) — needs S2.1, S2.2
    S2.4 (substitution) — needs S2.2
        S2.6 (screens) — needs S2.2, S2.3, S2.4, S2.5
            S2.7 (ladder)
```

Linearised: `S2.1` → `S2.2` → `S2.5` → `S2.3` → `S2.4` → `S2.6` → `S2.7`.

`S2.1` leads because `S2.3` consumes a number it does not yet hold — a generator built before
its corpus is built from recalled numbers, which is the whole of standard 15.

## Deliverables

- [ ] S2.1 — Research: what a preference may override
- [ ] S2.2 — Available equipment
- [ ] S2.3 — Preference: exclusions and preferred variants
- [ ] S2.4 — Substituting one exercise
- [ ] S2.5 — Estimated session duration
- [ ] S2.6 — The equipment and preference screens
- [ ] S2.7 — The ladder, containerized
- [ ] The verification ladder from `/protocol-feature`, green
- [ ] Every capability bullet above covered by at least one step
- [ ] A user whose equipment matches `TD-004` still gets `M1`'s week, byte for byte
