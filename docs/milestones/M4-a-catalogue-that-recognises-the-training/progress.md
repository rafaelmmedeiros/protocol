# M4 — progress

**Status:** in progress

One entry per step of `plan.md`, in the plan's linearised order. Git carries what changed; this
file carries what a future session would otherwise rediscover.

### S4.1 — What a default gym contains
- **Status:** completed
- **Tests:** no tests; `check-docs` proves the citations resolve (104 documents, no drift)
- **Observations:**
  - **The plan's closed question was reopened by the record it was superseding, and the engineer
    changed their answer.** `TD-004` did not choose a lean gym arbitrarily: it chose by the
    asymmetry of the failure — assume rich and be wrong and the user silently improvises, which
    corrupts the append-only history invisibly; assume lean and be wrong and the session is merely
    less than ideal. Its own *when to revisit* then named the better answer and said to prefer it
    **when the choice is live**: derive the equipment set from history. `M3` built the derivation
    and `M4` gives it something to derive, so the choice is live and the record's own instruction
    was followed. Reading the record before superseding it is what surfaced this; citing it would
    not have.
  - **`TD-004` held two things and only one of them moved.** It scoped the *catalogue* to the
    assumed gym and it defined the *assumed gym*. `TD-019` separates them: the catalogue models
    every movement including machines, and the assumed gym is unchanged. That separation is the
    whole content of the supersession, and it is why `ExerciseCatalogue.AssumedGym` must not gain
    a machine in any later step.
  - **The circularity that kept `ADR-020` silent is broken by `S4.2`, not by this step.** A logged
    machine exercise implied no equipment because no machine existed in the catalogue to carry a
    requirement. Widening the catalogue breaks it without widening any assumption — which is why
    the default can stay lean and the engineer still ends up with their machines.
  - **Code still cites `TD-004` in nine places and was deliberately not touched.** The plan says
    this step writes no code, and the citations are not yet false: the catalogue *is* still scoped
    to the assumed gym today. `S4.2` is the step that falsifies them, and standard 18 puts the
    correction in the commit that does. `ExerciseCatalogue.cs` lines 14 and 17 are the two that
    will read as lies the moment a machine row lands.

