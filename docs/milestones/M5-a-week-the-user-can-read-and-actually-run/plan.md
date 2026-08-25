# M5 — A week the user can read and actually run

## Objective

A generated session can be read rather than trusted: every slot says what it trains, why it is
there, and what swapping it would change, and the week states the direct and indirect volume each
muscle group receives against the target it was generated under. The plan stops being a calendar
week and becomes an ordered queue, so a session missed on Tuesday is the one waiting next rather
than volume silently lost — and what a muscle has actually accumulated across weeks is reported
instead of being quietly re-declared every Monday.

## Capabilities

Verbatim from `docs/ROADMAP.md`:

- Show what a prescribed exercise trains, so a session can be read instead of trusted
- Report direct and indirect set volume per muscle group against the week's target, naming where
  it falls short
- Say why an exercise fills a slot, and what substituting it would change
- Choose the split for a training frequency, rather than receiving the only one mapped to it
- Carry a session that did not happen into the next week, rather than regenerating past it
- Report what a muscle has accumulated across weeks when the same session is repeatedly missed

## Open questions

_None. Execution may start._

Both questions this plan opened with are resolved:

- **What a queue pushes to Hevy** is `ADR-031`: one folder per generated plan, named for when the
  plan was generated. `ADR-015` and `ADR-017` survive unchanged in substance — a cycle is pushed
  whole, so a queue reaches the trained-from refusal no more often than a week does.
- **The volume non-monotonicity was downgraded, not decided**, and the reason is where it came
  from. See the note below.

## A finding this milestone deliberately does not act on

At the boundary where `TD-013`'s ladder stops firing, a longer session can produce *less* volume
than a shorter one: `3x40` yields 19 slots and 7.5 fractional sets because forty minutes cannot
reach the floor at prescribed rest and the ladder is forced to cut rest, while `3x45` reaches the
floor uncut and yields 17 and 6.0. The same inversion appears at `4x25` against `4x30`. It is real
arithmetic over valid inputs, and the corpus suggests the shorter session's arrangement is the
better one — `inter-set-rest-and-hypertrophy` puts the whole rest effect in the step from one to
two minutes, and `weekly-set-volume-for-hypertrophy` is a square root with no plateau.

**It is recorded here rather than fixed here because no user has met it.** It was found by
sweeping a synthetic grid while verifying `TD-022`'s ceiling, not by observing the product; the
one real profile in this repo is `5x60`, which is nowhere near the boundary. Under the harness
backlog's own evidence gate that makes it an idea, not a pain.

Acting on it would also mean reopening `TD-022` within hours of writing it — that record forbids
exactly this fix, on the ground that compressing a researched rest interval to buy volume above
the researched target trades a measured quantity for a convention. Whether that clause is right is
its own decision, measured on its own terms, and it blocks no step below.

_(Execution does not start while `## Open questions` is non-empty.)_

## Steps

### S5.1 — Research: which splits a frequency may offer

**Description:** `TD-003` maps one template per frequency and names "the user asks to choose" as
its first revisit trigger. Decide which templates each supported frequency may offer, and what
constrains the set.

**Technical actions:**

1. Research through `/protocol-training`: what constrains a template set beyond schedulability —
   weekly repetition, per-muscle frequency landing at 2-3x, and rest distribution (standard 15)
2. Establish whether anything separates `Upper/Lower/Upper/Lower/Full` from `Upper/Lower/Push/Pull/Legs`
   at five days, or `PPL x2` from `Upper/Lower x3` at six, beyond convention
3. Record a `TD-###` superseding `TD-003`: the templates offered per frequency, which is the
   default, and the standing rule that no template may be presented as better for growth
4. State what the record gives up: `TD-003`'s rest distribution is guidance once `ADR-027` removes
   weekday assignment, and a chosen template cannot be argued against on evidence

**Tests:** _(none — this step produces records, not code)_

**Depends on:** none

**Acceptance criteria:**

- Every offered template repeats over a fixed number of sessions and lands per-muscle frequency in
  2-3x, with the arithmetic shown per template.
- "No new template is offered at frequency N" is a valid outcome, written as a decision with its
  reason rather than left as an omission.
- The record says plainly that the choice is scheduling and preference, never growth.

---

### S5.2 — Research: the dose window when the plan is a queue

**Description:** `TD-014`'s target is six fractional sets **per week**, and `ADR-027` leaves the
plan with no weeks. Decide what window the target is measured over, and what the generator fills
against.

