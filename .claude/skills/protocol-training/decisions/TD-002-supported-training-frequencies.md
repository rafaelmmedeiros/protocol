---
id: TD-002
title: The product supports 2 to 6 training days per week, and rejects 1 and 7
status: active
knowledge: [references/per-muscle-training-frequency.md, references/split-templates-by-frequency.md, references/cold-start-first-block.md]
decided: 2026-08-23
---

**Decision.**

`days_per_week` accepts the integers **2, 3, 4, 5 and 6**. The values **1 and 7** are rejected
with `FrequencyOutOfRange`, as is anything below 1 or above 7.

The two rejections are not the same kind of decision and the record keeps them apart
deliberately.

**Rejecting 1 is evidential.** Three reasons, and only the third is ours:

1. One session per week is the single point in the range where `TD-001`'s volume bound and the
   per-session ceiling actually collide. Delivering the top of the band — 12 fractional sets per
   muscle — in one session sits at or past Remmert's ~11-set point of undetectable superiority,
   and well past the disputed 6-8. Every other frequency has slack; this one does not.
2. ACSM 2026 says all major muscle groups at least twice weekly, and public-health
   muscle-strengthening guidance says 2+ days. Shipping a 1-day option means shipping something
   no position stand endorses.
3. One session must contain every muscle group at full weekly volume — a 90+ minute session for
   a user who just told us they have one day. That is the prescription most likely to be
   abandoned, and `references/cold-start-first-block.md` establishes that adherence, not
   stimulus, is the asymmetric cost.

**Rejecting 7 is a product bound, and must not be read as a training claim.** No trial found
tests 7 days a week volume-equated against 6. Nothing says 7 days is harmful. It is rejected
because:

1. There is no benefit to find. Saric (3 vs 6, volume-equated, trained men) found nothing;
   Pelland found no frequency effect on hypertrophy at all.
2. There is no new template. Every arrangement a 7th day could produce is one 6 days already
   produces with slightly more per-session volume. The 7th day adds scheduling, not stimulus.
3. There is no slack. A 7-day week leaves no room to move a missed session, and for a product
   whose live risk is adherence, that is the practical argument.

**Why this and not what the literature would suggest.** At the lower bound they agree. At the
upper bound the literature suggests nothing at all, and this record fills the silence with a
product judgement rather than pretending to a finding. Anyone reading `FrequencyOutOfRange` on
7 should find this paragraph and not conclude the evidence forbade it.

**What it costs.**

- **A user who genuinely trains 7 days a week is told a true thing is unsupported.** They will
  set 6 and train a seventh day the system never sees — which is worse than modelling it,
  because the generator's volume accounting will be wrong for that user. This is the real cost
  and it is accepted only because `M1` generates one week and analyses nothing yet.
- **A user with exactly one available day gets nothing.** The honest alternative was a 1-day
  full-body session at the bottom of the volume band, which is defensible; it was declined
  because the session it produces is the one most likely to end the user's training altogether.
  A rejection with a clear code is more honest than a programme built to be abandoned.
- **Both bounds are on sessions per week, not per-muscle frequency.** Per-muscle frequency is a
  consequence of the template and lands at 2-3x across the entire supported range
  (`TD-003`).

**How it shows up in code.**

- `TrainingProfile` validation accepts `2..6` inclusive and returns `FrequencyOutOfRange`
  otherwise — a stable code, never display text (root standard 3). The frontend owns the
  sentence, and the sentence for 7 should not imply harm.
- The generator has a split template for every value in `2..6` (`TD-003`), so no supported
  frequency can reach it without a mapping.
- The unit test asserting rejection covers `1` and `7` specifically, not just "out of range".

**When to revisit.**

- **The system observes a user logging a seventh day.** Hevy will show sessions the profile says
  cannot exist; that is the signal the 7-day rejection is costing real accounting accuracy.
- **A volume-equated trial of 7 versus 6 appears.** Unlikely, but it would convert the upper
  bound from a product judgement into an evidential one either way.
- **Weekly volume rises above ~12 fractional sets per muscle.** That pushes 2 days/week onto the
  contested per-session ceiling and reopens the lower bound alongside it.
- **A goal other than hypertrophy is supported.** Frequency is not inert for strength — Pelland
  found 100% posterior probability that strength gains rise with frequency — so this range was
  reasoned for one goal only.