### S4.2 — The vocabulary and the catalogue widen together
- **Status:** completed
- **Tests:** 198 backend unit, 118 backend integration, 21 frontend unit, `check-docs` clean
- **Observations:**
  - **The plan's step boundary was wrong and was corrected rather than worked around.** `M2` left
    `The_vocabulary_holds_nothing_the_catalogue_does_not_ask_for`, which asserts exact equality
    between `EquipmentItem` and what the catalogue asks for. The vocabulary and the rows therefore
    have no order in which both halves are green — items first orphans them, rows first leaves
    requirements the enum cannot express. The invariant is right; the plan was. `S4.3` is merged
    into `S4.2` and its number retired rather than reused, so every later citation still resolves.
    Weakening or skipping the invariant was the alternative and was refused.
  - **The earns-a-row rule needed a fourth term, and the existing catalogue is what proved it.**
    The plan wrote it with three — movement pattern, implement, required equipment. Under that
    version `Squat`/`Sumo Squat` and `Deadlift`/`Romanian Deadlift` are duplicates of each other,
    and they have been in the catalogue since `M1`. What separates them is the muscles they load,
    so the rule reads: **pattern, implement, requirements, or muscle attribution** — never a title
    (standard 9), never an attribute `TD-005` omitted. It is now a test rather than a paragraph.
  - **That test immediately deleted two rows I had already written.** `Hammer Curl (Dumbbell)` and
    `Reverse Curl (Barbell)` are, in everything this model represents, the dumbbell and barbell
    curls already seeded: `M1` tags `Forearms` secondary on *every* curl, so grip is the only thing
    left separating them and `TD-005` omitted grip. The tempting fix was to narrow the existing
    rows' attribution to make room — fitting the model to a conclusion I had already reached. They
    were dropped instead, with the reasoning left at the line where they would have been.
  - **They raise a boundary question this step deliberately did not answer.** The user trains both
    often, so they stay in `S4.5`'s coverage report as movements this model does not represent.
    The real question is whether one domain exercise may carry more than one Hevy
    `exercise_template_id`, which is `ADR-002` territory and a record, not a row.
  - **The split under `ADR-023` is by method, not by field.** C# does not define the order static
    field initialisers run in *across the files of a partial class*, and `All` reads every
    requirement while it builds — the same trap the single file avoided by textual ordering, now
    with no textual order to rely on. Each partial exposes `XRequirements()` and `X()` as methods
    and the core file composes them, which has no order to get wrong.
  - **The engineer chose to correct the attribution, and the research vetoed it — which is the
    gate working rather than failing.** The proposal was to drop `Forearms` from supinated curls so
    the hammer and reverse curls would separate. Standard 15 sent it to the literature first, and
    the literature contradicts the premise: Caufriez 2018 varies nothing but forearm rotation and
    finds brachioradialis activity *slightly higher in supination*; Uysal 2026 finds it exceeding
    the biceps in the eccentric phase **particularly** under a supinated grip. `TD-020` records the
    rejection, `references/grip-and-forearm-involvement-in-elbow-flexion.md` the evidence. Worth
    keeping: the stronger objection was not the vote count but that all three studies measure acute
    activation, and `exercise-variant-and-implementation` already refuses EMG as a proxy for growth.
    Accepting it here would have been selective.
  - **The want behind the request was real and got answered a different way.** Wrist curls train the
    forearm through a joint action no curl reaches at any grip, so they earn rows with nothing
    re-decided — two new `MovementPattern` values, `WristFlexion` and `WristExtension`, kept
    separate because the extensor side of the forearm is not the flexor side.
  - **That changes every generated week, and the change was confirmed behaviourally rather than
    inferred.** `Forearms` has been in `SplitTemplate`'s `Upper`, `Pull` and `FullBody` scopes and in
    `TD-014`'s uniform 6.0 target since `M1`; it simply had nothing direct to draw. A seated wrist
    curl needs only a barbell, plates and a bench, so it is reachable in `TD-004`'s assumed gym and
    the muscle now competes for slots. `The_assumed_gym_week_actually_prescribes_a_direct_forearm_exercise`
    exists because `Forearms` leaving `UncoveredMuscles` only proves the catalogue *can* train it.
  - **`WeekGenerator.Generate` filters by the profile's equipment internally.** The unit tests pass
    `ExerciseCatalogue.All` and still behave as the assumed gym, which is why the uncovered list
    stayed at two rather than dropping to one when the back extension landed — it needs a bench the
    assumed gym does not have. Worth knowing before reading any of these assertions as claims about
    the whole catalogue.
  - **`WeekPlan.UncoveredMuscles` is computed per user, not per catalogue.** Its doc comment
    claimed one fixed list of three muscles. It is derived from the exercises *that user* can
    perform, so widening the catalogue changes it only for a user whose equipment reaches the new
    rows — `SpinalErectors` leaves the list once the back extension is reachable, and an
    assumed-gym user still sees all three.

### S4.4 — What a logged load means
- **Status:** completed
- **Tests:** 202 backend unit (4 new in `ImportedVolumeTests`), `check-docs` clean
- **Observations:**
  - **Volume-load did not exist anywhere before this step.** `PerformedVolume` counted sets and
    nothing counted kilograms, so the step was an addition rather than a correction — which is why
    no existing test failed on the way in. `VolumeLoadOf` and `VolumeLoadByMuscle` sit beside
    `ByMuscle` and share its rules deliberately: working sets only, and `TD-006`'s fractional
    credit. Two quantities counted on different rules cannot be read side by side.
  - **The guard is a test rather than a comment.** `ADR-024`'s rejected option B — inspect
    `Equipment` and double a dumbbell load — is the tempting fix, and it is tempting precisely
    because it is what several logging apps do.
    `A_barbell_and_a_dumbbell_set_at_the_same_weight_count_the_same` asserts both the raw figure and
    the credited one, so the arithmetic cannot start branching on the implement without something
    going red.
  - **A null load is an absence, not a zero.** Bodyweight work stores no weight because the load is
    the body and Hevy does not report it. Such a set contributes no kilograms and still counts as a
    set, asserted in both directions in one test — the two numbers disagreeing there is the correct
    behaviour and would otherwise read as a bug.
  - **A comment in `HevyMappingTests` had gone stale and was corrected here (standard 18).** It read
    "no load until M4 has watched a lift", which by the end of this step implied M4 would start
    pushing loads. M4 fixed what a load *means*; `M5` decides what to ask for.