**Technical actions:**

1. Research through `/protocol-training` whether the weekly window is a property of the evidence
   or an artefact of how trials are reported — `weekly-set-volume-for-hypertrophy` and
   `per-muscle-training-frequency` are the notes to read first (standard 15)
2. Decide between a rolling seven-day window, a per-cycle target scaled to the template's session
   count, and keeping a Monday-anchored window for the dose as well as for measurement
3. Record a `TD-###`: the window, what the generator fills against, and what the number means when
   a user takes eleven days to complete a five-session cycle
4. State the interaction with `TD-022`'s band explicitly — the ceiling is defined over the same
   window and moves with it

**Tests:** _(none — this step produces records, not code)_

**Depends on:** none

**Acceptance criteria:**

- The record says which window and why, and names what breaks under the two it rejected.
- Root standard 6 is left intact for measurement: whatever window the dose uses, performed volume
  is still bucketed into Monday-anchored weeks.
- The answer is stated for the slow case — a cycle taken over more days than it has sessions —
  because that is the case a queue makes ordinary.

---

### S5.3 — Research: what happens to volume a missed session did not deliver

**Description:** When a session is carried forward rather than lost, the muscles it would have
trained are behind. Decide whether the next cycle repays that, reports it, or both.

**Technical actions:**

1. Research through `/protocol-training` what the corpus supports about adding volume on top of a
   baseline — `volume-progression-across-a-block` is `contested` and its two failing trials both
   started above 20 weekly sets (standard 15)
2. Decide between reporting the accumulated deficit only, repaying it up to a cap, and repaying it
   in full
3. Record a `TD-###` with the direction and its cost, including what a month of missed sessions
   produces under the chosen rule
4. State how the figure is expressed, so `S5.10` reports a number and not a verdict — the pattern
   `TD-016` already uses for shortfall

**Tests:** _(none — this step produces records, not code)_

**Depends on:** none

**Acceptance criteria:**

- The record says what a deficit does to the next cycle in one sentence a user could be shown.
- If the answer is "reports only", it says why that is not merely the conservative default —
  `ADR-027`'s queue already repairs the cause, which is the argument that makes it sufficient.

---

### S5.4 — What a prescribed slot says

**Description:** The week endpoint returns what a slot trains, the class that decided its numbers,
its movement pattern and equipment, and the volume it credits — derived at read time.

**Technical actions:**

1. Extend the week response so each prescription carries its exercise's muscles with roles, its
   `order_class`, movement pattern and equipment (per `ADR-029`)
2. Return enum names and never display text, and add no user-visible string to the backend
   (standard 3; standard 2 leaves every translated string to the frontend)
3. Derive the fields by joining the stored week against the catalogue, adding no column and no
   migration (per `ADR-029`)
4. Mark each slot as a full slot or one bought above the guaranteed target, so a two-set slot is
   not read as a cut week (per `TD-022`)

**Tests:**

| Artifact | Layer | Test file |
|----------|-------|-----------|
| Week response shape, muscles and roles per slot | Integration | `Protocol.Api.Tests.Integration/Training/GeneratedWeekEndpointsTests.cs` |
| Ceiling slots distinguishable from cut slots | Integration | `Protocol.Api.Tests.Integration/Training/GeneratedWeekEndpointsTests.cs` |

**Depends on:** none

**Acceptance criteria:**

- Every prescription in the response names its primary muscle and its secondary muscles.
- No response field contains a sentence; every one is a code, a number or an enum name.
- A week generated with headroom returns slots of both sizes, distinguishable without inference.

---

### S5.5 — Per-muscle volume against the week's own target

**Description:** The week reports what each muscle group receives, direct and indirect, against
the target the week was generated under — including the muscles that fall short.

**Technical actions:**

1. Add `WeeklyTargetFractionalSets` and `WeeklyCeilingFractionalSets` to `generated_weeks` as an
   additive forward-only migration, and write them when a week is generated (per `ADR-029`
   revision of 2026-08-24, `ADR-003`; standard 10)
2. Backfill existing rows with `6.0` and `6.0` — those weeks predate the ceiling and were built
   to stop at the target (per `ADR-029` revision of 2026-08-24)
3. Compute per-muscle fractional volume from the stored slots, primary whole and secondary half
   (per `TD-006`)
4. Compare against the target stored on the week rather than against today's constant (per
   `ADR-029`, `ADR-003`)
