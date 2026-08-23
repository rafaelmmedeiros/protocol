# M1 — Progress

Written by `/protocol-feature` in milestone mode. One entry per step of `plan.md`, in the
plan's dependency order. **Status:** `pending` until the step is started, `completed` when its
tests pass and its acceptance criteria hold.

**Milestone status:** completed

All twelve steps done, every deliverable in `plan.md` ticked, and the verification ladder green
end to end. What the product can do that it could not before: a signed-in user states a goal,
a frequency and a session length, and reads back a week of sessions built from it — where every
number traces to a decision record and none of them was recalled.

Two things the milestone changed about itself, both recorded where they happened rather than
smoothed over: `TD-008`'s volume target was superseded by `TD-014` one step after it was made,
because `S1.5`'s time arithmetic showed an ordinary configuration could never reach it; and the
catalogue stayed flat against a two-level proposal, because the level a parent would add already
exists as `movement_pattern` (`TD-015`).

### S1.1 — Research: training status and the cold start
- **Status:** completed
- **Tests:** no tests — this step produces records, not code
- **Produced:** `references/training-status.md` (contested), `references/cold-start-first-block.md` (thin), `TD-001`
- **Observations:**
  - `TD-001` deliberately does **not** pick a weekly set number. It binds one — 4-12 fractional
    sets per muscle group per week — and `S1.4` chooses inside that bound. A session picking up
    `S1.4` should not read `TD-001` as already having answered it, and a number above 12 may not
    cite `TD-001` as its justification.
  - The research inverted the expected cost of a too-hard first block. Injury was the assumed
    risk and turns out to be small for hypertrophy work (0.24-1 per 1,000 training hours); the
    real cost is adherence — 82% of beginners are gone by six months and the first 28 days
    predict who stays. `S1.5`'s cut rule and `S1.8`'s conservatism inherit that framing.
  - The literature confirmed `ADR-004` on stronger grounds than the ADR itself claimed. The ADR
    treats dropping experience level as a knowing sacrifice; ACSM 2026 does not differentiate
    hypertrophy protocols by status at all, so it is closer to free. No revision made — the ADR
    is not wrong, only more pessimistic than it needed to be.
  - Three figures reached the corpus through secondary summaries (publisher fetches blocked).
    Recorded as a **Provenance caveat** in `training-status.md` rather than left in a transcript.
    Verify at source before any decision turns on one of them precisely.
  - The `references/` and `decisions/` directories did not exist and were created by this step,
    as the skill intends.

### S1.2 — Research: the split
- **Status:** completed
- **Tests:** no tests — this step produces records, not code
- **Produced:** `references/per-muscle-training-frequency.md` (settled),
  `references/split-templates-by-frequency.md` (thin), `TD-002`, `TD-003`
- **Observations:**
  - **The split is not a training decision.** Once weekly volume is equated, frequency has no
    detectable effect on hypertrophy — Schoenfeld 2019 (25 studies), Pelland 2025/2026 (67
    studies), Ramos-Campo 2024 (14 trials, I2=0%). `S1.8` must not present or reason about a
    split as growth-optimal; it is scheduling. This closes the question the step was opened to
    ask.
  - **"Train each muscle 2x a week" cites a superseded paper.** Schoenfeld 2016 found the 2x
    effect *without* equating volume and said so; Schoenfeld 2019 is the same author re-running
    it volume-equated and reversing the conclusion. The 2x recommendation is so widespread that
    a future session will likely reintroduce it as settled evidence. It is convention, and
    `per-muscle-training-frequency.md` says so explicitly.
  - **The generator must count fractional sets** — an indirect set (biceps on a row) counts 0.5.
    A generator counting only direct sets will systematically under-read arm and shoulder volume
    on any push/pull or upper/lower template. This is a hard requirement on `S1.4`'s volume
    accounting and on `S1.6`'s schema, which must therefore model secondary musculature.
  - **`TD-001`'s volume bound turned out not to constrain the split at all.** The step expected
    to find a frequency that could not deliver 12 sets sanely; at 4-12 weekly sets nothing
    between 2 and 6 days approaches a per-session ceiling. The binding constraint at low
    frequency is session *length*, which is `S1.5`'s problem, not this step's.
  - **The status question did not bite where it was expected to.** ACSM 2009 differentiated
    novice from intermediate only in frequency and split organisation — exactly this step's
    subject — so this was the place `TD-001`'s refusal could have cost something. It does not:
    ACSM 2026 dropped the differentiation, and Pelland adjusted for status and still found no
    frequency effect. Recorded in the note rather than left as a silent inheritance.
  - **Two provenance caveats now stand**, one narrowed and one new. Pelland is published
    (Sports Medicine 56:481-505) and its frequency figure was verified at source, so
    `training-status.md`'s caveat was narrowed accordingly. A new one covers Remmert's ~11-set
    per-session ceiling, which is load-bearing for `TD-002`'s rejection of 1 day/week.
  - `TD-002` keeps its two rejections apart on purpose: 1 is rejected on evidence, 7 is a
    product bound with no trial behind it. A reader hitting `FrequencyOutOfRange` on 7 must not
    conclude the evidence forbade it.

