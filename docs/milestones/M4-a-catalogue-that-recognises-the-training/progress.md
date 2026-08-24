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
- **Status:** pending

### S4.5 — How far the catalogue still is
- **Status:** pending

### S4.6 — Erasing everything of mine
- **Status:** pending

### S4.7 — The ladder, containerized
- **Status:** pending
