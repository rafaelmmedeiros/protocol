---
id: ADR-001
title: The training profile is its own section in the app, beside Equipment
status: active
binds: [frontend]
decided: 2026-08-22
---

**Context.** `M1` adds a screen where the user says how they intend to train. The app already
has four sections (Dashboard, Workouts, Equipment, Template) and a Settings page reached from
the account menu. Settings holds theme and language — system preferences. Equipment already
states its own purpose as "what is here decides what a generated session is allowed to ask
for", which is exactly what the training profile also decides. The two are the personal half
and the hardware half of one constraint on the generator, and `M2` brings the second.

**Options.**

### A — A new top-level section beside Equipment
- The nav becomes Dashboard · Workouts · Profile · Equipment · Template. The profile screen is
  a sibling of Equipment; Settings is untouched and stays systemic.
- **Pros:** No route moves, so no `data-testid` churn and no stale `.next/` route validator.
  Both halves of the generator's constraints are discoverable at the same level. Keeps
  Settings meaning one thing.
- **Cons:** Two nav entries for what is conceptually one setup. The nav grows to five.

### B — A section inside Settings
- Settings gains a group: Appearance, then Training.
- **Pros:** One nav entry. Reads as "everything you configure".
- **Cons:** Puts weekly frequency and colour theme in the same category. The first is an input
  to a generator that produces a training programme; the second is a rendering preference.
  Settings stops meaning anything.

### C — A Setup section with Personal and Equipment tabs
- One nav entry that absorbs the existing Equipment route as a tab.
- **Pros:** Matches the engineer's own framing of a personal step and a hardware step. One
  place to answer "what may the generator ask of me?".
- **Cons:** Moves an existing route — Playwright selectors, the nav test ids, and the `.next/`
  route validator all follow (`frontend/CLAUDE.md` records that trap). Pays that cost for a
  grouping whose second half does not exist until `M2`.

**Recommendation.** A — it costs nothing to build and nothing to undo, and C stays available
the moment Equipment has content worth grouping with.

**Decision.** A

**Revisions.**
- _(none)_
