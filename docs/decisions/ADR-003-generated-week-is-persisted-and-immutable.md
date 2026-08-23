---
id: ADR-003
title: A generated week is persisted and immutable
status: active
binds: [backend]
decided: 2026-08-22
---

**Context.** `M1` generates a week of sessions from a profile and shows it. Whether that week
is stored decides more than a table: the product's reason to exist is comparing what was
prescribed against what was logged, and a week that is recomputed on every visit has nothing to
compare against. The training decisions behind a week also change over time — records supersede
each other — so a week regenerated a month later is a different week, silently.

**Options.**

### A — Ephemeral: recompute on every request
- **Pros:** No table, no migration, no storage surface in `M1`.
- **Cons:** The week the user read yesterday stops existing. Nothing can be compared to what
  was logged. A superseded `TD` rewrites history that was never recorded as history.

### B — Persisted and mutable
- Stored, and regenerated in place when the profile or a decision changes.
- **Pros:** Always current.
- **Cons:** Overwrites the programme the user actually trained under. Standard 7's reasoning
  applies: a correction that destroys the record makes every past week unexplainable.

### C — Persisted and immutable
- A generated week is written once, with the moment it was generated and the profile it came
  from. A change produces a new week; the old one stays.
- **Pros:** What the user saw remains readable, and remains explainable against the decisions in
  force when it was produced. Gives the logged-versus-prescribed comparison something to stand
  on. Consistent with standard 7 and with the append-only training records.
- **Cons:** A migration and more surface in `M1`. Weeks accumulate, including discarded ones.

**Recommendation.** C

**Decision.** C

**Notes.** Immutable is about the record, not the interface: a user regenerating their week is
expected, and produces a new row rather than editing one.

**Revisions.**
- 2026-08-23 — "a user regenerating their week is expected, and produces a new row rather than
  editing one" is refined by `ADR-009`: a regeneration that produces a week **identical** to the
  stored one writes nothing at all. Immutability is unchanged — nothing here is edited or
  deleted — but this record's claim that regenerating always produces a row stopped being true,
  and it is corrected here rather than left to disagree with the code (standard 18).
