---
id: TD-003
title: One split template per supported frequency, chosen for schedulability rather than growth
status: superseded-by TD-023
knowledge: [references/split-templates-by-frequency.md, references/per-muscle-training-frequency.md]
decided: 2026-08-23
---

**Decision.**

Each supported frequency (`TD-002`) maps to exactly one split template. The mapping is total and
deterministic — the user does not choose a split in `M1`, and the same frequency always produces
the same shape (`ADR-005`).

| Days | Template | Days of the week | Per-muscle frequency |
|---|---|---|---|
| 2 | Full body x2 | Mon, Thu | 2x |
| 3 | Full body x3 | Mon, Wed, Fri | 3x |
| 4 | Upper / Lower x2 | Mon, Tue, Thu, Fri | 2x |
| 5 | Upper / Lower / Upper / Lower / Full | Mon, Tue, Thu, Fri, Sat | ~2.5x |
| 6 | Push / Pull / Legs x2 | Mon-Sat | 2x |

Three properties hold across the whole table, and each is a constraint rather than a preference:

1. **Every template repeats weekly and starts on Monday** (root standard 6). Rotating splits
   that run on a 6-day cycle — a common PPL arrangement — are excluded for this reason alone:
   a week that does not align to the calendar week makes "which week did this session belong
   to" unanswerable, and every later analysis stands on that question.
2. **Per-muscle frequency lands in 2-3x at every supported frequency**, which is where every
   defensible convention lands anyway.
3. **Rest days are distributed, not trailing.** Sessions are not stacked Mon-Tue-Wed with four
   days off, at any frequency where that is avoidable.

**Why this and not what the literature would suggest.** The literature suggests nothing here,
and that is the finding rather than a gap. Ramos-Campo et al. (2024) tested split against
full-body across 14 trials and found no difference in any hypertrophy measure with I2=0%;
Schoenfeld et al. (2019) reached the same null from the frequency side. **Split organisation is
not a hypertrophy variable once weekly volume is equated.** So this table is not a training
judgement dressed as one — it is a scheduling choice, and the honest justification for every row
is that it repeats weekly, distributes rest, and hits 2-3x.

Where more than one template was defensible, the tie was broken on schedulability:

- **At 3 days**, full body x3 over a rotating upper/lower, because the rotating version does not
  repeat weekly.
- **At 5 days**, U/L/U/L/Full over PPL+U/L, because it is symmetric and the fifth session absorbs
  the remainder cleanly.
- **At 6 days**, PPL x2 over Upper/Lower x3. Both are defensible and Saric et al. (2019) found
  nothing separating 3 from 6 sessions volume-equated. PPL was chosen for exercise variety per
  session; Upper/Lower x3 remains equally valid and this is the row most likely to change.

**What it costs.**

- **The user cannot choose their split.** Someone who prefers upper/lower at 6 days gets PPL.
  The evidence says this costs them no growth, but it may cost preference, and preference is an
  adherence variable this corpus takes seriously
  (`references/cold-start-first-block.md`).
- **One template per frequency is a simplification of a space with several right answers.** The
  cost is paid in flexibility, not accuracy — but the table's rows are conventions, and a future
  session must not mistake their specificity for evidential weight. This is why the note behind
  them is graded `thin` while the negative result under it is `settled`.
- **The 5-day row is the weakest.** There is no tidy 5-day arrangement; both candidates are
  compromises and no trial has compared them.

**How it shows up in code.**

- `Training/WeekGenerator` holds the mapping as a total function from `days_per_week` to a
  template, citing `TD-003` at the line. Every value `TD-002` admits has a row, so an unmapped
  frequency is unreachable rather than a runtime failure.
- Day-of-week assignment comes from this table, and the generated week's `week_start_date` is
  the Monday (root standard 6, `ADR-003`).
- Nothing in the UI may present a template as better for growth — the honest framing is
  scheduling. A claim of greater growth would contradict Ramos-Campo (2024) directly.

**When to revisit.**

- **The user asks to choose.** The moment split selection becomes a field, this record becomes a
  default rather than a mapping, and that is a new decision superseding this one.
- **Equipment enters the model (`M2`).** A template assumes the movements it names are
  available; PPL in particular assumes a press, a pull and a leg pattern all exist.
- **A trial compares specific templates at equal frequency and volume.** None exists today; one
  would move this note off `thin`.
- **Weekly volume rises above ~12 fractional sets per muscle.** At 2 days/week that lands on the
  contested per-session ceiling and the 2-day row would need re-examining.