### S1.3 — Research: exercise selection and ordering
- **Status:** completed
- **Tests:** no tests — this step produces records, not code
- **Produced:** `references/exercise-selection-within-a-movement-pattern.md` (contested),
  `references/muscle-length-and-exercise-variant.md` (contested),
  `references/exercise-order-within-a-session.md` (contested), `TD-004`, `TD-005`, `TD-006`,
  `TD-007`
- **Observations:**
  - **Selection is arithmetic, not judgement.** Compound vs isolation, machines vs free weights,
    unilateral vs bilateral, varied vs fixed exercises — all null for whole-muscle growth once
    volume is equated. The generator never asks "does this session need an isolation exercise";
    it asks "does every muscle group reach its weekly fractional target". `S1.8` should be built
    that way from the start.
  - **The 0.5 fractional weight got an empirical anchor.** Mannarino 2021 compared a row against
    a curl, matched sets: elbow flexors +5.16% vs +11.06%, a ratio of 0.47. One trial, and
    secondary-sourced — but it turns the convention from arbitrary into shaped. Recorded as
    `TD-006` with the caveat attached.
  - **Deterministic generation costs nothing here.** No evidence supports rotating exercises for
    growth, so `ADR-005` is not paying a training price. The strongest case for variety in the
    literature is *motivation* (Baz-Valle 2019), which is an adherence argument, not a growth
    one.
  - **"Compounds first" is a specificity finding wearing an anatomy costume.** Nunes 2021 found
    order null for hypertrophy (ES 0.03, p=0.862) and that whatever goes first gains most
    strength *at that task* — single-joint-first favoured single-joint strength by ES -0.58,
    larger than the reverse. This is the second time the corpus has caught a widely-repeated
    rule that rests on a different outcome than the one it is quoted for; the first was "train
    each muscle 2x a week". Worth expecting a third.
  - **The engineer's small-muscle-last instinct is compatible with the evidence but is not a
    benefit.** `TD-007` accommodates it and says so explicitly, so a later session does not
    upgrade it into a claimed advantage.
  - **The ordering convention went into `TD-007`, not into a fourth knowledge note.** A
    convention is what we do, not what is known — putting it in a decision record makes the
    format itself state that it is ours, which is stronger than a `thin` tag inside a
    `contested` note. Same laundering risk `S1.2` flagged, handled structurally.
  - **`TD-005` records what is deliberately *omitted* from the schema** — `carry`,
    `lengthened_position`, `stability_demand`, `fatigue_cost`, `difficulty` — with reasons. An
    absent column is invisible, and without this a future session reads each omission as an
    oversight and adds it.
  - **The soft spot of the whole design is `secondary` muscle assignment.** It must mean
    "meaningfully loaded through a substantial range". If two sessions tag the catalogue
    differently every volume number moves, and unlike a wrong constant it will not show in a
    diff. `S1.6` must fix the catalogue once and change it deliberately.
  - **`TD-004` names a hole rather than hiding it:** with no leg curl machine assumed, there is
    no direct `knee_flexion` exercise that accepts a load/RIR prescription, so hamstrings are
    covered by the hinge alone. Written down so `M2` supersedes a stated assumption.
  - The gym assumption was settled by root standard 7, not by training evidence: assume rich and
    be wrong and the user silently improvises, corrupting append-only history invisibly; assume
    lean and be wrong and the session is merely suboptimal. Prefer the assumption whose failure
    is loud.
  - Three provenance caveats carried in (Mannarino percentages, Fonseca's regional reading, Maeo
    per-head figures); one meta-analysis is cited by DOI without authors because it could not be
    opened. All flagged in-note.

