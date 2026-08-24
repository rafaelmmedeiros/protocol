---
id: ADR-015
title: A pushed week is a Hevy routine folder holding one routine per session
status: active
binds: [backend]
decided: 2026-08-23
---

**Context.** A generated week holds two to six sessions (`TD-002`, `TD-003`). Hevy offers
routines and routine folders, and a live experiment established that a workout carries the
`routine_id` of the routine it was started from — which is the only association Hevy provides and
therefore the only join available (`ADR-019`). Whatever shape a week takes in Hevy decides what
granularity a logged workout can be matched at, and that decision cannot be revised later without
re-pushing history. Two API facts constrain it: a routine folder's `id` is a **number** while a
routine's `id` is a **string** uuid, and there is **no delete endpoint** for either.

**Options.**

### A — A folder per week, one routine per session inside it
- `POST /v1/routine_folders` with the week's title, then `POST /v1/routines` once per session with
  `folder_id` set. Both identifiers are stored beside our week as external keys (standard 8), in
  two columns of different types.
- **Pros:** matches how Hevy is actually used — starting a routine *is* starting a session, so the
  `routine_id` the workout comes back with identifies a session and not merely a week. Grouping is
  visible in Hevy, so a user with several weeks pushed can tell them apart. Session-level matching
  is the granularity the prescribed-against-performed comparison needs.
- **Cons:** one API call per session plus one for the folder. Folders accumulate, and there is no
  endpoint to remove them.

### B — One routine for the whole week
- Every session's exercises concatenated into a single routine.
- **Pros:** one call. No folder to manage.
- **Cons:** fatal — every workout in the week comes back with the same `routine_id`, so a logged
  workout can no longer be matched to the session that prescribed it. It also misrepresents the
  week in Hevy's own UI, where a routine is a thing you start and finish.

### C — One routine per session, no folder
- Routines land in Hevy's default "My Routines".
- **Pros:** one fewer call and one fewer external identifier to store. No orphan folders.
- **Cons:** every pushed week piles into one flat list with nothing separating them. By the third
  week the user cannot tell which routines are current, and the API gives no way to tidy up.

**Recommendation.** A — it is the only option that preserves session-level matching, and the
folder is what keeps repeated pushes legible in a surface we cannot clean up.

**Decision.** A

**Notes on the shape.** The folder is titled for the week and the routines for the session, using
the same vocabulary the app shows. Titles are display only on the way back (standard 9): nothing
in the import ever reads them, and `ADR-019` matches on identifiers alone. A routine's title is
useful to a human scrolling Hevy and to nobody else.

**Revisions.**

- 2026-08-24 — **Their write responses are enveloped and their OpenAPI document says they are
  not.** `POST /v1/routine_folders` answers `{"routine_folder": { ... }}` and `POST /v1/routines`
  answers `{"routine": [ ... ]}` — an envelope holding an **array** — where the document declares
  a bare object for both.

  Deserialising the declared shape produced a folder identifier of **zero**, which was stored
  without complaint. Every routine was then sent to folder 0, which does not exist, and Hevy
  refused each one with a 400 whose body we were discarding. The user saw "Hevy could not be
  reached" for a service that had answered twice.

  Three things changed, and the second is the one that mattered most. The envelopes are now
  modelled from bodies the live service actually returned, pinned by `HevyResponseShapeTests`. A
  successful response whose body does not carry what we asked for is its own outcome,
  `Unreadable`, rather than being reported as unreachable — a "try again" is a lie when the fault
  is our reading of their shape. And a refusal's body is logged instead of thrown away.

  The stored zeros are corrected by a forward-only migration, and the push treats a non-positive
  folder identifier as absent so those weeks recover by creating a real one.

  **The lesson is not "read the contract" — that was done.** It is that a published contract is
  evidence and not proof, and one live call is what separates the two. The same instinct that ran
  the `routine_id` experiment should have run a write once before shipping the writer.
