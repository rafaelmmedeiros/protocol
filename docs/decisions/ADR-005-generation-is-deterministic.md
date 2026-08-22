---
id: ADR-005
title: Generation is deterministic for a given profile
status: active
binds: [backend]
decided: 2026-08-22
---

**Context.** Selecting exercises invites randomness — variety feels like a feature. But every
number a generated week contains has to be defensible against a decision record (standard 15),
and a test can only assert on output it can predict.

**Options.**

### A — Deterministic: the same profile and catalogue produce the same week
- **Pros:** The generator is a pure function, so its tests assert on the whole output rather
  than on properties of it. A week can be reproduced from its inputs when someone asks why it
  looks the way it does. Randomness cannot quietly stand in for a decision that was never made.
- **Cons:** Regenerating without changing the profile returns the same week, so variety has to
  come from an explicit input later rather than from chance.

### B — Randomised selection within the allowed set
- **Pros:** Variety for free; two weeks never look identical.
- **Cons:** Tests can only assert properties, which is exactly where a wrong prescription
  hides. "Why this exercise?" has no answer. An unseeded random choice is an undeclared
  decision, which is what standard 15 exists to prevent.

**Recommendation.** A

**Decision.** A

**Notes.** Variety is not rejected — it is deferred to something that can be cited: rotation
driven by training history, or an explicit variation input, both of which arrive with the
milestones that have history to reason about.

**Revisions.**
- _(none)_