### S1.4 — Research: the prescription per slot
- **Status:** completed
- **Tests:** no tests — this step produces records, not code
- **Produced:** `references/weekly-set-volume-for-hypertrophy.md` (settled),
  `references/muscle-group-specific-volume-requirements.md` (thin),
  `references/repetition-range-and-load-for-hypertrophy.md` (settled),
  `references/proximity-to-failure-and-hypertrophy.md` (contested),
  `references/inter-set-rest-and-hypertrophy.md` (contested), `TD-008`, `TD-009`, `TD-010`,
  `TD-011`
- **Observations:**
  - **`TD-008`'s 8.0 is the number to argue with first, and the record says so.** `TD-001` binds
    the first block to the lower half of 4-12; 8.0 is the *top* of that half, while
    `cold-start-first-block` says "near the bottom", which argues for 6.0. Taken on the
    concavity argument — the 8-to-12 step is worth ~1% muscle thickness while 4-to-8 is worth
    much more — with the tension written into the record rather than smoothed over. 6.0 is the
    named standing alternative.
  - **Pelland's volume curve is a square root and has no plateau.** It rises forever with a
    shrinking slope. Half the practitioner world quotes it as identifying an optimum. The corpus
    must never say "volume plateaus at X sets" citing Pelland — only "the marginal set is worth
    less as volume rises".
  - **Per-muscle volume tables have essentially no evidence.** No meta-regression stratifies
    dose-response by muscle group; the one muscle-specific signal favours *higher* volume for
    triceps, the opposite of the usual story and probably an indirect-volume confound. `TD-008`
    is uniform across all 16 muscle groups. If a per-muscle table ever appears, it was invented.
  - **The uniform target lands unevenly, and that is arithmetic not physiology.** Under
    `TD-006`, front delts and triceps reach 8.0 mostly through 0.5s; side delts, rear delts and
    calves only through direct slots. A muscle that cannot reach the 4.0 floor is a **catalogue
    coverage failure to surface**, not a reason to move the target. `S1.6` and `S1.8` both
    inherit this.
  - **The rep-range null is conditional, and our first block breaks the condition.** "5-30 reps
    are equivalent" holds *when sets are near failure*; `TD-010` prescribes 2-3 RIR. A 15-25 rep
    set at 3 RIR is the least-evidenced cell in the whole prescription — tested by none of the
    meta-analyses behind it. That is why `TD-009` caps at 15 rather than 30, and why `TD-009`
    and `TD-010` are explicitly coupled.
  - **The plan's rest criterion resolves to "supported in shape, not in value."** Per-slot rest
    with a descending gradient is well justified and built (180 → 90). The plan's *60-second*
    last slot is not: the one consistent acute finding is that the 1-minute condition loses
    repetitions, in isolation exercises as much as compounds. Floor is 90 s. `TD-011` records
    the conditional resolving, since the criterion was written "if the research supports it".
  - **"Compounds need more rest" is half-wrong as usually justified.** In the best-controlled
    comparison the 1-minute penalty hit the leg extension as hard as the squat, and a light-load
    single-joint condition was the *most* rest-sensitive. `TD-011`'s descending gradient is
    justified on discomfort and load magnitude, not on compound recovery — written that way so
    the wrong justification cannot attach later.
  - **`TD-010` pre-empts an objection `S1.6` onward will otherwise raise:** `TD-005` omitted
    `fatigue_cost`, and a per-`order_class` RIR/rest table is functionally a three-valued
    fatigue proxy. It is defensible because it keys off a column that already exists rather than
    inventing a per-exercise number — stated in the record so a future session applies the same
    test rather than reverting the omission.
  - **The largest unmodelled gap in the whole prescription:** a prescribed RIR may not be the
    RIR performed. Novices misjudge by 4-5 reps, so "2 RIR" may land anywhere from 0 to 6, and
    the system never watches the set. Nothing in the literature covers what an app-prescribed
    RIR produces in the field. Training history import is the first thing that could close it.
  - Provenance: Pelland's results section could not be retrieved, so **no estimated marginal
    mean at a specific set count may be quoted**; Robinson's numeric RIR slope was not obtained,
    so `TD-010` cannot say *how much* growth 3 RIR gives up; the 2024 rest meta-analysis is
    cited without an author list. All flagged in-note.

