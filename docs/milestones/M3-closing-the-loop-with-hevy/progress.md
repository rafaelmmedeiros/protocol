# M3 — progress

**Status:** in progress

One entry per step of `plan.md`, in the plan's linearised order. Git carries what changed; this
file carries what a future session would otherwise rediscover.

### S3.1 — The Hevy connection
- **Status:** completed
- **Tests:** 4 unit (`HevyKeyProtectionTests`), 9 integration (`HevyConnectionEndpointsTests`)
- **Observations:**
  - **`IHevyClient` had to be born in this step, not `S3.2`.** The plan lists `S3.1` as depending
    on nothing, but validating a key before storing it (`ADR-014`) needs a call to Hevy. The seam
    was introduced here holding only `CheckKeyAsync`, and `S3.2` grows it. The dependency order in
    the plan still holds; what was wrong was the implication that `S3.1` touches no client.
  - **The restart trap is testable in-process and worth the machinery.** `ADR-014` names an
    ephemeral key ring as the failure mode, and a second `WebApplicationFactory` pointed at the
    same Testcontainers database *is* a restart. `A_stored_key_still_decrypts_after_the_host_restarts`
    fails loudly without `PersistKeysToDbContext`. This was cheaper than waiting for rung 7 to
    catch it in `S3.8`.
  - **`SetApplicationName` is not optional and would have been missed.** Data Protection otherwise
    derives the name from the content root, which differs between the container and a test host —
    so a ring correctly persisted to the database would still fail to decrypt under a different
    name. The restart test caught this shape of problem, not the discipline.
  - **The key ring lives in the database rather than on a volume.** Chosen so a restored backup
    brings its own keys: a filesystem ring and a database can drift apart, and the only symptom is
    that nothing decrypts. This is a decision `ADR-014` left open, recorded in the context and in
    `backend/CLAUDE.md` rather than as a new record.
  - **`ApiFactory` now substitutes Hevy for the whole suite.** That makes `S3.8`'s "no test run
    touches the real Hevy account" true by construction instead of by everyone remembering.
  - **Two plan corrections, made rather than deferred.** The connection is its own table instead of
    two columns on the user, and the key is text instead of bytes. Both are recorded in the plan's
    Specifications with the reasons.
  - **Not built, and deliberately: there is no disconnect.** The capability is "connect a Hevy
    account", `DELETE /hevy/connection` is outside the plan's technical actions, and adding it here
    would be scope this step was not given. Reported rather than fixed.

### S3.2 — The Hevy boundary
- **Status:** completed
- **Tests:** 27 unit (`EffortConversionTests`, `HevyMappingTests`, `BoundaryIsolationTests`)
- **Observations:**
  - **A test asserted something false and the code was right.** `The_half_points_and_the_floor_do
    _not_round_trip` failed on RPE 6, because 6 is a whole point: 6 → 4 → 6 closes arithmetically.
    The loss at that anchor is **semantic** rather than numeric — Hevy words it "4+ more reps", so
    it is a floor with no ceiling. Corrected the test, and split it so the distinction is written
    down rather than implied.
  - **The inbound mapper refuses an unmodelled set type rather than defaulting to working.**
    Counting a `cluster` or anything else as a working set would inflate every fractional volume
    figure (`TD-006`) silently. `ADR-018` retains the raw payload, so failing loudly loses nothing
    and a wrong guess would. `S3.4` has to decide what a sync does with a workout that fails to
    map — skip and record, or stop — and this step deliberately does not.
  - **`BoundaryIsolationTests` is reflection over the assembly, not a lint rule.** It walks every
    property, field, method parameter and return type of `Protocol.Api.Training` and fails if any
    resolves into `Protocol.Api.Hevy`, unwrapping arrays, generics and tasks on the way. The
    second test bans the substring "rpe" in any domain symbol. Both would have passed trivially
    today and exist for the commit that would not.
  - **`PerformedWorkout` lives in `Training/`, not in `Hevy/`.** Training that happened is domain,
    so the dependency runs Hevy → Training and never back. Its EF configuration and migration are
    `S3.4`'s; this step defines the shape the mapper produces.
  - **Correlation needed no invention.** Standard 12 is satisfied by the `Activity` ASP.NET Core
    already creates and `HttpClient` already propagates as `traceparent`. A second identifier
    would have given one request two names.

