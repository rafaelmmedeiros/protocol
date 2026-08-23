# M2 — Progress

Written by `/protocol-feature` in milestone mode. One entry per step of `plan.md`, in the
plan's dependency order.

**Milestone status:** in progress

### S2.0 — ADR-008 and ADR-009, carried over from M1
- **Status:** completed
- **Tests:** 3 unit added, 1 integration added. Suites green: 58 unit, 35 integration.
- **Not in the plan.** Both records were decided while reviewing `M1` and neither was
  implemented, so the code contradicted two active `ADR`s. Every `M2` step touches the
  generator, so building on top would have baked the wrong behaviour into each step's tests.
  Paid off first, as an `M1` correction, before `S2.1`.
- **Observations:**
  - **Postgres `timestamptz` keeps microseconds; a .NET `DateTimeOffset` keeps 100-nanosecond
    ticks.** The first test failure was `.4825536` against `.4825530` — the POST response
    carried the in-memory value and the later read carried the truncated one. Fixed at the
    source by truncating before persisting rather than by loosening the assertion, so the value
    this endpoint returns is the value that comes back. **This will bite again anywhere an
    in-memory timestamp is compared to a persisted one.**
  - **Existing tests broke and were updated, not weakened.** `ExpectedMonday` moved from
    2026-08-24 to 2026-08-31: the reference date is a Wednesday, so under `ADR-008` the current
    week can no longer hold a split whose Monday has passed. The behaviour changed by decision,
    so the expectation had to.
  - Added a property test rather than only examples: **no week generated on any of fourteen
    consecutive days, at any supported frequency, contains a session before the reference
    date.** The example tests would have passed with a rule that is right on Sunday and wrong on
    Thursday.
  - `ADR-009`'s comparison covers the week start, the sessions and every number on every slot.
    A field added to a prescription and forgotten here would make two different weeks look
    identical — the record names the safe direction (write when in doubt), and this is where it
    would be violated.

### S2.1 — Research: what a preference may override
- **Status:** completed
- **Tests:** no tests — this step produces records, not code
- **Produced:** `references/self-selected-exercise-and-autonomy.md` (thin),
  `references/indirect-only-volume-and-the-coverage-floor.md` (contested), `TD-016`
- **Observations:**
  - **The starvation problem is much narrower than the plan assumed, and that is the finding
    that shapes `S2.3`.** For muscles compounds cover, the evidence that indirect-only volume is
    worse is *contested and possibly absent* — a 7-study meta-analysis finds a trivial estimate,
    and a trained-men trial found nothing. The cost concentrates on **side delts, rear delts and
    calves**, which no compound covers. Excluding curls is cheap; excluding lateral raises is
    not. `S2.3` should not treat all exclusions as equally dangerous.
  - **"Choice makes it more fun" is not supported and the corpus now says so.** Both
    resistance-training choice trials moved *perceived autonomy* hard (3.05 vs 2.19, p<0.001)
    and left enjoyment flat (p=0.72 and p=0.40, with Bayes factors favouring the null). The case
    for honouring preference is that it costs nothing measurable — not that it is more
    enjoyable.
  - **Self-selected *load* has a measured price and self-selected *exercise* does not.** Left to
    choose, trainees pick 53% of 1RM. That is the concrete reason `TD-016` stops preference at
    the exercise and keeps `TD-009`/`TD-010`/`TD-011` prescribed.
  - **Refusing an exclusion was rejected on auditability, not on training grounds** — and that
    is the strongest argument in the record. A refused exclusion becomes an **unlogged skip**,
    converting a shortfall the system can count into one it cannot, and root standard 7 then has
    history recording a plan nobody executed.
  - **No threshold on how much may be excluded, deliberately.** "Fraction of catalogue excluded"
    carries no information — half a catalogue is free if it is redundant variants and fatal if
    it is the only three rows loading rear delts. The quantity that carries information is one
    the generator already computes.
  - **A refinement to `TD-004` and `TD-015` worth knowing before `M3`:** both record that
    deriving preference beats asking, on evidence about *intentions*. An exclusion is not an
    intention — it is a report of past affect, which is the one construct with a demonstrated
    forward link to behaviour. **A stated exclusion is more trustworthy than a stated ranking**,
    and the corpus's scepticism about asking should not be applied to both equally.
  - Provenance: the single-vs-multi-joint meta-analysis effect size and de Franca's percentages
    are secondary-sourced (publishers returned 402/403), as is Ntoumanis' device-assessed
    figure. All flagged in-note.