### S1.5 — Research: the time budget
- **Status:** completed
- **Tests:** no tests — this step produces records, not code
- **Produced:** `references/session-time-cost-of-a-set.md` (thin),
  `references/warm-up-cost-before-resistance-training.md` (contested),
  `references/cutting-training-volume-under-a-time-constraint.md` (thin), `TD-012`, `TD-013`,
  `TD-014` — and **`TD-008` is superseded**
- **Observations:**
  - **This step reopened `S1.4`'s volume number and won.** At a target of 8.0, a user training
    3 x 40 min can never reach it in any arrangement of their time — the full cut ladder lifts
    them to 6.1 per muscle, clearing the floor and permanently under-delivering. `TD-014`
    supersedes `TD-008` at **6.0**, which that configuration reaches exactly once rest is cut to
    the floor. A target an ordinary configuration cannot reach is set in the wrong place. This
    was cheap to correct only because `TD-008` recorded its own tension instead of hiding it.
  - **`TD-008` was superseded, not edited** — status line only, everything else untouched. Weeks
    generated under 8.0 must stay explainable.
  - **Rest is 74-79% of a session's clock**, measured in two trials. Everything else combined is
    a fifth. This makes `TD-011`'s "rest first" ordering far more consequential than `TD-011`
    knew: cutting rest to the floor is worth **+2 slots at every duration from 30 to 90 min**.
    The whole tempo/time-under-tension discourse is irrelevant to a time budget.
  - **A slot costs 7.5 minutes**, and the conversion is linear across the supported range:
    `floor((minutes - 15) / 7.5)` at prescribed rest, `floor(minutes / 7.5)` at the floor.
  - **The plan's forty-vs-fifty criterion passes wide.** 3 x 40 gives 9 slots, 5 x 50 gives 20 —
    a 2.2x difference in weekly volume, not a redistribution. It passes at prescribed rest and
    at floor rest alike.
  - **The cut ladder is not an edge case.** It runs to step 1 in full on 3 x 40 min, an entirely
    ordinary configuration. A user with modest availability trains at floor rest as a matter of
    course.
  - **Shorter sessions do not protect adherence — the association runs the other way.** 22 of 23
    trained women preferred one 46-min session to two short ones, and in a 522,994-user cohort
    longer duration tracked *better* adherence among frequent trainers. The duration maximum is
    therefore a product bound, not an adherence bound, and `TD-013` never cuts frequency.
  - **Supersets are declined, not overlooked.** Worth ~37% of session duration at a pooled
    hypertrophy SMD of -0.05 — but only 3 of 19 studies were chronic, they raise RPE by 1.3
    points into the first 28 days that predict retention, and they need two stations `TD-004`'s
    gym cannot promise. `TD-013` records the refusal so it is not silently rediscovered.
  - **The model's weakest number is the 60 s transition constant — it is invented.** It is also
    the term that grows fastest with slot count. **One logged Hevy workout with timestamps would
    replace it with a measured number**, which is the strongest argument this milestone produced
    for prioritising history import. `TD-012` marks it in-code as an engineering estimate so no
    reader mistakes it for a researched constant sitting beside it.
  - **`S1.8` must recompute credits-per-slot from the real catalogue before trusting `TD-014`.**
    The 4.5 estimate behind its arithmetic predates `S1.6`. The conclusion holds across 4.0-6.0
    credits/slot; outside that range, `TD-014` reopens.
  - Warm-up is omitted on evidence that measured repetitions and never injury — flagged in-note
    as the boundary most likely to be over-read.

