---
id: ADR-022
title: The equipment vocabulary names individual machines, one item each
status: active
binds: [backend, frontend]
decided: 2026-08-24
---

**Context.** `ADR-013` decided that an equipment item may be as specific as an individual machine,
and that decision has never been exercised: the catalogue contained no machine to name, because
`TD-004` assumed a gym without selectorised ones. `M3`'s first real import ended that — **3,798 of
5,186 logged exercises are movements this catalogue does not model, and the most-trained of them
are machines**: seated leg curl 162 times, hip abduction 132, leg press 131, leg extension 112.

Adding those movements forces the vocabulary question `ADR-013` deferred. An exercise declares the
items it needs; a user declares the items they have; the generator draws only what the second
covers. So how finely machines are named decides what a user can truthfully say about their gym.

**Options.**

### A — One item per machine
- `LegPressMachine`, `LegCurlMachine`, `LegExtensionMachine`, `HipAbductionMachine`,
  `ChestPressMachine`, `PecDeckMachine`, `SeatedRowMachine`, `ShoulderPressMachine`,
  `LatPulldownMachine`, `PreacherCurlMachine`, `CalfRaiseMachine`, `AbdominalMachine`,
  `BackExtensionBench`, `HackSquatMachine`, `SmithMachine` — around fifteen new values beside the
  eleven that exist.
- **Pros:** it is the only option that can be true. A gym with a leg press and no leg curl is
  ordinary, and only this lets someone say so. It is also what `ADR-013` already decided in
  principle, so choosing anything else would be reopening a settled record rather than applying it.
- **Cons:** the equipment screen roughly doubles, from fifteen checkboxes to around thirty. The
  engineer already called that screen limited in `M2`, so a longer one is a real cost even if it is
  the right kind of longer.

### B — Machine families
- Three or four values: `LegMachine`, `UpperPushMachine`, `UpperPullMachine`, `CoreMachine`.
- **Pros:** a short screen, and fewer rows to curate against.
- **Cons:** it lies. Ticking "leg machine" asserts a leg press *and* a leg curl *and* a leg
  extension *and* an abduction machine, and the generator will prescribe whichever is missing —
  which is the unperformable prescription `TD-004` reasons from, reintroduced deliberately.

### C — One `SelectorisedMachine` value
- A single checkbox for "this gym has machines".
- **Pros:** simplest possible.
- **Cons:** discards the entire granularity `ADR-013` exists to permit, and makes the equipment set
  useless for exactly the exercises this milestone adds.

**Recommendation.** A — B and C both buy a shorter screen by making the equipment set say something
untrue, and an equipment set that lies is worse than a long one that does not.

**Decision.** A

**Consequences.**

- **The vocabulary grows with the catalogue and not ahead of it.** An item is added when a movement
  needs it, never speculatively — an unused value is a checkbox that asks the user a question the
  system cannot act on.
- **`ADR-020`'s derived equipment gets sharper.** Until now a logged machine exercise could only be
  reported as a catalogue gap; once the movement exists and requires a named machine, the same
  history *implies* that machine and can suggest it.
- **The screen needs grouping, not shortening.** Around thirty checkboxes read as a wall
  ungrouped; that is a presentation problem and is solved there, rather than by making the
  vocabulary coarser.
