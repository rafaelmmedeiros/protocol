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
- **Status:** pending

### S2.5 — Estimated session duration
- **Status:** pending

### S2.3 — Preference: exclusions and preferred variants
- **Status:** pending

### S2.4 — Substituting one exercise
- **Status:** pending

### S2.6 — The equipment and preference screens
- **Status:** pending

### S2.7 — The ladder, containerized
- **Status:** pending