### S1.6 — The exercise catalogue
- **Status:** completed
- **Tests:** 6 integration (`ExerciseCatalogueTests`), all passing. Full backend suites green:
  3 unit, 11 integration, no regression.
- **Produced:** `Training/` — `Exercise`, `ExerciseMuscle`, `ExerciseVocabulary` (7 enums),
  `ExerciseCatalogue` (36 seeded exercises), `ExerciseCatalogueSeeder`; migration
  `20260823141301_ExerciseCatalogue`; `TD-015` and three notes from the movement/variant
  question the engineer raised mid-step.
- **Observations:**
  - **The engineer proposed a movement/variant split mid-step and it was researched, not
    waved through.** Verdict: keep the catalogue flat (`TD-015`). The decisive argument was
    structural, not evidential — the "movement" level already exists as `movement_pattern`, and
    eight of eleven attributes would have to live on the variant anyway, including the muscle
    map (which changes by variant: Chaves 2020, incline vs flat, 0.62 cm, p=0.003). A parent
    that cannot carry the attribute justifying it is not an abstraction.
  - **Hevy cannot supply a single attribute we need, and this was verified live, not assumed.**
    It encodes the variant only in the title string (root standard 9 forbids parsing it),
    collapses cable + Smith + selectorised into one `machine` value — leaving `TD-004`'s gym
    inexpressible in its vocabulary — collapses all three deltoid heads into `shoulders`, and
    returns `secondary_muscle_groups: []` on most isolation templates. Importing that map would
    have credited side and rear delts from bench press, breaking `TD-006` invisibly. **The
    catalogue is curated by hand and always will be.**
  - **Three of sixteen muscle groups have no direct exercise under `TD-004`'s gym:**
    `Forearms`, `SpinalErectors`, `Adductors`. All three reach volume only through 0.5-weighted
    secondary roles. This is the coverage-failure case `TD-008` describes, and it is **left
    surfaced rather than patched** — `S1.8`'s floor check is where it must be handled, and
    padding the catalogue to hide it would have been the wrong fix. `KneeFlexion` remains the
    hole `TD-004` already named.
  - **Enums are stored as text, not ordinals.** An ordinal silently changes meaning when a value
    is inserted into the enum, and training history is append-only (root standard 7) — a week
    generated last month must still read correctly.
  - **The seeder is idempotent by `ExternalTemplateId`**, with a test proving identifiers
    survive a re-run. A generated week will reference these ids; re-seeding must never rewrite
    one.
  - **The step's test breaks this tier's "assert through the API" rule, deliberately.** The
    catalogue is seeded reference data with no endpoint in `M1`, and what the test asserts *is*
    the seed contract — precisely what should fail when it stops holding. The exception is now
    written into `backend/CLAUDE.md` so the next such test is not read as one that forgot the
    rule.
  - The MCP `search` filters **client-side within one page** (there are 5). "Not found" never
    means "does not exist" — Preacher Curl only appeared on page 3. Ids were collected by
    paging deliberately, not by trusting a search.
  - `backend/CLAUDE.md`'s layout block gained `Training/` in the same step that created it
    (root standard 18).

### S1.7 — The training profile
- **Status:** completed
- **Tests:** 13 unit (`TrainingProfileTests`), 9 integration (`TrainingProfileEndpointsTests`).
  Full suites green: 30 unit, 25 integration.
- **Produced:** `Training/TrainingGoal`, `TrainingProfile`, `TrainingProfileRules`,
  `TrainingErrorCodes` + `ApiError`, `TrainingEndpoints`; migration
  `20260823..._TrainingProfile`.
