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
- **Status:** pending

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
