---
id: ADR-009
title: Regenerating does not write a week identical to the current one
status: active
binds: [backend]
decided: 2026-08-23
---

**Context.** `ADR-003` settled that a generated week is persisted and immutable, and that
regenerating produces a new row rather than editing one. It did not ask whether a regeneration
that changes nothing should write anything.

Observed in use: the engineer pressed generate twice, sixty-nine seconds apart, with no profile
edit in between. Both weeks are byte-identical — same Monday, same five sessions, same nineteen
slots, same prescriptions, verified by hashing the joined rows. The generator is deterministic
by decision (`ADR-005`), so this is not a coincidence: **with an unchanged profile and an
unchanged catalogue, regenerating can only ever produce what is already stored.**

`ADR-003` accepted that weeks accumulate, "including discarded ones", as the price of
immutability. A discarded week is one that was superseded by a *different* one; an identical
row is not a discarded alternative, it is the same answer written twice. It carries no
explanatory value, which is the entire justification `ADR-003` gave for storing weeks at all.

**Options.**

### A — Write a row every time
- What it does today.
- **Pros:** One rule, no comparison, no edge case. A literal reading of `ADR-003`.
- **Cons:** Accumulates rows that explain nothing. The screen shows no difference except the
  generated-at line, so a user cannot tell that pressing the button did nothing — and the
  product silently teaches them that the button is meaningless.

### B — Skip the write when the generated week matches the current one
- Compare what the generator produced against the stored current week; if the sessions and
  prescriptions are identical, return the existing week and write nothing.
- **Pros:** Immutability is untouched — nothing is edited and nothing is deleted. Works today,
  with no dependency on data the system does not have. It also makes the generated-at line
  honest: it dates the week, not the last button press.
- **Cons:** Introduces a comparison that has to stay correct as the shape of a week grows; a
  field added to a prescription and forgotten in the comparison would make two different weeks
  look identical. The endpoint stops being a plain write, which is a small amount of behaviour
  to explain.

### C — Hard delete the previous week when it was never started
- The engineer's proposal: keep the database clean by removing a week nobody trained.
- **Pros:** Addresses the accumulation directly, including weeks that differ but were never
  used.
- **Cons:** **"Not started" is not knowable yet.** It requires reading what was actually
  trained, which is the Hevy import and is a later milestone. Before that the system would have
  to assume, and an assumption that is wrong deletes a week someone trained — an append-only
  history losing a row is the failure root standard 7 exists to prevent. It is also a delete,
  where `ADR-003` chose immutability precisely to avoid one.

### D — Store a content hash and deduplicate on it
- Same effect as B, with the comparison denormalised into a column.
- **Pros:** Cheap comparison, and the hash is a natural key for "is this the same week".
- **Cons:** A hash column is a derived value that can silently disagree with the rows it
  summarises, which is the reason `S1.9` already declined to store `cut_applied`. It buys
  performance that nothing needs at one week per user per generation.

**Recommendation.** B — it solves the observed problem with no dependency on data that does not
exist yet, and without introducing a delete into a model whose whole point is that nothing is
removed.

**Decision.** B

**Divergence.** The engineer proposed C. It was not taken because its precondition — knowing
whether a week was started — is unavailable until training history is imported, and the failure
mode of guessing wrong is the deletion of a week that was actually trained. C remains a
reasonable capability *after* the import lands, at which point it is a different record: it
would be about discarding weeks that differ and went unused, which B does not address.

**Notes.** B changes when a row is written, never whether a written row can change. If the
comparison is ever in doubt, the safe direction is to write — a duplicate row is noise, and a
skipped write that should have happened is a lost week.

**Revisions.**
- _(none)_
