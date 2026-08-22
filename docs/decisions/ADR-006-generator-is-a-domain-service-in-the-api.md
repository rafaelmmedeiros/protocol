---
id: ADR-006
title: The generator is a domain service inside Protocol.Api
status: active
binds: [backend]
decided: 2026-08-22
---

**Context.** The backend is one project with a composition root and an `Auth/` folder. The
generator is the first piece of real domain logic the system has, and where it lives decides
how it is tested: the backend's own testing invariant is that unit tests cover pure logic with
no I/O, and that integration tests assert through the API rather than against the database.

**Options.**

### A — A separate class library, referenced by the API
- **Pros:** The domain cannot accidentally take a dependency on HTTP or on the database. A
  compiler-enforced boundary.
- **Cons:** A second project, a second set of build and Docker considerations, for a boundary
  one folder and one review already hold at this size.

### B — A `Training/` folder inside `Protocol.Api`, with the generator as a pure service
- The generator takes a profile and a catalogue and returns a week. It reads nothing and
  writes nothing; persistence happens at the endpoint around it.
- **Pros:** Matches the tier's existing shape (`Auth/`). Unit-testable with no I/O, which is
  precisely what the tier's testing invariant asks for. No build or packaging change.
- **Cons:** Nothing stops a later edit from injecting a repository into it. The purity is a
  convention, not a compiler error.

### C — A background worker
- **Pros:** Generation could not block a request.
- **Cons:** Generating a week is arithmetic over a small catalogue. Asynchrony would buy
  latency that is not being spent and cost a queue that does not exist.

**Recommendation.** B

**Decision.** B

**Notes.** The purity is what the unit tests protect: if the generator ever needs I/O to do its
job, that is the signal to revisit this record rather than to inject the dependency.

**Revisions.**
- _(none)_