### S2.2 — Available equipment
- **Status:** completed
- **Tests:** 8 unit (`EquipmentFilterTests`), 8 integration (`EquipmentEndpointsTests`), 1 added
  to `ExerciseCatalogueTests`. Suites green: 66 unit, 44 integration.
- **Produced:** `EquipmentItem`, `ExerciseRequirement`, `UserEquipment`, the 36-row requirement
  table, `GET`/`PUT /training/equipment`, migration `..._EquipmentRequirements`.
- **Observations:**
  - **The backfill is the part no test could have caught.** The seeder is idempotent by external
    template id, so the 36 rows already in the development database would never have been
    touched by the insert — and an exercise with no requirements is unperformable under
    `ADR-013`, so **every existing catalogue would have generated an empty week**. Fixed in the
    seeder rather than a migration, because a migration cannot read the catalogue. Verified on
    the real database: 36 exercises, 63 requirements, 0 without.
  - **Static initialiser order cost an attempt.** `All` is declared before the requirement
    table and C# runs field initialisers in textual order, so `Make` read a null dictionary and
    the type initialiser threw. The tables are now declared above `All` with a comment saying
    why the order matters.
  - **A small gym exposed a real generator defect: an empty training day.** With a
    barbell-and-bench catalogue the first two full-body sessions carried every trainable muscle
    to target, and Friday came out blank. A week now contains only sessions that have work in
    them — padding one would mean prescribing volume above the target, which is the one thing
    the target exists to prevent. Pinned by a property test across three gyms and every
    frequency.
  - **A conflict `S1.6` left behind, found by curating requirements:** the catalogue seeds
    `Preacher Curl (Barbell)` and `TD-004`'s assumed gym never listed a preacher bench. Resolved
    by requiring `AdjustableBench` — an adjustable bench set upright serves — rather than
    inventing an item and quietly widening `TD-004`.
  - **`Bodyweight` is an item, not an empty set.** An empty requirement set cannot be told apart
    from a row nobody curated, and `TD-005` already names miscuration as this catalogue's soft
    spot. A missing row now throws at startup.
  - **`AdjustableBench` does not imply `Bench`.** An inclined movement requires both, which
    avoids an implication rule entirely; someone with an adjustable bench owns both items.
  - **The vocabulary holds only what the catalogue asks for**, asserted by a test. A checkbox
    with no exercise behind it is worse than an absent one — which means **describing a machine
    the catalogue has no exercise for is still impossible.** That is the honest limit of this
    step: equipment currently only *subtracts*. Adding machine exercises is catalogue growth and
    is not in this plan.

### S2.5 — Estimated session duration
- **Status:** completed
- **Tests:** 9 unit (`SessionEstimateTests`), 1 E2E. Suites green: 75 unit, 44 integration,
  21 E2E in Docker, typecheck clean.
- **Observations:**
  - **The estimate is computed from the numbers the prescription actually stores, not from the
    `order_class` they came from.** That distinction matters and is not cosmetic: a week is
    immutable and the records behind it are append-only, so pricing a stored week from
    `TD-011`'s *current* rest values would re-price it under rules it was never generated
    under. There is a test pinning it.
  - **The point of the number is falsifiability, not convenience.** Two of the three terms —
    the 60-second transition and the 180-second warm-up — are engineering estimates with no
    source (`TD-012` says so at the line). A session that reads 52 minutes and takes 70 is the
    first real evidence those constants are wrong, and nothing before this step could produce
    it.
  - **Nothing was stored.** The estimate is derivable from the prescriptions beside it, and a
    derived column can disagree with its own source — the same reasoning `S1.9` used to decline
    `cut_applied`.
  - The session card's meta slot showed a bare prescription count, which carried no
    information. Replaced by the estimate rather than adding a second line.