### S4.5 — How far the catalogue still is
- **Status:** completed
- **Tests:** 205 backend unit (3 new in `DerivedEquipmentTests`), 118 integration, 36 E2E in Docker,
  `check-docs` clean
- **Observations:**
  - **Two counts rather than a percentage, on purpose.** Root standard 3 puts every sentence in the
    frontend, and a proportion is a sentence in numeric clothing: "73%" and "3,798 of 5,186" are the
    same fact, and only the second survives being read by someone deciding what to curate next. The
    backend returns `ExplainedExercises` and `UnexplainedExercises`; the dictionary owns the
    sentence in both locales.
  - **They count logged entries, not distinct movements, and that is why both numbers exist.** One
    movement trained 162 times weighs 162 in the counts and 1 in `TotalCatalogueGaps`. A list of
    twenty names reads identically whether it covers 3% of someone's training or 73% — the counts
    are what separate those, and the list is what says which movements to curate.
  - **The card's render condition was the real change, and the E2E is what proves it.** It used to
    render only when a gap existed, so *full coverage* and *nothing imported yet* looked identical
    on screen — the milestone's success state was invisible. It now renders on the coverage line
    alone, and `catalogue-gap-list` having zero rows beside a visible coverage line is asserted
    together for exactly that reason.
  - **A first E2E assertion was wrong and the feature was right.** `not.toContainText(" 0 ")`
    failed against "We recognise 120 of your 120 logged exercises" — a brittle substring standing in
    for a claim it did not make. Replaced with the render-condition assertion above, which is what
    the step actually changed.
  - **Two claims in `DerivedEquipmentTests` were falsified by `S4.2` and corrected here
    (standard 18).** Both said every catalogue exercise is performable in the assumed gym, and
    concluded that only a user who narrowed their gym can ever receive a suggestion. `TD-019`
    withdrew that scoping; the narrow gym stays in the tests because it is still the sharpest way to
    reach the path, which is a different reason from the one written down.

### S4.6 — Erasing everything of mine
- **Status:** completed
- **Tests:** 205 backend unit, 123 backend integration (5 new in `EraseUserDataTests`), 38 E2E in
  Docker (2 new in `erase.spec.ts`), `check-docs` clean
- **Observations:**
  - **Mapped, not guarded — and that distinction is the whole gate.** With `Development:AllowErase`
    unset the route does not exist and the router answers 404. There is no check inside a handler
    that a later change could relax, and no documented endpoint that politely refuses. The
    integration suite proves the absence because the base `ApiFactory` never sets the switch: it is
    the default configuration answering, not a flag turned off for the occasion.
  - **The frontend asks the API rather than carrying a second flag.** A `DEVELOPMENT_ALLOW_ERASE` on
    the web tier could disagree with the API's, and the disagreement would surface as a button that
    404s. `GET /training/erase` exists only where the POST does, so the page probes it and draws the
    panel on the answer.
  - **The tests assert through the API, which `backend/CLAUDE.md` requires and which is also the
    stronger claim here.** What is being reproduced is *what the product looks like to a new
    account* — tables can be empty while a screen still answers. The one exception reads the context
    directly and says why at the line: the catalogue and the Data Protection key ring have no
    endpoint, and what is asserted is precisely that rows *nobody asked about* were left alone.
  - **A silently-failed setup would have made the erase test pass for the wrong reason**, and nearly
    did: `POST /training/week` is `/training/weeks`, so the week was never generated and there was
    nothing to delete. Every setup call now asserts its own response with the body in the message.
  - **`ADR-025` touches root standard 14, and standard 18 puts that correction here.** Standard 14
    said a reset is the moment to stop and ask; there is now a supported way to get a clean start
    that is not a reset, and a reader who does not know that will still reach for `psql`. The
    standard names the affordance, its switch, and the fact that it expires at `M5`.
  - **The affordance's expiry is written in three places on purpose.** `ADR-025` carries it as a
    record, `EraseUserData`'s doc comment carries it where someone would actually read it, and
    standard 14 carries it where someone looking for permission to reset would land. `M5` starts
    storing judgements Hevy cannot return and no regeneration reproduces, and on that day this is
    no longer adequate.

### S4.7 — The ladder, containerized
- **Status:** pending