### S3.3 — Pushing a week
- **Status:** completed
- **Tests:** 15 unit (`RoutinePayloadTests`, `RoutineNoteTests`), 9 integration (`PushWeekTests`)
- **Observations:**
  - **The plan had a missing dependency, and it was real.** Action 5 needs to know whether a week
    has been trained from, and nothing recording imported training existed — the plan gave that to
    `S3.4`. Resolved by landing the performed-training schema here, because this step must query
    it; `S3.4` adds the version and tombstone columns and the sync that fills them. Recorded in the
    plan under `S3.3` and `S3.4` rather than left as a surprise.
  - **`ADR-017`'s second branch turned out not to need a branch.** `ADR-009` already makes a
    regenerated week a new row, and a new row has no folder, so it takes the create path on its
    own. What detection is actually for is the *same* week pushed again after training — where
    replacing breaks the join forward and creating fresh routines orphans it backward. Refusal with
    `WeekAlreadyTrainedFrom` is what replaced it, recorded as a `Revisions` bullet.
  - **The push saves after every write, on purpose.** The folder identifier is stored before any
    routine is created, and each routine identifier as it comes back. An interrupted push therefore
    resumes into the same folder and replaces the routines it already made, rather than creating a
    second folder and duplicating. This is what makes `ADR-021`'s "safe to retry" true rather than
    aspirational, and it costs one round trip per session.
  - **`StubHevyClient` became a recording stub and moved to its own file.** Push is only observable
    through what Hevy received — asserting on our stored identifiers would prove we saved
    something, not that we sent the right thing.
  - **`PerformedTraining` had to adopt the codebase's entity pattern.** `IReadOnlyList` with
    `required` does not map cleanly; the aggregate now mirrors `GeneratedWeek`/`GeneratedSession`
    with an `Id` and a foreign key per level. The ordering guarantee moved from the collection type
    to `Position`, which is where it belonged anyway since EF does not preserve insertion order.
  - **The routine note is the only display text the backend composes**, and the locale is carried
    explicitly in the push body rather than sniffed from a header — the string is unusual enough
    that what decides it should be as visible as it is.

### S3.4 — Importing history
- **Status:** completed
- **Tests:** 7 unit (`ImportedVolumeTests`), 16 integration (`ImportHistoryTests`, `ReconciliationTests`)
- **Observations:**
  - **A test caught a real bug, and it was the C# nullable-comparison trap.** The cursor advanced
    with `at > connection.SyncCursor`, and a lifted comparison against a **null** operand is
    `false` — so the null cursor, which is every first sync, never moved and every sync would have
    re-read the whole history from the epoch forever. Nothing else would have shown it: the
    duplicate-suppression check made the re-read a silent no-op, so the only visible symptom was a
    slow sync. `The_cursor_advances_so_the_next_sync_asks_for_less` exists because of it.
  - **The open question from `S3.2` is answered, and retention is what made the answer cheap.**
    The payload is stored **before** it is mapped, so a workout carrying an unmodelled set type is
    still captured; the sync records the reason, counts it as `Unmapped`, advances the cursor and
    continues. Aborting or holding the cursor would have let one odd workout block every future
    sync permanently, with the offending data sitting in a third party's account where the user
    could not fix it. Recorded as an `ADR-018` revision.
  - **The feed's cursor is inclusive, which makes idempotence the importer's job.** "At or after"
    means the last event of one sync is the first event of the next, so an update whose
    `updated_at` already has a row is skipped and a deletion whose latest version is already a
    tombstone is skipped. Without that, every sync would append a duplicate of the boundary event.
  - **A tombstone carries no exercises.** It says "this stopped counting"; the sets it used to
    carry stay readable on the version below it, which is root standard 7 in its literal form.
  - **`PerformedVolume` had to exist here rather than in `S3.5`.** The acceptance criterion is that
    a deleted workout *stops counting toward volume*, which is unassertable without the arithmetic.
    It reuses the generator's own credit constants, because planned and performed must be counted
    the same way or the comparison compares two different quantities.

### S3.5 — Prescribed against performed
- **Status:** completed
- **Tests:** 18 unit (`WorkoutBindingTests`, one added to `ImportedVolumeTests`), 8 integration (`ComparisonEndpointsTests`)
- **Observations:**
  - **The comparison lives in `Training/`, not in `Hevy/`.** Both sides of it are ours — our
    prescription against our performed training — and that the second arrives through Hevy today is
    an import detail the builder does not know. The join reads a column of ours that happens to
    hold their identifier (standard 8), and the endpoint is `/training/weeks/{id}/comparison` for
    the same reason: on the day the logging surface is ours, this file does not change.
  - **The outcome code says where the repetitions landed and nothing else.** `InRange`,
    `AboveRange`, `BelowRange`, `Mixed`, `NotPerformed` — deliberately not a judgement about
    progress. What a sequence *means* is the `M4` decision the research left open, and a read model
    that pre-empted it would smuggle a training judgement in without a record.
  - **Performed exercises are consumed as slots claim them.** Two slots prescribing the same
    exercise each take their own performed entry rather than both reading the first; whatever is
    left over becomes an `Extra`. Hiding extras would make the screen a claim about the plan rather
    than a record of the day.
  - **`The_performed_sequence_keeps_its_order` hands the sets over shuffled on purpose.** EF does
    not preserve insertion order, so the guarantee has to come from `Position` — asserting it
    against already-ordered input would have proved nothing.
  - **The binding rate is reported as data.** `ADR-019` chose the narrow join and named this number
    as what would justify revisiting it. Every workout observed from the real account so far
    carries `routine_id: null`, so this may well be the number that reopens the record — and it
    will do it with a measurement rather than a hunch.