- **Observations:**
  - **The goal is received as a string, not bound to an enum, and that is deliberate.** Binding
    to `TrainingGoal` would turn `"powerlifting"` into a framework deserialization failure — an
    error the frontend cannot translate. Parsing it ourselves makes every unrecognised goal
    answer `GoalNotSupported`, which is a code (root standard 3). A goal the schema knows but
    `M1` does not programme for gets the same answer, which is the honest one.
  - **`Enum.TryParse` accepts numeric strings for any underlying value**, so `"7"` parses
    cleanly into an undefined `TrainingGoal`. `Enum.IsDefined` closes it, and there is a unit
    test pinning `"7"` specifically. This would have shipped silently.
  - **`ApiError` carries `Min`/`Max` alongside the code**, so `S1.10` can render "between 2 and
    6 days" without copying `TD-002` and `TD-012`'s bounds into a translation dictionary. A
    range duplicated in the frontend is a range that drifts when the record is superseded — and
    `TD-008` was superseded inside this milestone, so that is not hypothetical.
  - **Validation is a pure static with no I/O**, which is why the bounds every generated week
    stands on are covered by 13 unit tests and no container. The goal is checked before the
    ranges: a profile for a goal we do not programme has no defensible bounds to report.
  - **The development stack was serving pre-`S1.7` code until `api` was rebuilt** — the trap
    `CLAUDE.md` already records, hit again. The migration only reached the development database
    after `up -d --build api`.
  - No rest column, verified in the generated migration rather than assumed (`ADR-007`).

### S1.8 — The generator
- **Status:** completed
- **Tests:** 25 unit (`WeekGeneratorTests`). Full suites green: 55 unit, 25 integration, 0
  warnings.
- **Produced:** `Training/WeekGenerator`, `GeneratedWeek` (+ session, slot, shortfall, cut
  level), `TrainingPrescription`, `SplitTemplate`, `SessionTimeBudget`.
- **Observations:**
  - **`TD-014` was recomputed against the real catalogue and holds.** 36 primaries and 51
    secondaries at 3 sets is `3 x (36 + 51x0.5) / 36` = **5.125 fractional credits per slot**,
    against the 4.5 the record estimated before `S1.6` existed. `TD-014` says its conclusion
    holds across 4.0-6.0, so it does **not** reopen. Pinned as a test, so a catalogue change
    that pushes it out of range fails loudly instead of silently invalidating the record.
  - **Two real design defects, found by the tests and fixed at the root** — neither test was
    weakened:
    - **The cut ladder was chasing a gap no cut can close.** The floor was checked against all
      16 muscle groups, but three have no direct exercise under `TD-004`'s gym, so `MeetsFloor`
      was never true and *every* week climbed to the last rung: rest always at the floor, and
      40 vs 90 minutes producing identical volume. Fixed by splitting `Shortfalls` (time-budget
      gaps, the user can act) from `UncoveredMuscles` (catalogue gaps, only `M2` can act). They
      look identical in the data and are different problems.
    - **Greedy filling starved later sessions.** A muscle's whole weekly target could be spent
      in session one, so the second Push day of a 6-day split generated **zero slots**. Fixed by
      spreading each muscle's target across the sessions that can train it — which is also what
      makes per-muscle frequency land at 2-3x, the thing `TD-003`'s templates exist for.
  - **One test was wrong and was corrected, not the code.** "Every muscle trained at least twice"
    failed on `FrontDelts`, which reaches target almost entirely through indirect credit from
    pressing. That is `TD-006` working as designed. Counting only direct slots asserted something
    the records never ask for, and would fail precisely on the muscles the fractional scheme is
    built around. Now counted over any credited role.
  - **The additive time model was implemented rather than `TD-012`'s 7.5-minute shortcut.** The
    shortcut assumes a representative slot ordering and a greedy fill does not guarantee one.
    Warm-up is reserved for every session rather than only those containing a primary compound —
    slightly conservative, and `TD-012` says over-predicting is the safe direction.
  - **Four sessions of an hour reaches the floor with `CutLevel.None`**, asserted as a test: if
    the product's best-served configuration ever needs the ladder, either the time model or the
    weekly target is wrong.
  - Purity verified by grep, not by intent: the five generator files have **zero `using`
    directives** — no EF, no HTTP, no clock. The reference date is a parameter precisely so the
    week can be asserted whole (`ADR-005`).

### S1.9 — Persisting a generated week
- **Status:** completed
- **Tests:** 9 integration (`GeneratedWeekEndpointsTests`). Full suites green: 55 unit, 34
  integration, 0 warnings.