5. Return direct and indirect volume as separate figures, so the half-weighted half is visible
   rather than folded in (per `TD-006`)
6. Keep shortfall and uncovered separate in the response, because they are different failures and
   only one is the user's to fix (per `TD-013`; `WeekPlan` already separates them)
7. Express both figures per **cycle**, which is the window they are now measured over (per
   `TD-024`)

**Tests:**

| Artifact | Layer | Test file |
|----------|-------|-----------|
| Direct and indirect volume per muscle | Unit | `Protocol.Api.Tests.Unit/Training/WeekGeneratorTests.cs` |
| Volume compared against the week's stored target | Integration | `Protocol.Api.Tests.Integration/Training/GeneratedWeekEndpointsTests.cs` |
| A stored week keeps its own target when the constant moves | Integration | `Protocol.Api.Tests.Integration/Training/GeneratedWeekEndpointsTests.cs` |

**Depends on:** S5.4

**Acceptance criteria:**

- A week whose target constant has since changed still reports against the target it was generated
  under — asserted by writing a week, changing the stored value, and reading it back, since the
  constant itself cannot move inside a test.
- Direct and indirect are separately readable for every muscle group.
- A muscle no catalogue exercise trains directly is reported as uncovered, not as a shortfall.

---

### S5.6 — The week screen explains itself

**Description:** The Week section shows what each slot trains, the per-muscle volume against
target, and what a substitution would change.

**Technical actions:**

1. Render each slot's primary and secondary muscles, its class and its equipment, with every
   string coming from the dictionaries (standard 2)
2. Render per-muscle volume against target, marking shortfalls, from `S5.5`'s response
3. Show, per candidate, the equipment and class the endpoint already returns and the component
   discards today, and state that a swap changes the repetition range and rest (per `ADR-029`,
   `ADR-012`)
4. Present no split, ordering or substitution as better for growth (per `TD-003`, `TD-007`,
   `TD-016`)
5. Keep it reachable by keyboard with semantic elements and labelled controls (standard 13)

**Tests:**

| Artifact | Layer | Test file |
|----------|-------|-----------|
| Slot attributes and volume rendered | E2E | `frontend/e2e/week.spec.ts` |
| Both locales carry every new string | Unit | `frontend/lib/i18n/__tests__/dictionaries.test.ts` |

**Depends on:** S5.5

**Acceptance criteria:**

- A reader can answer "why is this exercise here" from the screen alone.
- Every string added exists in `en-US` and `pt-BR`.
- No screen claims a growth advantage for any choice the corpus records as null.

---

### S5.7 — The split becomes a choice

**Description:** The profile carries an optional split, and the generator honours it.

**Technical actions:**

1. Add the nullable split column to `training_profiles` as a forward-only migration (per
   `ADR-030`; standard 10)
2. Resolve null to the mapped default in exactly one place (per `ADR-030`)
3. Offer the templates `S5.1`'s record admits for the profile's frequency, and reject one that
   frequency does not admit with a code (standard 3)
4. Read the resolved split in `SplitTemplate`, leaving stored weeks untouched (per `ADR-003`)

**Tests:**

| Artifact | Layer | Test file |
|----------|-------|-----------|
| Null resolves to the default; a value overrides it | Unit | `Protocol.Api.Tests.Unit/Training/WeekGeneratorTests.cs` |
| A split not admitted by the frequency is rejected by code | Integration | `Protocol.Api.Tests.Integration/Training/TrainingProfileEndpointsTests.cs` |
| Choosing a split on the Profile screen | E2E | `frontend/e2e/profile.spec.ts` |

**Depends on:** S5.1

**Acceptance criteria:**

- An existing profile with no choice generates exactly the week it generated before.
- A chosen split survives regeneration.
- The rejection is a code the frontend translates, never a sentence from the backend.

---

### S5.8 — The plan becomes a queue

**Description:** A generated plan is an ordered list of sessions with no weekday and no anchoring,
and the generator fills against the window `S5.2` decided.

**Technical actions:**

1. Remove weekday assignment and week anchoring from the plan, keeping Monday-anchored bucketing
   for measurement (per `ADR-027`; standard 6)
2. Fill against `S5.2`'s window rather than a calendar week, leaving `TD-022`'s band defined over
   the same window
3. Migrate forward: existing weeks keep what they hold and are not rewritten (standard 10;
   `ADR-003`)
