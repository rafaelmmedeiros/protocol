---
id: ADR-021
title: The Hevy client is sequential, resumable and backs off, because no rate limit is declared
status: active
binds: [backend]
decided: 2026-08-23
---

**Context.** `ADR-018` makes the import incremental over the workout events feed, with a per-user
cursor and a first-run backfill. How hard that backfill may push is undecided, and the fact that
would settle it does not exist: **Hevy's OpenAPI document declares no rate limit at all.** It
declares response codes 200, 201, 400, 403, 404, 409 and 500 — no 429 — and contains no
`Retry-After` header, no `X-RateLimit-*` header, and no mention of throttling or quota anywhere.

An undeclared limit is not an absent limit. It is a limit whose value and behaviour we would
discover in production, on a user's real account, at the moment a backfill is running.

The scale is modest and worth stating, because it removes the temptation to optimise. The events
feed caps a page at **ten** items, so a history of three hundred workouts is about thirty
sequential calls. This is not a throughput problem.

**Options.**

### A — Sequential, resumable, and back off on anything that looks like refusal
- One request at a time per user, no parallel paging. The cursor is persisted **as each page is
  committed**, so an interrupted backfill resumes where it stopped rather than restarting.
  Any 429 or 5xx is retried with exponential backoff and a bounded attempt count; a `Retry-After`
  header is honoured if one ever appears. Exhausting the attempts ends the sync as a **partial
  success** with the cursor intact, surfaced as a code.
- **Pros:** correct whatever the real limit turns out to be, including no limit at all. Costs
  nothing at this scale — thirty sequential calls against a ten-item page is not slow enough to
  optimise. The resumability is nearly free because `ADR-018` already stores a cursor, and it is
  what turns an unknown limit from an outage into a delay.
- **Cons:** a very long history backfills more slowly than it strictly must. Retry logic is code
  that exists for a condition never yet observed.

### B — Discover the limit with a probe, then design against it
- Deliberately push the API until it refuses, record the threshold, and encode it.
- **Pros:** replaces a guess with a measurement, which is this project's usual preference and the
  reason the `routine_id` experiment was run rather than assumed.
- **Cons:** the only account available is the engineer's real one, and the probe's success
  condition is *getting it refused* — possibly rate-limited or flagged, on the account holding the
  training history this product exists to read. The measurement is also not durable: an undeclared
  limit is undeclared precisely because the vendor reserves the right to change it, so the number
  would be encoded and silently wrong later. **The experiment that settled `routine_id` was safe
  and repeatable; this one is neither.**

### C — Assume no limit, handle failure as failure
- Page as fast as the API answers; if a request fails, the sync fails and the user retries.
- **Pros:** least code.
- **Cons:** the first user with a long history discovers the limit for us, mid-backfill, and a sync
  that fails partway with no persisted progress restarts from the beginning — which is the exact
  behaviour most likely to hit the limit again.

**Recommendation.** A — it is correct under every hypothesis about the limit, it costs almost
nothing at this scale, and it needs no fact we do not have.

**Decision.** A

**Consequences.**

- **Retry code will ship untested against a real 429.** The suite exercises it against a stubbed
  refusal, and the first genuine one is the real test. That is accepted: the alternative is
  provoking one on the engineer's account.
- **A partial sync is a first-class outcome**, not an error state. The cursor is the guarantee: a
  sync that stops halfway has still made progress, and the next one continues. Nothing about
  `ADR-018`'s append-only versioning is disturbed by running twice.
- **This binds outbound pushes too.** `ADR-015` creates a folder and one routine per session, so a
  six-day week is seven writes. Same client, same sequential discipline, same backoff — a push that
  fails partway leaves the routines it already created, and `ADR-017`'s reuse-by-`PUT` is what makes
  retrying it safe rather than duplicating.
- **If a 429 is ever observed, its shape is worth recording** — the code, any header, the recovery
  time. That is a `Revisions` bullet here, and it is the cheap version of option B: measurement
  taken when the system hands it to us for free, instead of provoked.
