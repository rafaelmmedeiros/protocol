# M3 — Closing the loop with Hevy

## Objective

The generated week stops being a screen and becomes something trained from. The user connects
their own Hevy account, pushes a generated week into it as routines, trains from those routines in
the gym, and presses sync — after which each session reads back with what was performed beside
what was prescribed, and the equipment the history reveals is offered as a correction to the gym
they described by hand. Nothing here progresses anything: `M3` closes the loop, and `M4` reasons
over what the loop produces.

## Capabilities

Verbatim from `docs/ROADMAP.md`:

- Connect a Hevy account with a personal API key the system can use and never reveals
- Push a generated week into Hevy as routines, remembering which routine belongs to which session
- Import training history out of Hevy, reconciling records that changed upstream
- Match a logged workout to the session that prescribed it, and read what was performed against
  what was prescribed
- Derive the available equipment from what has actually been trained, rather than from a
  description

## Open questions

**None.** Two stood when this plan was written, and both are closed:

- *What the comparison shows, and at what grain.* Settled as **one row per exercise slot with the
  performed sequence inline** — see **Specifications**. The grain follows from what the research
  established rather than from taste: the unit is the slot, not the muscle and not the set, and the
  sequence itself is the thing worth seeing.
- *Hevy's rate limits.* Settled by `ADR-021` **without the fact, because the fact does not exist**
  — the OpenAPI document declares no 429, no `Retry-After` and no rate-limit header. The client is
  written to survive a limit it cannot know rather than provoking one on a real account.

_(Execution does not start while this section is non-empty.)_

## No new training judgement

Unusually for a milestone this size, `M3` asks `/protocol-training` for nothing new, and that is a
consequence of the scope split rather than an oversight. Everything it needs is already recorded:
`TD-017` converts effort in both directions, `TD-006` keeps `warmup` sets out of the fractional
arithmetic, `TD-009` and `TD-011` supply the rep ranges and rest the push writes, and `TD-018`
supplies the one reserve the routine note displays. `M3` observes and compares; it asserts nothing
about how anyone should train. The judgements arrive in `M4`, which is why the research for them
belongs there and not here.

Three notes landed while this plan sat unexecuted, and none of them adds work to `M3` — they
constrain `M4` and they change one sentence here.
`references/separating-execution-modes-from-a-bare-log.md` establishes that effort cannot be
recovered from a bare log at all, which is why `ADR-016`'s routine note now frames the range as
something to terminate on effort within rather than a number to reach — the one change that makes
the log this milestone imports worth reading.
`references/progression-trigger-under-constant-effort-execution.md` and
`references/muscle-specific-repetition-drop-off-and-fibre-type.md` decide nothing here and are why
`S3.5` keeps the performed sequence ordered rather than summing it.

## Steps

### S3.1 — The Hevy connection

**Description:** a user saves their own Hevy API key; the system can use it and can never show it
back.

**Technical actions:**

1. Add an encrypted per-user key column, written through ASP.NET Core Data Protection (per
   `ADR-014`).
2. **Persist the Data Protection key ring to a volume** — `ADR-014` names this as the trap: without
   it every stored key becomes silently undecryptable after a container restart.
3. Validate the key on save with `GET /v1/user/info` before storing it; reject with a code
   (standard 3).
4. Never return the key from any endpoint — the API exposes only whether one is connected
   (per `ADR-014`).
5. Read the key from the environment nowhere. It is per-user data, not configuration
   (standard 11 governs secrets *of the system*; this is a secret *of the user*).

**Tests:**

| Artifact | Layer | Test file |
|----------|-------|-----------|
| Key encrypt/decrypt round trip, and that ciphertext differs from plaintext | Unit | `backend/Protocol.Api.Tests.Unit/Hevy/HevyKeyProtectionTests.cs` |
| Save, validate, connected-status, and that no response body ever contains the key | Integration | `backend/Protocol.Api.Tests.Integration/Hevy/HevyConnectionEndpointsTests.cs` |

**Depends on:** none

**Acceptance criteria:**