4. Push a plan as one folder per generation, named for when the plan was generated, leaving the
   folder-of-routines shape and the trained-from refusal as they are (per `ADR-031`, `ADR-015`,
   `ADR-017`)

**Tests:**

| Artifact | Layer | Test file |
|----------|-------|-----------|
| A plan has ordered sessions and no dates | Unit | `Protocol.Api.Tests.Unit/Training/WeekGeneratorTests.cs` |
| Performed volume still buckets into Monday weeks | Unit | `Protocol.Api.Tests.Unit/Training/WeekGeneratorTests.cs` |
| An existing stored week still reads | Integration | `Protocol.Api.Tests.Integration/Training/GeneratedWeekEndpointsTests.cs` |

**Depends on:** S5.2

**Acceptance criteria:**

- No generated session carries a date or a weekday.
- A week stored before this step is still readable and is not rewritten.
- The dose window is the one `S5.2` recorded, cited at the line.

---

### S5.9 — A session is done, and the queue advances

**Description:** A session leaves the head of the queue when a workout binds to it, when the user
marks it done, or when the user skips it — and the next session becomes current.

**Technical actions:**

1. Advance on a bound workout, using `routine_id` and nothing else (per `ADR-019`, `ADR-028`)
2. Add an explicit mark for a session nothing bound to, writing nothing into imported history
   (per `ADR-028`; standard 7)
3. Add a skip, stored, which advances the queue without claiming the session happened (per
   `ADR-032`; standard 7)
4. Store only what cannot be derived: binding stays a join on `routine_id`, the mark and the skip
   are declarations and are columns (per `ADR-029`, `ADR-032`)
5. Report which sessions left by binding, which by mark and which by skip, so the binding rate is
   visible and a skip is never read as a completion (per `ADR-028`, `ADR-032`)
6. Fix the coverage denominator: count bound against workouts that had a routine to bind to, not
   against every workout ever imported (per `ADR-019`, revision of 2026-08-24)

**Tests:**

| Artifact | Layer | Test file |
|----------|-------|-----------|
| A bound workout advances the queue | Integration | `Protocol.Api.Tests.Integration/Training/GeneratedWeekEndpointsTests.cs` |
| A mark advances it and writes no history | Integration | `Protocol.Api.Tests.Integration/Training/GeneratedWeekEndpointsTests.cs` |
| A skip advances it and is distinguishable from a mark | Integration | `Protocol.Api.Tests.Integration/Training/GeneratedWeekEndpointsTests.cs` |
| Coverage counts only bindable workouts | Unit | `Protocol.Api.Tests.Unit/Training/WeekComparisonTests.cs` |
| Marking a session on the week screen | E2E | `frontend/e2e/week.spec.ts` |

**Depends on:** S5.8

**Acceptance criteria:**

- A session marked done advances the queue and leaves `performed_workouts` byte-for-byte unchanged.
- A session skipped advances the queue, leaves `performed_workouts` unchanged, and is reported as
  skipped rather than as done.
- Coverage against a history that predates the first push does not report a near-zero rate.
- The three routes out of the queue are distinguishable in the response.

---

### S5.10 — What a muscle has actually accumulated

**Description:** Volume accumulated across cycles is reported per muscle, so a session repeatedly
missed shows up as a number rather than as a week that looks complete.

**Technical actions:**

1. Accumulate performed volume per muscle across cycles from imported history, warm-up sets
   excluded (per `TD-006`)
2. Apply `S5.3`'s record — report, repay to a cap, or repay in full — citing it at the line
3. Report deferred volume apart from skipped volume: one is still ahead in the queue and the other
   will never arrive (per `ADR-032`)
4. Express it as arithmetic and not as a verdict, the pattern `TD-016` already sets for shortfall
5. Count against the window `S5.2` decided, so the accumulated figure and the prescribed one are
   the same kind of number

**Tests:**

| Artifact | Layer | Test file |
|----------|-------|-----------|
| Accumulated volume per muscle across cycles | Unit | `Protocol.Api.Tests.Unit/Training/PerformedVolumeTests.cs` |
| A repeatedly missed session surfaces as a deficit | Integration | `Protocol.Api.Tests.Integration/Training/GeneratedWeekEndpointsTests.cs` |
| The deficit on screen | E2E | `frontend/e2e/week.spec.ts` |

**Depends on:** S5.3, S5.9

**Acceptance criteria:**

