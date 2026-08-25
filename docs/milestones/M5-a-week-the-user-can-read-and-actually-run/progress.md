# M5 — progress

Status: `in-progress`

One entry per step of `plan.md`, in the plan's linearised order. The `Observations` line is the
point: git carries what changed, this carries what a future session would otherwise rediscover.

### S5.1 — Research: which splits a frequency may offer
- **Status:** completed
- **Tests:** no tests — this step produced records
- **Observations:**
  - **`ScopeOf(Push)` and `ScopeOf(Pull)` union to exactly `ScopeOf(Upper)`**, which is why
    `Upper / Lower / Push / Pull / Legs` lands on precisely 2x for every upper-body group and not
    on a ragged 1x-3x. Nothing states that anywhere in `SplitTemplate` and nothing enforces it: a
    future session moving one muscle between Push and Pull — rear delts are the obvious candidate
    — silently drops that template below the floor this record measured it against.
  - **`Upper / Lower` at two sessions was measured rather than assumed, and it fails uniformly**:
    1x to all sixteen groups. `TD-003` mapped two days to full body without recording the
    arithmetic, so the two-session row now has evidence under it rather than a convention.
  - **The `P12` gate paid immediately.** The nine per-muscle figures in `TD-023` came from walking
    each candidate cycle through the real `ScopeOf` in a throwaway probe, not from reasoning about
    the table. Writing them by hand would have been plausible and unverified — which is exactly
    how `TD-021` was written the day before and superseded within the hour.
  - **`TD-003`'s rest distribution died with `ADR-027` and nothing replaced it.** That record
    refused to stack sessions Mon-Tue-Wed with four days off; there are no weekdays now, so the
    user's own spacing decides it. `TD-023` records the loss rather than papering over it, and the
    corpus cannot price it — every trial equates volume and reports frequency per week.

### S5.2 — Research: the dose window when the plan is a queue
- **Status:** completed
- **Tests:** no tests — this step produced records
- **Observations:**
  - **The question was decided by `ADR-005`, not by the literature.** Both alternatives — a
    rolling seven-day window and a Monday-anchored dose window — require the generator to know how
    fast the user will train or which sessions land in which calendar week. Both are future
    behaviour, and a generator that reads them stops being a pure function of profile and
    catalogue. The corpus could not have settled this: it says plainly that **volume as prescribed
    is not volume as performed** and that no trial models a user who does not complete it.
  - **`TD-023` had already answered it and nobody noticed, including the plan.** A cycle holds
    exactly as many sessions as the declared frequency in every row of that table, so a cycle *is*
    the declared week and no number had to move — only what the numbers attach to. The plan framed
    this as choosing between three windows; the real work was seeing that one of them was already
    there.
  - **That property is load-bearing and unenforced.** If a future template ever holds a session
    count different from its frequency, `TD-024`'s central claim quietly stops being true and
    nothing fails. `S5.8` carries the test.
  - **The compressed case is the sharp one and it was nearly missed.** The stretched cycle —
    eleven days, ~3.8 sets a week — is the obvious worry and it is merely slower progress. The
    mirror is worse: a two-session template consumed three times in a calendar week reaches 18
    fractional sets, above the ~12 where the meta-analyses stop agreeing, and it means `TD-022`'s
    "8.0, exceeded nowhere" no longer bounds a calendar week. Reported rather than blocked,
    because blocking it is a decision that should be taken against an observed case.

### S5.3 — Research: what happens to volume a missed session did not deliver
- **Status:** completed
- **Tests:** no tests — this step produced records
- **Observations:**
  - **The step's own premise was wrong and the plan carried the error.** It asks what happens to
    volume a missed session "did not deliver", which presumes volume was lost. Under `ADR-027`
    nothing is lost: the untrained session is carried forward and its volume arrives late. The
    real question was never repay-or-report — it was noticing there is nothing to repay, and that
    the deficient quantity is the **rate**, which `TD-024` had already made reportable the step
    before.
  - **The queue's effect on the engineer's own case is a change of kind, not of degree.** With a
    week regenerated every Monday, a user completing three of four sessions starves whichever
    session sits fourth — permanently, invisibly, and concentrated on those muscles. A queue
    rotates it (`S1 S2 S3` / `S4 S1 S2` / `S3 S4 S1`), so every muscle reaches the full cycle dose
    at three quarters of the declared pace. A systematic per-muscle deficit becomes a uniform rate
    deficit.
  - **A gap in the plan surfaced and is not filled here: skipping is undefined.** `S5.9`'s actions
    describe a session completing by binding or by an explicit mark, and neither is skipping.
    `S5.10`'s acceptance criterion assumes a session that "never completes" over four cycles —
    which a strict queue cannot produce, because it would simply stall there and nothing else
    would ever be trained. Either the queue can be advanced past a session or that criterion is
    unreachable, and `TD-025` is explicit that its central claim only holds while sessions are
    completed in order. **Reported rather than decided**: it changes what `S5.9` builds, and the
    skill says a wrong plan is revised through `/protocol-milestone`, not patched mid-build.
  - **The evidence pointed the right way for the wrong reason and the record says so.** Enes 2024
    and Barsuhn 2024 both argue against adding sets, but both started above 20 weekly sets and we
    prescribe 6.0 — the corpus itself calls that "a null in a region we never enter". What
    actually decides it is that a repayment is a catch-up above target for someone who has just
    demonstrated less capacity than they declared, which is `cold-start-first-block`'s
    over-prescription failure exactly.