### S2.3 — Preference: exclusions and preferred variants
- **Status:** completed
- **Tests:** 9 unit (`PreferenceTests`), 8 integration (`PreferenceEndpointsTests`). Suites
  green: 84 unit, 52 integration.
- **Produced:** `ExerciseExclusion`, `PreferredVariant`, `TrainingPreferences`, the generator's
  filter and draw reordering, `GET`/`PUT /training/preferences`, migration
  `..._ExercisePreferences`.
- **Observations:**
  - **A preference has to outrank `order_class` in the *draw* to have any effect at all**, and
    that looked at first like a violation of `TD-016`, which forbids overriding `order_class`.
    It is not: `order_class` does two jobs. It orders the *selection* — which the preference now
    sits above, because asking for dumbbells is asking for the secondary compound — and it
    orders the *session* (`TD-007`), which the preference never touches. There is a test for
    each half.
  - **A preferred variant is validated against its own movement pattern.** Preferring a dumbbell
    press for `Squat` would be stored happily and could never fire, and the user would see a
    generator ignoring what they asked for. Refused with `NotACandidate`.
  - **The exclusion path needed no new shortfall machinery.** Excluding shrinks the catalogue
    exactly as missing equipment does, so a starved muscle already lands in the existing
    reporting. **What is not distinguished is the cause** — "rear delts unreachable" reads the
    same whether the gym lacks a cable station or the user excluded all three exercises. That is
    a reporting gap, not an arithmetic one, and it belongs with `S2.6`'s screens.
  - **`Restrict` on both foreign keys.** An exercise a user has excluded, or prefers, cannot be
    deleted out from under that statement — same reasoning as a stored week pinning its
    exercises (root standard 7).
  - The acceptance criterion is covered end to end and it is the engineer's own case: excluding
    `Overhead Press (Barbell)` yields the dumbbell one, **and the prescription follows the
    exercise across the `order_class` boundary** — 8-12 at 2 RIR instead of 6-10 at 3. `ADR-011`
    predicted this would look like a bug; it is now pinned by two tests so nobody "fixes" it.

### S2.4 — Substituting one exercise
- **Status:** completed
- **Tests:** 9 integration (`SubstitutionTests`). Suites green: 84 unit, 61 integration,
  typecheck clean.
- **Produced:** `GET .../prescriptions/{id}/candidates`, `POST .../substitute`, the shortfall
  finally reaching the API response.
- **Observations:**
  - **The shortfall was being computed and thrown away.** `S1.9` never put it in the response,
    so every week since has silently discarded the one number `TD-008` exists to surface. This
    step needed it — "recompute rather than inherit" is meaningless if nobody can see it — so
    `Shortfalls` is now on the week, **recomputed from the stored prescriptions** rather than
    stored as a column.
  - **A substitution has to land on the same rung of `TD-013`'s ladder as the week it edits**,
    or one slot would rest three minutes in a week where everything else rests ninety seconds.
    The cut level is not stored (`S1.9` declined the column deliberately), so it is **inferred
    from what the week contains**: fewer sets than `TD-008` prescribes means the sets rung, any
    rest below its class's prescribed value means the rest rung.
  - **`TD-015`'s `movement_group` tag is still not needed.** Candidates are exercises sharing
    `movement_pattern` and the primary muscle, filtered by owned equipment and exclusions —
    all of it computable from what `S1.6` already stores, exactly as `ADR-012` predicted.
  - **Slot identifiers are deliberately absent from the week response** and the tests read them
    from the context. The API addresses a slot by id; the screen never needs one. If `S2.6`
    turns out to need them, that is a response change, not a model change.
  - The candidate list is filtered by exclusions too, so an exercise the user refuses is never
    offered as the fix for one they refused.

### S2.6 — The equipment and preference screens
- **Status:** pending

### S2.7 — The ladder, containerized
- **Status:** pending