### S3.6 — Equipment the history reveals
- **Status:** completed
- **Tests:** 8 unit (`DerivedEquipmentTests`), 9 integration (`DerivedEquipmentEndpointsTests`)
- **Observations:**
  - **A finding that changes what this feature reaches: every exercise in the catalogue is
    performable in the assumed gym**, because the catalogue was built for it (`TD-004`). So a
    suggestion can only ever arise for a user who **narrowed** their equipment set — a user on the
    default receives none, by construction. The real account's own case,
    `Iso-Lateral Row (Machine)`, arrives as a **catalogue gap** rather than as a suggestion,
    because that exercise is not in our catalogue at all. The gap path is doing most of the work
    today, and the suggestion path is real but narrow. Four tests failed on this before it was
    understood, and they were the tests being wrong rather than the code.
  - **Accepting had to seed from the *effective* set, not from the stored rows.** The equipment
    resolver reads `rows.Count == 0 ? AssumedGym : rows`, so a user who never opened the equipment
    screen has no rows. Writing a single row for the accepted item would have replaced a whole gym
    with one machine — a catastrophic **narrowing** from a feature whose entire premise is that
    inference only ever adds (`ADR-020`). Caught by reading the resolver before writing the
    endpoint, and `Confirming_widens_the_set_and_never_narrows_it` is what keeps it caught.
  - **Declining is stored, not just ignored.** A `declined_equipment_suggestions` row per user per
    item, so an offer the user refused does not return on every sync. `ADR-020` names the reason:
    a suggestion that keeps coming back is one the user learns to dismiss without reading.
  - **There is no removal path to test**, because there is none to write. `Absence_from_the_history
    _never_removes_anything` asserts the shape of the function rather than a behaviour — not
    training something is far more often evidence it was never programmed than evidence the
    equipment is gone.

### S3.7 — The screens
- **Status:** completed
- **Tests:** 21 frontend unit (`locales.test.ts` covers the new keys), 36 E2E green in Docker
  (`hevy.spec.ts` new, `equipment.spec.ts` extended)
- **Observations:**
  - **The E2E runs the real API container, so Hevy had to be faked *inside* it.** The unit and
    integration suites substitute `IHevyClient` in process; a container cannot. `FakeHevyClient` is
    registered only when `Hevy:UseFake` is set, and only `docker-compose.test.yml` sets it. It is
    deliberately not a null object — it **synthesises a workout from each routine it was given**,
    with repetitions falling across the sets, because a fake that accepted writes and returned
    nothing would let the milestone's most important test pass without the loop ever closing.
  - **A Server Function that revalidates its own page wipes the state it just returned.** `pushWeek`
    called `revalidatePath("/week")`, the re-render reset `useActionState`, and the confirmation
    vanished — one E2E went flaky on exactly that. The revalidation bought nothing: a push stores
    routine identifiers the screen never renders and leaves the comparison untouched. Removed, with
    the reason at the line, and promoted to `frontend/CLAUDE.md`.
  - **Fixing that flake immediately created its mirror image, and it is the `M1` failure again.**
    With the message no longer wiped, a second press finds the *previous* outcome and passes
    without the second press having happened. An E2E press now waits for the round trip **and**
    the outcome, with the subscription opened before the click. Also promoted.
  - **`data-testid` on `Card` type-checks and never reaches the DOM.** Our `Card` and `Pill` take
    named props only, and TypeScript waves hyphenated JSX attributes through without an excess
    property error — so the identifier compiles, renders nothing, and the E2E fails on an element
    that looks present in the source. Caught before the suite depended on it. Promoted.
  - **The comparison lives on the week screen rather than its own route**, because the acceptance
    criterion is that a session shows prescribed and performed *without navigating*. The loop
    controls and the comparison both appear only once an account is connected, so `M1` and `M2`
    still work untouched for a user who never connects one — asserted by its own E2E.

### S3.8 — The ladder, containerized
- **Status:** pending