- Four cycles in which the same session is skipped produce a visible, growing deficit for the
  muscles that session trains, reported as skipped rather than as deferred.
- The figure is a number against a target, with no adjectival judgement attached.
- Warm-up sets contribute nothing.

---

### S5.11 — The ladder, containerized

**Description:** The full verification ladder from `/protocol-feature`, green, against the
containerized stack.

**Technical actions:**

1. Run all eleven rungs in order, fixing before climbing
2. Rebuild each edited service's image, so a container is not serving old code
3. Seed and read the development database once, since it holds weeks generated under the previous
   model and is the only place that migration is exercised against real data

**Tests:** _(the ladder itself)_

**Depends on:** every step above

**Acceptance criteria:**

- All eleven rungs green, counts recorded in `progress.md`.
- Weeks generated before `S5.8` still read in the running application.
- The tree is clean and every step is committed.

## Specifications

**The week response gains** (`S5.4`, `S5.5`), all derived at read time per `ADR-029`:

| Field | Shape | Source |
|---|---|---|
| `prescription.muscles[]` | `{ muscleGroup, role }`, enum names | catalogue join |
| `prescription.orderClass`, `movementPattern`, `equipment` | enum names | catalogue join |
| `prescription.slotKind` | full or ceiling | slot's set count against `TD-022` |
| `volume[]` | `{ muscleGroup, direct, indirect, target }` | stored slots, target from the week |
| `shortfalls[]`, `uncovered[]` | unchanged shape | already computed |

**`generated_weeks` gains** (`S5.5`), the only schema change in this milestone:

| Column | Type | Why it is stored rather than derived |
|---|---|---|
| `WeeklyTargetFractionalSets` | `decimal` | Not recoverable from the plan: a week holding 6.0 is indistinguishable from one that aimed at 8.0 and ran out of minutes (`ADR-029`) |
| `WeeklyCeilingFractionalSets` | `decimal` | Same, and `TD-022`'s band has two edges |

Existing rows backfill to `6.0` / `6.0`. Everything else a slot explains stays derived at read
time and adds no column.

**Completion** (`S5.9`): a session carries how it completed — bound or marked — and nothing is
written into `performed_workouts` either way (standard 7).

**Error codes** (`S5.7`): a split not admitted by the profile's frequency is rejected with a code
in `TrainingErrorCodes`, never a message (standard 3).

## Dependency order

```
S5.1 ──────────────► S5.7
S5.2 ──────────────► S5.8 ──► S5.9 ──┐
S5.3 ──────────────────────────────► S5.10
S5.4 ──► S5.5 ──► S5.6                │
                                      ▼
        everything ────────────────► S5.11
```

Linearised: `S5.1`, `S5.2`, `S5.3`, `S5.4`, `S5.5`, `S5.6`, `S5.7`, `S5.8`, `S5.9`, `S5.10`,
`S5.11`.

The three research steps lead because a judgement's research comes before anything that consumes
it. `S5.4` through `S5.6` depend on none of them and could run first — they are placed after so
that the milestone's records exist before its code does, which is the order that keeps a recalled
number and a researched one distinguishable.

## Deliverables

- [ ] `S5.1` — which splits a frequency may offer, superseding `TD-003`
- [ ] `S5.2` — the dose window under a queue
- [ ] `S5.3` — what happens to volume a missed session did not deliver
- [ ] `S5.4` — what a prescribed slot says
- [ ] `S5.5` — per-muscle volume against the week's own target
- [ ] `S5.6` — the week screen explains itself
- [ ] `S5.7` — the split becomes a choice
- [ ] `S5.8` — the plan becomes a queue
- [ ] `S5.9` — a session is done, and the queue advances
- [ ] `S5.10` — what a muscle has actually accumulated
- [ ] `S5.11` — the ladder, containerized
- [ ] the verification ladder from `/protocol-feature`, green
- [ ] every capability bullet above covered by at least one step

**Coverage of the capability bullets:**

| Capability | Step |
|---|---|
| Show what a prescribed exercise trains… | `S5.4`, `S5.6` |
| Report direct and indirect set volume per muscle group… | `S5.5`, `S5.6` |
| Say why an exercise fills a slot… | `S5.4`, `S5.6` |
| Choose the split for a training frequency… | `S5.1`, `S5.7` |
| Carry a session that did not happen into the next week… | `S5.8`, `S5.9` |
| Report what a muscle has accumulated across weeks… | `S5.10` |