- A saved key is unreadable in the database by inspection.
- No endpoint response contains the key, including error responses.
- An invalid key is refused at save time with a stable code, not stored and discovered later.
- The stack restarts and a previously saved key still decrypts.

### S3.2 — The Hevy boundary

**Description:** one mapper per direction and an HTTP client, with no Hevy type reaching the
domain.

**Technical actions:**

1. Create the integration under `backend/Protocol.Api/Hevy/`, holding the client, the request and
   response contracts, and exactly two mappers — inbound and outbound (standard 17, one place per
   direction).
2. Implement the outbound effort conversion `RPE = 10 - RIR` and the inbound
   `RIR = 10 - ceil(RPE)` (per `TD-017`), the inbound one total over Hevy's eight anchors and
   rejecting any value outside them.
3. Map an exercise by its external key, never by title (per `ADR-002`, standards 8 and 9).
4. Assert by construction that no type under `Training/` references a Hevy contract, and that no
   symbol in the domain contains "RPE" (per `TD-017`).
5. Log every call with the correlation identifier (standard 12), and never log the key.

**Tests:**

| Artifact | Layer | Test file |
|----------|-------|-----------|
| All eight anchors inbound, rejection outside them, and the outbound map | Unit | `backend/Protocol.Api.Tests.Unit/Hevy/EffortConversionTests.cs` |
| A prescribed session to a routine payload, and a workout payload to our records | Unit | `backend/Protocol.Api.Tests.Unit/Hevy/HevyMappingTests.cs` |
| No `Training/` type references a Hevy contract | Unit | `backend/Protocol.Api.Tests.Unit/Hevy/BoundaryIsolationTests.cs` |

**Depends on:** none

**Acceptance criteria:**

- RPE 9.5 maps to 1 reserve short of RPE 9's — that is, to 0 — and 8.5 maps to 1, 7.5 to 2, 6 to 4.
- A fractional reserve cannot be produced by any input.
- An RPE Hevy does not offer is refused rather than rounded into range.
- A domain type can be constructed with no Hevy payload in sight.

### S3.3 — Pushing a week

**Description:** a generated week becomes a routine folder with one routine per session, and the
identifiers come back and are stored.

**Technical actions:**

1. Create a folder for the week and one routine per session inside it (per `ADR-015`).
2. Store the folder identifier as a number and each routine identifier as a string, in external
   key columns beside our week and sessions (per `ADR-015`, standard 8). Forward-only migration
   (standard 10).
3. Send working sets as `type: "normal"` with `rep_range` from `TD-009`, `rest_seconds` from
   `TD-011`, `weight_kg` null and no warm-up sets (per `ADR-016`).
4. Compose the prescribed reserve as one line of `notes` per routine exercise, in the user's
   language (per `ADR-016`, standard 2) — the push carries the locale.
5. Re-pushing a week with no workout matched to it overwrites its routines by `PUT`; once any
   workout has matched it, a regenerated week is pushed as new routines (per `ADR-017`).
6. Never call a delete — none exists (per `ADR-017`). A `PUT` against a routine the user removed
   is a push failure, surfaced as a code.
7. An exercise with no external key cannot be pushed and fails loudly (per `ADR-016`).
8. Writes are sequential with backoff, and a partly-created week is safe to retry because
   re-pushing reuses what exists (per `ADR-021`, `ADR-017`).

**Tests:**

| Artifact | Layer | Test file |
|----------|-------|-----------|
| Payload shape: one routine per session, ranges, rest, null weight, no warm-up sets | Unit | `backend/Protocol.Api.Tests.Unit/Hevy/RoutinePayloadTests.cs` |
| Re-push overwrites while untrained and creates new once matched | Integration | `backend/Protocol.Api.Tests.Integration/Hevy/PushWeekTests.cs` |
| The note line renders in both locales | Unit | `backend/Protocol.Api.Tests.Unit/Hevy/RoutineNoteTests.cs` |

**Depends on:** S3.1, S3.2

**A missing dependency, found while building and resolved here.** Action 5 needs to know whether a
week has been trained from, and nothing that records imported training existed — the plan listed
that as `S3.4`'s. `S3.3` therefore lands the **performed-training schema** it must query, and
`S3.4` implements the sync that fills it. No step moved and no order changed; what was wrong was
the implication that `S3.3` reads nothing.