- **Produced:** `GeneratedWeek`/`GeneratedSession`/`GeneratedPrescription` entities, migration
  `20260823..._GeneratedWeek`, `POST /training/weeks`, `GET /training/weeks/current`.
- **Observations:**
  - **A name collision forced a rename, and the rename is an improvement.** `S1.8`'s pure output
    was called `GeneratedWeek`, which is what the plan's *table* is called. The generator's
    result is now `WeekPlan`/`PlannedSession`/`PlannedSlot` and the entities took the storage
    names. Worth keeping distinct anyway: one is what the generator computed, the other is what
    was written down and can never change.
  - **`WeekNotFound` was added to the error catalog**, which the plan did not list. `GET
    /training/weeks/current` has to answer something when no week exists, and `S1.11` needs to
    tell "no week yet" apart from a failure — the plan itself requires an empty state rather
    than an error there.
  - **`cut_applied` was deliberately not stored**, though `ADR-003`'s "explainable" reasoning
    argues for it. It is derivable from the stored rest values, so a column would be redundant
    data that can disagree with its own source. If `S1.11` turns out to need it, a forward-only
    migration is cheap.
  - **The exercise foreign key is `Restrict`, not `Cascade`.** An exercise a stored week
    references cannot be deleted out from under it — history is append-only (root standard 7),
    and cascading would let a catalogue edit silently rewrite what a user trained.
  - **One test reads the context rather than the API, for a reason the API cannot cover.**
    "Generating twice leaves two weeks, the first unchanged" is a claim about storage, and no
    endpoint exposes a week that is no longer current. The alternative was inventing
    `GET /training/weeks/{id}` that nothing needs. Same documented exception as
    `ExerciseCatalogueTests`, already written into `backend/CLAUDE.md`.
  - The other half of that criterion **is** provable through the API and is: generate at 3 days,
    edit the profile to 6, and `GET current` still answers 3. That is `ADR-003`'s snapshot doing
    its job.
  - No `weight_kg` column: `M1` prescribes nothing about load, so it would be a field nothing
    writes.

### S1.10 — The Profile section
- **Status:** completed
- **Tests:** 3 unit (`lib/__tests__/duration.test.ts`), 5 E2E (`e2e/profile.spec.ts`).
  Typecheck clean; Vitest 13; containerized E2E 14 passed.
- **Produced:** `app/(app)/profile/` (page, form, Server Function), `lib/duration.ts`,
  `lib/api-error.ts`, `components/ui/select.tsx`, both dictionaries, nav entry.
- **Observations:**
  - **A function cannot cross from a Server Component into a Client Component**, and the first
    draft passed `errorFor(state)` as a prop. The fix is better than the workaround would have
    been: the **Server Function resolves the sentence itself**, because it runs on the server
    where the dictionary lives. The form never sees a code, and root standard 3 is honoured by
    construction rather than by discipline.
  - **The bounded error sentences take their bounds as arguments** —
    `FrequencyOutOfRange: (min, max) => ...`. Because `Dictionary` is derived from `en-US`, the
    compiler forces `pt-BR` to match the *signature*, not just the key. The numbers `TD-002`
    and `TD-012` decided are never copied into this tier, so superseding a record moves the
    sentence with it.
  - **`DurationOutOfRange` needs a unit conversion in the error path**, which is easy to miss:
    the backend bounds duration in seconds and the screen speaks minutes, so the sentence would
    have read "between 1500 and 7200 minutes". Converted where every other rendered unit is
    converted (root standard 4).
  - **Our error shape is not Identity's**, so `lib/problem.ts` does not apply. `lib/api-error.ts`
    reads `{ code, min, max }`. Two shapes on one API is a real thing to know before writing
    the next screen.
  - **The proxy route has no `PUT` handler** — only GET, POST and DELETE. Not hit here because
    a Server Function goes server-to-server, but the next feature that writes from the browser
    will find it. Reported, not fixed: nothing needs it yet.
  - **A styled `<select>` was extracted to `components/ui/select.tsx` and added to
    `/template`.** The tier's invariant is that the style guide renders the real components, so
    a one-off select inline would have made the guide and the product disagree — which that
    invariant defines as a bug in one of the two.
  - `ADR-004`'s "collect the goal, programme one value" is rendered literally: all four goals
    are listed and three are `disabled`. Hiding them would make the field look arbitrary and
    the roadmap invisible.
  - E2E was run against the **containerized** stack rather than `npm run test:e2e`, which
    points at the development database and leaves accounts behind (`W6`). Slower, and it keeps
    the development data clean.

