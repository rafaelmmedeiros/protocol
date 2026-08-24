---
id: ADR-023
title: The exercise catalogue stays authored in C#, split by movement pattern
status: active
binds: [backend]
decided: 2026-08-24
---

**Context.** The catalogue is a static C# table — one `Make(...)` call per exercise plus a
requirements table keyed by Hevy's template id — seeded idempotently at startup. It holds 36 rows
in 309 lines. This milestone adds enough rows to roughly triple it, which is the point at which
"where does curated data live" stops being obvious.

**Options.**

### A — Stay in C#, split the file by movement pattern
- `Make(...)` per row, as today; the single file becomes several partials grouped by pattern.
- **Pros:** the method signature makes every attribute mandatory — a row cannot be added without a
  movement pattern, a mechanic, an order class, a laterality and a primary muscle. A row missing
  its equipment requirements throws at startup rather than seeding a movement nobody can perform
  (`ADR-013`). Both guarantees are structural; neither survives a move to data without being
  rebuilt as validation.
- **Cons:** curating requires a rebuild, and the file is long. Someone who is not editing code
  cannot add an exercise.

### B — A data file loaded at seed time
- JSON or CSV in the repository, parsed by the seeder.
- **Pros:** separates curation from code, and can be edited without recompiling.
- **Cons:** every guarantee above becomes a runtime check that has to be written and tested, and
  validity moves from *guaranteed* to *asserted*. It also adds a schema to keep in step with the
  entity, which is a second surface for the same facts.

### C — An administration screen
- Curate through the app.
- **Pros:** the right answer eventually, when more than one person curates.
- **Cons:** a screen, a CRUD and a write path for data that is a global seed today, built for a
  single curator. Expensive now and cheap later, which is the wrong order.

**Recommendation.** A — the compiler is doing real work here, and at this size nothing else pays
for replacing it.

**Decision.** A

**Consequences.**

- **Curation is a commit**, which means it is reviewed and reverted like anything else, and a wrong
  muscle attribution has a diff. `TD-005` calls that attribution the soft spot of the design, so
  having it in version control with the reasoning beside it is worth more than editability.
- **This decision has an expiry and it is not a date.** When someone who does not write C# needs to
  add an exercise, or when the catalogue is curated by more than one person, B or C wins and this
  record is superseded rather than revised.