**Acceptance criteria:**

- A pushed week yields one folder identifier and as many routine identifiers as it has sessions,
  all stored.
- No pushed set carries a weight.
- Pushing twice before any training leaves exactly one folder.
- Pushing after a matched workout leaves the trained-from routines untouched.

### S3.4 — Importing history

**Description:** sync pulls what changed since last time, appends versions, and keeps the raw
payload.

**Technical actions:**

0. The `performed_workouts`, `performed_exercises` and `performed_sets` tables already exist —
   `S3.3` created them because it had to read them. This step adds the version and tombstone
   columns `ADR-018` needs and the behaviour that fills all of it.
1. Page `GET /v1/workouts/events?since=<cursor>` and advance a per-user cursor (per `ADR-018`).
2. Append a new version row per `updated` event and a tombstone version per `deleted` event —
   never update, never delete (per `ADR-018`, standard 7).
3. Store the raw JSON alongside the mapped rows (per `ADR-018`), so a changed conversion is a
   recomputation.
4. Store all workout and set identifiers as external keys (standard 8), and every timestamp as UTC
   (standard 5).
5. Retain `warmup` sets on import; exclude them only where volume is counted (per `ADR-018`,
   `TD-006`).
6. The first sync is a backfill from the feed's epoch and is written to page rather than to assume
   a page size.
7. Requests are sequential, the cursor is persisted **as each page commits**, and a refusal is
   retried with backoff — a sync that gives up is a partial success with progress kept, never a
   restart (per `ADR-021`).

**Tests:**

| Artifact | Layer | Test file |
|----------|-------|-----------|
| Cursor advance, paging, and that a second sync fetches nothing new | Integration | `backend/Protocol.Api.Tests.Integration/Hevy/ImportHistoryTests.cs` |
| An updated workout appends a version and leaves the earlier one readable | Integration | `backend/Protocol.Api.Tests.Integration/Hevy/ReconciliationTests.cs` |
| A deleted workout writes a tombstone and removes no row | Integration | `backend/Protocol.Api.Tests.Integration/Hevy/ReconciliationTests.cs` |
| Warm-up sets are stored and excluded from fractional volume | Unit | `backend/Protocol.Api.Tests.Unit/Hevy/ImportedVolumeTests.cs` |

**Depends on:** S3.1, S3.2

**Acceptance criteria:**

- Re-importing a workout that changed upstream leaves the earlier version readable.
- A workout deleted in Hevy stops counting toward volume without any row being removed.
- The row count after a no-op sync is unchanged.

### S3.5 — Prescribed against performed

**Description:** a logged workout binds to the session that prescribed it, and the two are read
side by side.

**Technical actions:**

1. Bind on `routine_id` alone; a workout with none, or one we did not create, stays unbound
   (per `ADR-019`).
2. Read no title anywhere in the binding (per `ADR-019`, standard 9).
3. Convert reported effort inbound with `TD-017`; a set with no `rpe` yields no reserve rather
   than a default.
4. Expose the comparison read model at the settled grain — one entry per prescribed slot,
   carrying the performed sequence in set order (see **Specifications**).
5. Report the proportion of imported workouts that bind, as the evidence `ADR-019` says would
   justify revisiting it.

**Tests:**

| Artifact | Layer | Test file |
|----------|-------|-----------|
| Binding by identifier, and that a matching title never binds | Unit | `backend/Protocol.Api.Tests.Unit/Hevy/WorkoutBindingTests.cs` |
| Unbound history still counts toward volume | Unit | `backend/Protocol.Api.Tests.Unit/Hevy/ImportedVolumeTests.cs` |
| The comparison read model over a pushed, trained and synced week | Integration | `backend/Protocol.Api.Tests.Integration/Hevy/ComparisonEndpointsTests.cs` |

**Depends on:** S3.3, S3.4

**Acceptance criteria:**

