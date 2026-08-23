---
id: ADR-014
title: The Hevy API key belongs to the user, is encrypted at rest, and is never returned
status: active
binds: [backend, frontend]
decided: 2026-08-23
---

**Context.** Reading training history needs a Hevy API key, and the engineer has settled that
**each user supplies their own**, in their settings. That is the right call for the trajectory
the roadmap records — the product is local now and published later, and a single shared key
would have to be unmade the day a second person uses it.

It also introduces something this repo has not held before: **a credential belonging to a
user**. Root standard 11 governs the *application's* secrets — environment only, never in
tracked source — and says nothing about this. A Hevy key grants read **and write** access to
someone's whole training account, so a database dump of plaintext keys is every user's account
at once.

**Options.**

### A — A plaintext column
- **Pros:** Nothing to build. The import reads it directly.
- **Cons:** A backup, a log that dumps an entity, or a compromised read replica exposes every
  user's Hevy account. For a product the roadmap intends to publish, this is the failure that
  ends it.

### B — Encrypted at rest with ASP.NET Core Data Protection
- The key is protected on write and unprotected only when an import runs. The key ring lives
  outside the database, supplied by the environment.
- **Pros:** The framework's own answer, with key rotation and algorithm agility already in it.
  A database dump on its own is useless. Nothing new is invented.
- **Cons:** The key ring becomes state that must survive a container restart — and its default
  location inside a container does not. Losing it makes **every stored key permanently
  undecryptable**, silently, and the only recovery is asking every user to paste theirs again.

### C — Encrypted with a symmetric key read directly from the environment
- Our own AES over a secret in configuration.
- **Pros:** Explicit and easy to reason about.
- **Cons:** Reimplements what B provides, including rotation, which is exactly where a
  hand-rolled version goes wrong. No advantage over B.

### D — Never stored: the user pastes the key for each import
- **Pros:** Nothing at rest to leak.
- **Cons:** Import is periodic and will eventually be unattended; a key that must be typed
  cannot be used by anything scheduled. It also makes the product worse at precisely the thing
  it exists to do.

**Recommendation.** B — the risk that decides it is the one A carries, and B is the framework's
own answer rather than a home-made one.

**Decision.** B

**Notes.** Three properties follow and are part of this decision, not details of it.

**The key is never returned.** `GET` answers whether one is set, and nothing more — the same
posture as a password. A masked hint is acceptable; the value is not.

**The key is validated when it is saved**, by calling Hevy once. A typo that fails at entry is
a corrected typo; a typo that fails during an unattended import at three in the morning is a
support conversation.

**The key ring must be persisted, and that is a Docker trap this repo has to comment at the
line.** ASP.NET Core Data Protection writes its key ring to a location that does not survive a
container being recreated, and the failure is silent: encryption keeps working, decryption of
anything written before the restart does not. It has to be a mounted volume or a configured
store, and the comment belongs where someone would otherwise remove it.

Revoking is the user's, at Hevy. This product stores a key it was given and offers to forget
it; it cannot invalidate one.

**Revisions.**
- _(none)_