### S1.11 — The generated week on screen
- **Status:** completed
- **Tests:** 8 unit added (`week.test.ts`, `duration.test.ts`), 6 E2E (`e2e/week.spec.ts`).
  Typecheck clean; Vitest 21; containerized E2E **20 passed, no flakes**.
- **Produced:** `app/(app)/week/` (page, Server Function, generate form), `lib/week.ts`,
  `splitDuration` in `lib/duration.ts`, both dictionaries, nav entry.
- **Observations:**
  - **A flaky test from `S1.10` surfaced here and was a real defect in both the test and the
    UI.** `fillProfile` waited for the "saved" message, which **survives the previous save** —
    so on a second submit the assertion passed instantly and the reload raced a write still in
    flight. Fixed twice over: the helper now waits for the Server Function's actual round trip,
    and the form hides the confirmation while a save is pending. A "saved" message beside a
    pending submit claims something untrue to the user as well as to the test.
  - **Day names come from `Intl`, not from the dictionary.** The API sends `"Monday"` as a
    stable name and the page resolves it to a real date against the week's Monday, so the
    dictionary carries six session kinds rather than seven weekdays per locale — and the screen
    shows the actual date, which is more useful.
  - **The date is built and formatted in UTC deliberately.** `new Date("2026-08-24")` is
    midnight UTC; formatting that in a zone behind UTC renders the day before, which would make
    the training week silently start on Sunday for any reader west of Greenwich. There is a
    test pinning it.
  - **Two absences are told apart, and that is the whole design of the screen.** No profile
    means there is nothing to generate *from* (link to the profile); a profile with no week
    means nothing generated *yet* (the generate button). Collapsing them into one empty state
    would blame the user for the first case.
  - `Button` has no `asChild`, so the first draft's `<Button asChild><Link/></Button>` was
    wrong. Navigation is a link and now looks like one — no new component invented for it.
  - Regenerating is deliberately **not** idempotent (`ADR-003` writes a new row every time), so
    the control says "generate again" rather than "refresh".

### S1.12 — The ladder, containerized
- **Status:** completed
- **Tests:** all eleven rungs, in order. Backend 55 unit / 34 integration (host **and** in
  Docker), frontend 21 unit, E2E 20 in Docker, 0 warnings, 66 documents no drift.
- **Observations:**
  - **`W6` proved by a count, not by a green suite.** The development database held **8
    accounts before the ladder and 8 after**, and **zero rows in `generated_weeks`** — despite
    the E2E run generating a week in six different tests. The test stack owned every one of
    them. A passing suite says the tests work; only the count says they stayed out of the
    development data, and those are different claims.
  - **Rungs 3/4 and 10 are the same suites and both were run.** They are not redundant: the
    host run uses the machine's SDK and Testcontainers, the Docker run is what actually ships.
    Both green, same numbers.
  - **Standard 15 verified mechanically, and the result confirms the design rather than just
    passing.** The only bare numbers left in `WeekGenerator` are calendar arithmetic, a
    zeroed accumulator, loop indices and `deficit > 0`. Every training judgement lives in
    `TrainingPrescription` beside its `TD-###` — the generator carries arithmetic and the
    records carry the judgements.
  - **The corpus index is complete**, checked file-by-file rather than by eye: 18 notes and 15
    decisions on disk, 18 and 15 rows in the index, nothing unindexed.
  - No new Docker trap surfaced in this milestone. The one that did bite repeatedly is already
    documented: **a container keeps serving old code until its image is rebuilt** — hit at
    `S1.7`, `S1.9`, `S1.10` and `S1.11`, every time after a migration or a route was added.