- A workout started from a pushed routine binds to the session that produced it.
- A workout with the same title and no `routine_id` does not bind.
- A set with no reported effort produces no reserve, and the screen can tell that from a zero.
- A performed sequence reads back in set order, so 11/9/8 is distinguishable from 8/9/11.

### S3.6 — Equipment the history reveals

**Description:** logged exercises imply equipment items, offered as suggestions the user confirms.

**Technical actions:**

1. Derive candidate items from the requirements of exercises actually logged (per `ADR-020`,
   `ADR-013`).
2. Surface each with the logged exercise that implied it and when (per `ADR-020`).
3. Confirmation adds to the user's existing equipment set; nothing is ever removed by inference
   (per `ADR-020`).
4. A declined suggestion is not offered again (per `ADR-020`).
5. A logged exercise outside our catalogue implies no equipment and is surfaced separately as a
   catalogue gap (per `ADR-020`, `TD-004`).

**Tests:**

| Artifact | Layer | Test file |
|----------|-------|-----------|
| Derivation, add-only, and that absence never removes | Unit | `backend/Protocol.Api.Tests.Unit/Hevy/DerivedEquipmentTests.cs` |
| Suggest, confirm, decline, and that a declined item does not return | Integration | `backend/Protocol.Api.Tests.Integration/Hevy/DerivedEquipmentEndpointsTests.cs` |

**Depends on:** S3.4

**Acceptance criteria:**

- An exercise logged on a machine the user never ticked produces a suggestion naming it.
- Confirming widens the draw pool the generator uses; declining changes nothing.
- A muscle trained only outside the catalogue is reported as a gap, not silently ignored.

### S3.7 — The screens

**Description:** connect, push, sync, read the comparison, and accept equipment suggestions.

**Technical actions:**