### S5.4 — What a prescribed slot says
- **Status:** completed
- **Tests:** 14 integration in `GeneratedWeekEndpointsTests` (3 new), backend build clean
- **Observations:**
  - **The response record's own property shadowed the enum it needed.** Naming the field
    `SlotKind` on `GeneratedPrescriptionResponse` made `SlotKind.Ceiling` resolve to the property
    rather than to the type, inside the record's own body. The fix was better than a rename: the
    rule moved to `TrainingPrescription.KindOf`, beside the constants it reads, which is where a
    domain rule belonged anyway. A response record was never the right home for "why is this slot
    this size".
  - **`InferCut` had to become `internal`.** The response mapper now needs the same cut level a
    substitution needs, read back from what the week contains rather than stored. Two callers, one
    rule — worth noticing if a third ever wants it, because at that point it wants to be a method
    on the week rather than on the endpoints class.
  - **A ceiling slot and a cut slot both carry two sets and mean opposite things**, so the field
    is not derivable from `sets` and the test says so directly: a 2x25 week (`TD-012`'s minimum,
    which forces the ladder to its last rung) returns every slot as `Full` at two sets, while a
    5x60 week returns both kinds. Without that second test the first would have passed against an
    implementation that simply reported "fewer than three sets means bought".
  - **Enum-name assertions are written as `Enum.TryParse`**, which is what actually pins root
    standard 3 here: a response that started returning a sentence would fail the parse rather than
    the eye.

### S5.5 — Per-muscle volume against the week's own target
- **Status:** completed
- **Tests:** 41 unit in `WeekGeneratorTests` (3 new), 17 integration in `GeneratedWeekEndpointsTests` (3 new)
- **Observations:**
  - **EF generated the migration with `defaultValue: 0m` and that would have been the backfill.**
    Eight existing weeks would have reported every muscle as infinitely over a target of zero.
    The scaffolded default is not a neutral placeholder — on an `AddColumn` against a non-empty
    table it *is* the historical value, forever, because standard 10 makes migrations
    forward-only. Edited to `6.0m` before it was applied anywhere, with the reasoning in the
    migration rather than in a script beside it.
  - **`required` on the two new columns caught a construction site the compiler would otherwise
    have defaulted silently.** `WorkoutBindingTests` builds a `GeneratedWeek` by hand; with an
    initialiser it would have inherited today's constant without anyone deciding. Matching how
    `Goal` and `DaysPerWeek` are declared was worth more than the convenience.
  - **The substitution path had to copy the band, not re-read it.** `Substitute` builds a new row
    describing the same plan, so taking today's constant there would silently re-judge a week
    under rules it was never generated under — the same failure `ADR-003` exists to prevent, one
    level up and easy to miss because the code reads naturally either way.
  - **`Uncovered` is exactly one muscle group today: `Adductors`**, the only one of sixteen that
    no row of the 63-exercise catalogue trains directly. Measured rather than assumed, and the
    integration test carries an `Assert.NotEmpty` guard because every other assertion in it is an
    `Assert.All` that would pass vacuously against an empty list. If that guard ever fails the
    catalogue grew an adductor exercise, and the assertion should be re-decided rather than
    deleted.
  - **The response's `uncovered` is computed over the whole catalogue, while `WeekPlan`'s is
    computed over what the user can actually perform.** They are different questions — "nothing
    models this" against "nothing you own trains this" — and the plan's acceptance criterion asks
    for the first. The second needs the user's equipment at read time and would also raise which
    equipment applies: today's, or what they owned when the week was generated. Left as written
    and recorded here because the two lists will not always agree.

### S5.6 — The week screen explains itself
- **Status:** completed
- **Tests:** 22 frontend unit (1 new), 41 E2E in Docker (3 new), typecheck clean
- **Observations:**
  - **The plan named a test file that does not exist.** It asks for
    `lib/i18n/__tests__/dictionaries.test.ts`; the real file is `locales.test.ts`, and it already
    compares every key recursively — so the new strings were covered the moment they were added,
    without writing anything. What it did *not* cover is the failure that actually happens: a
    vocabulary block added in English and forgotten, which passes key parity and satisfies the
    compiler while a pt-BR reader sees "Chest". That assertion is the new test.
  - **`Pill` takes five tones and no `title`.** The first attempt marked a ceiling slot with a
    tooltip, which the compiler refused and which would have been the wrong answer anyway: a
    tooltip is not keyboard-reachable, and standard 13 puts elements before ARIA. The note became
    a visible line, rendered only when the week actually contains a bought slot.
  - **Fifteen volume rows, not sixteen.** `Adductors` is uncovered and appears in its own block
    instead, so the E2E asserts 15. A future adductor exercise changes that number and the test
    will say so — which is the intended direction, since the count is the assertion that the two
    lists stay disjoint.
  - **The equipment vocabulary is `week.implements`, not `week.equipment`.** The exercise's
    `Equipment` enum (ten values: how the movement is done) is a different vocabulary from
    `EquipmentItem` (what a gym contains), which the `equipment` section already translates under
    `equipment.items`. Two blocks named `equipment` one level apart would be read as one.
  - **`ok`, `warn` and `bad` were available and deliberately not used.** An extra slot is neither
    good nor bad, and this tier reserves green and red as data ink for progress and regression.

### S5.7 — The split becomes a choice
- **Status:** pending

### S5.8 — The plan becomes a queue
- **Status:** pending

### S5.9 — A session is done, and the queue advances
- **Status:** pending

### S5.10 — What a muscle has actually accumulated
- **Status:** pending

### S5.11 — The ladder, containerized
- **Status:** pending
