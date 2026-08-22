---
id: ADR-002
title: The exercise catalogue is ours, keyed to Hevy by an external identifier
status: active
binds: [backend]
decided: 2026-08-22
---

**Context.** The generator has to choose exercises, and choosing requires attributes Hevy does
not expose on its templates — movement pattern, primary and secondary musculature, the
equipment a movement needs, whether it belongs early or late in a session. Standard 9 says an
exercise is identified by `exercise_template_id` and its title is display only; standard 8 says
Hevy's identifiers stay external, beside our own, never as a primary key. A generated week must
eventually be pushable into Hevy, which means every exercise in it has to resolve to something
Hevy recognises.

**Options.**

### A — Import Hevy's exercise templates and use them as the catalogue
- Pull the template list through the backend's Hevy client and store it as reference data.
- **Pros:** Every exercise is Hevy-resolvable by construction. No mapping step, ever.
- **Cons:** Brings the Hevy integration and an API key into `M1`. The generator can only reason
  about attributes Hevy publishes, which are not the attributes selection needs.

### B — Our own catalogue, carrying `exercise_template_id` as an external key
- Our entity, our primary key, our attributes, with Hevy's identifier stored beside them.
  Seeded from Hevy's data as the base, enriched with what selection requires.
- **Pros:** The generator reasons over the attributes it actually needs. Standard 8 is honoured
  literally. The Hevy link exists from the first row, so pushing a week later is a lookup, not
  a reconciliation. Live import stays a later step without blocking anything.
- **Cons:** The catalogue has to be curated and kept current by hand until import lands. Two
  sources of truth for the same movement, with drift possible in the titles.

### C — Our own catalogue with no Hevy reference
- Our exercises, no external identifier.
- **Pros:** Simplest possible `M1`.
- **Cons:** Every generated week becomes unpushable until a mapping is invented, and the
  mapping would have to be applied retroactively to an append-only history. Standard 8 exists
  to prevent exactly this.

**Recommendation.** B

**Decision.** B

**Notes.** Live import of Hevy's template list is deliberately out of `M1`; the catalogue is
seeded without a network call. What is not deferred is the external key — a row without its
`exercise_template_id` is the failure C describes, arriving one row at a time.

**Revisions.**
- 2026-08-22 — no change to the decision; recording the intent that reinforces it. The product
  is meant to grow its own logging surface eventually, at which point Hevy becomes an optional
  integration rather than the substrate (root `CLAUDE.md`, Product; `docs/ROADMAP.md`, The
  horizon). Option B is what makes that survivable: the catalogue and every week generated
  from it remain ours, and the Hevy column can go empty without anything else moving. Option A
  would have made the entire catalogue disappear with the integration.