1. A Hevy connection section in settings — save a key, see connected or not, never see the key
   (per `ADR-014`, `ADR-001`'s sibling placement).
2. An explicit push control on the week. Pushing writes to a third party and is never automatic.
3. An explicit sync control, with the outcome reported — how many workouts arrived, how many
   bound.
4. The comparison view — one block per exercise, prescription above and the performed sequence
   below, with a marker where the sequence fell outside the prescribed range. Slots with no
   matching work read as not performed; exercises performed but never prescribed are listed after
   them; workouts bound to no session at all are listed separately (per `ADR-019`).
5. Every string in both dictionaries, no hardcoded text (standard 2); every error rendered from a
   code (standard 3).
6. Semantic elements, labelled controls, keyboard reachable (standard 13).

**Tests:**

| Artifact | Layer | Test file |
|----------|-------|-----------|
| Both dictionaries carry every new key with matching signatures | Unit | `frontend/lib/i18n/__tests__/locales.test.ts` |
| Connect, push, sync, read the comparison | E2E | `frontend/e2e/hevy.spec.ts` |
| Accept and decline an equipment suggestion | E2E | `frontend/e2e/equipment.spec.ts` |

**Depends on:** S3.5, S3.6

**Acceptance criteria:**

- The key is never rendered, including after a reload.
- Push and sync are deliberate actions with visible outcomes.
- Every new screen reads correctly in `pt-BR`.
- A session shows its prescribed slots and its performed sequences on one screen without
  navigating, and an unprescribed exercise is visible rather than dropped.

### S3.8 — The ladder, containerized

**Description:** the full verification ladder green, in Docker.

**Technical actions:**

1. Climb all eleven rungs from `/protocol-feature`.
2. Confirm the Data Protection key ring survives a container restart with a real saved key
   (per `ADR-014`) — a rung-7 concern that no host run can prove.
3. Confirm the E2E suite never reaches the real Hevy API, and never the development database.
4. Read `git status` and confirm nothing is left uncommitted (rung 11, standard 19).

**Tests:**

| Artifact | Layer | Test file |
|----------|-------|-----------|
| The whole suite | All | existing |

**Depends on:** S3.7

**Acceptance criteria:**

- Rungs 1 through 11 green.
- No test run touches the real Hevy account.

## Specifications

**External keys added** (all standard 8, forward-only per standard 10):

| Our record | Column | Type | Source |
|---|---|---|---|
| generated week | `hevy_routine_folder_id` | number, nullable | `POST /v1/routine_folders` |
| planned session | `hevy_routine_id` | string, nullable | `POST /v1/routines` |
| imported workout | `hevy_workout_id` | string | events feed |
| `hevy_connections` row | `ProtectedApiKey` | text | the user |
| `hevy_connections` row | `SyncCursor` | timestamptz, nullable | the events feed |

Every column on our own records is nullable and legitimately so: a week that was never pushed has
no folder.

**The connection is its own table, not two columns on the user** — corrected here in `S3.1`, where
building it made the reason obvious. A user who never connected has no row at all rather than a row
full of nulls, the cursor lives beside the key it is meaningless without, and disconnecting one day
is a delete rather than three nullifications. The key is **text** rather than bytes because Data
Protection's string API already returns base64url; storing bytes would mean decoding in and
encoding out for nothing.

**Error codes** (standard 3 — codes, never display text):

| Code | Meaning |
|---|---|
| `HevyKeyInvalid` | the key was rejected by Hevy at save time |
| `HevyNotConnected` | an operation needing a key ran without one |
| `HevyUnreachable` | the API could not be reached or returned a server error |
| `HevyRateLimited` | the API refused for rate reasons; retried with backoff first (`ADR-021`) |
| `WeekNotPushed` | a sync-dependent operation ran against a week never pushed |
| `ExerciseNotMappable` | a prescribed exercise has no external key and cannot be pushed |
| `PushedRoutineMissing` | a `PUT` target no longer exists in Hevy |
| `WeekAlreadyTrainedFrom` | the week's routines are evidence and are not rewritten (`ADR-017` revision) |

**The comparison read model.** One entry per prescribed slot, in session order:

| Field | Source |
|---|---|
| exercise | ours, by our key (`ADR-002`, standard 9) |
| prescribed sets, range, reserve, rest | the stored week (`ADR-003`, `TD-009`, `TD-011`, `TD-018`) |
| performed sets | the bound workout's `normal` sets **in set order**, each carrying its own load and repetitions |
| performed reserve | `TD-017` inbound, per set, **absent when `rpe` is absent** — which is every set observed so far |
| outcome | in range / above / below / not performed, derived per set and summarised per slot |

Two rules the model must not break. **The sequence is ordered and is never reduced to a total** —
11/9/8 and 8/9/11 are different facts, and `references/progression-trigger-under-constant-effort-execution.md`
is why. And **an absent reserve is absent, never zero**: with `rpe` null on every set observed, the
distinction between "reported nothing" and "reported no reserve left" is the difference between an
empty column and a false claim about how hard the user worked.

Exercises performed but never prescribed, and workouts bound to no session, are carried alongside
rather than discarded — `ADR-019` makes unbound history first-class, and it is what `ADR-020` reads.

**Effort conversion** is `TD-017` and is not restated here; the table lives in the record.

## Dependency order

```
S3.1 ──┐
       ├──> S3.3 ──┐
S3.2 ──┤           ├──> S3.5 ──┐
       └──> S3.4 ──┤           ├──> S3.7 ──> S3.8
                   └──> S3.6 ──┘
```

Linearised: **S3.1 → S3.2 → S3.3 → S3.4 → S3.5 → S3.6 → S3.7 → S3.8**

`S3.1` and `S3.2` are independent of each other and either could start; the order above puts the
key first because everything else is untestable against a real account without it.

## Deliverables

- [x] S3.1 — a per-user key, encrypted, validated, never returned
- [x] S3.2 — one mapper per direction, and no Hevy type in the domain
- [x] S3.3 — a week pushed as a folder of routines, identifiers stored
- [x] S3.4 — incremental import, versions and tombstones, raw payload retained
- [x] S3.5 — workouts bound to the sessions that prescribed them, and read side by side, sequence intact
- [x] S3.6 — equipment suggested from history, add-only and confirmed
- [x] S3.7 — the screens, both locales
- [x] S3.8 — the verification ladder from `/protocol-feature`, green
- [x] every capability bullet above covered by at least one step
