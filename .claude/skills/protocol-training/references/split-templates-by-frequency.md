---
topic: split-templates-by-frequency
confidence: thin
bearing: Decides the frequency-to-split mapping the generator needs, but the mapping is practitioner convention resting on a settled negative result — no template at 2-6 days/week is better for growth than another, so pick for schedulability and never claim otherwise in the UI.
sources:
  - Ramos-Campo DJ et al. (2024). Efficacy of Split Versus Full-Body Resistance Training on Strength and Muscle Growth - A Systematic Review With Meta-Analysis. JSCR 38(7):1330-1340. https://journals.lww.com/nsca-jscr/fulltext/2024/07000/efficacy_of_split_versus_full_body_resistance.20.aspx
  - Schoenfeld BJ, Grgic J, Krieger J (2019). How many times per week should a muscle be trained to maximize muscle hypertrophy? J Sports Sci 37(11):1286-1295. https://pubmed.ncbi.nlm.nih.gov/30558493/
  - Remmert J et al. (2025). Is There Too Much of a Good Thing? Meta-Regressions of the Effect of Per-Session Volume on Hypertrophy and Strength. https://sportrxiv.org/index.php/server/preprint/view/537
  - Pelland JC et al. (2025/2026). The Resistance Training Dose Response. Sports Medicine 56:481-505. https://pubmed.ncbi.nlm.nih.gov/41343037/
  - ACSM (2009). Position Stand - Progression Models in Resistance Training for Healthy Adults. MSSE 41(3):687-708. https://pubmed.ncbi.nlm.nih.gov/19204579/
  - ACSM (2026). Resistance Training Prescription for Muscle Function, Hypertrophy, and Physical Performance in Healthy Adults. https://acsm.org/resistance-training-guidelines-update-2026/
  - Iversen VM, Norum M, Schoenfeld BJ, Fimland MS (2021). No Time to Lift? Designing Time-Efficient Training Programs for Strength and Hypertrophy - A Narrative Review. Sports Medicine 51:2079-2095. https://pubmed.ncbi.nlm.nih.gov/34125411/
  - Saric J et al. (2019). Resistance Training Frequencies of 3 and 6 Times Per Week Produce Similar Muscular Adaptations in Resistance-Trained Men. JSCR 33:S122-S129. https://pubmed.ncbi.nlm.nih.gov/30363041/
  - Baz-Valle E et al. (2022). Equating Resistance-Training Volume Between Programs Focused on Muscle Hypertrophy. Sports Medicine 52:1273-1281. https://pubmed.ncbi.nlm.nih.gov/33826122/
last-reviewed: 2026-08-23
---

**What is claimed.** That a given number of training days per week implies a particular split —
full body at 2-3, upper/lower at 4, push/pull/legs at 6 — and that these mappings are training
knowledge.

**What the evidence actually shows.** The mapping is convention. What the literature supplies is
a constraint set the conventions happen to satisfy, plus a negative result strong enough to make
the choice between them free.

*The negative result.* Ramos-Campo et al. (2024) tested split versus full-body head-on in 14
trials (392 adults, 18-40) and found no difference in any hypertrophy or strength measure, with
I2=0%. Schoenfeld et al. (2019) reached the same place from the frequency side. **Split
organisation is not a hypertrophy variable when weekly volume is equated.** See
`references/per-muscle-training-frequency.md` for the full case.

*The constraints that do bind.* Three, and only three:

1. Weekly fractional sets per muscle must land in the prescribed range — for us, `TD-001`'s
   4-12, first block in the lower half.
2. Per-session fractional sets per muscle should stay under roughly 11, where returns become
   undetectable (Remmert 2025). Contested; see the boundary.
3. Session length must be liveable. Iversen et al. (2021) is the only source found that treats
   this as a design variable, and its recommendation is structural: prioritise bilateral
   multi-joint exercises, and include at minimum one leg press pattern, one upper-body pull and
   one upper-body push. That is the minimum viable full-body session, and it is what makes low
   frequency workable at all.

*The conventional templates, and what each delivers.* Recorded as practitioner consensus; the
per-muscle frequencies are arithmetic, not findings.

| Days | Template | Per-muscle frequency | Notes |
|---|---|---|---|
| 2 | Full body x2 (e.g. Mon, Thu) | 2x | Forced. No other arrangement reaches 2x. ~6-8 exercises/session. |
| 3 | Full body x3 (Mon, Wed, Fri) | 3x | The clean choice: identical every week. |
| 3 | Upper/Lower rotating (U-L-U / L-U-L) | 1.5x avg | Higher per-session volume per muscle, but the week does not repeat — a 2-week cycle. |
| 4 | Upper/Lower x2 (Mon, Tue, Thu, Fri) | 2x | The default. Repeats weekly, balanced, 10-14 sets/session/region. |
| 4 | Full body x4 | 4x | ~1.5-2 sets/muscle/session at our volumes — short sessions, more exercise-selection churn. |
| 5 | Upper/Lower/Upper/Lower/Full | ~2.5x | Repeats weekly. The most common defensible 5-day arrangement. |
| 5 | Push/Pull/Legs/Upper/Lower | ~2x | Also repeats weekly; more exercise variety, less symmetric. |
| 5 | PPL on a rolling 6-day cycle | 2x avg | Does *not* align to a Monday-start week (root standard 6). Avoid. |
| 6 | Push/Pull/Legs x2 | 2x | The classic 6-day template. |
| 6 | Upper/Lower x3 | 3x | Same days, higher per-muscle frequency, lower per-session volume. |
| 6 | "Arnold": Chest+Back / Shoulders+Arms / Legs, x2 | 2x | A relabelling of PPL by antagonist pairing rather than movement pattern. No distinct evidence. |

Two things fall out of that table and are worth more than the table itself:

- **Every defensible template between 2 and 6 days lands per-muscle frequency in 2-3x.** The
  split question, asked properly, has already been answered by the frequency evidence: the
  conventions exist because they are the tidy ways to hit 2-3x on a 7-day calendar.
- **What actually distinguishes templates at the same frequency is per-session volume and
  whether the week repeats**, not growth. At 6 days, PPL x2 puts ~2x the per-session volume into
  a muscle that Upper/Lower x3 does; Saric et al. (2019) tested 3 vs 6 sessions/week
  volume-equated in trained men and found nothing separating them on any measure.

*The interaction with `TD-001`'s 4-12 fractional sets.* This is the question that turns out to
be answered before it is asked. Worst case inside the supported range is 2 days/week at the top
of the band: 12 fractional sets split across 2 sessions is 6 per session, comfortably below
Remmert's ~11 ceiling and below the disputed 6-8 too. At the first-block target — the lower half
of 4-12, so roughly 6-8 weekly sets — 2 days/week gives 3-4 fractional sets per muscle per
session. **`TD-001`'s volume bound does not constrain the split at any frequency from 2 to 6.**
The binding constraint at low frequency is session *length* — a full-body session must touch
every muscle group, so 8 weekly sets each across 7-ish groups is ~28 fractional sets in a
session, which fractional counting reduces to perhaps 15-18 actual working sets once compounds
are credited to two or three muscles. That is a 60-75 minute session, not a training problem —
but it is `S1.5`'s problem.

*Training status.* This is the one place the status question could have bitten, and it does not.
ACSM 2009 differentiated novice (2-3 d/wk), intermediate (3-4 d/wk) and advanced (4-5 d/wk)
*only* in frequency and split organisation, which is exactly this note's subject. Three reasons
that table does not support a status-dependent split here:

- ACSM 2026, built on 137 systematic reviews, dropped the differentiation entirely and gives one
  recommendation for all: all major muscle groups at least twice weekly.
- Pelland et al. adjusted for training status in every model and still found no detectable
  frequency effect on hypertrophy.
- The 2009 table is best read as a statement about *available weekly volume capacity* — more
  days permit more total sets — not as a claim that a trained muscle requires more frequency.
  Read that way it is not a split recommendation at all.

**What `TD-001`'s refusal costs here: nothing, because the product asks a better question.** The
user is asked how many days a week they will train. Availability is observable, honest, and is
the variable ACSM 2009's table was really indexing through the status proxy. A status field
would add no information to the split decision that the frequency field does not already carry.

**What it does not settle.**

- **The templates themselves have no direct evidence.** No trial has compared, say, Upper/Lower
  x2 against PPL+Upper/Lower at 5 days. The table is convention, satisfying evidence-based
  constraints. That is why this note is `thin` and `per-muscle-training-frequency` is `settled`
  — they are different claims and must not be merged.
- **The per-session ceiling is contested** (~11 fractional sets, Remmert 2025, versus a
  practitioner argument for 6-8). Nothing in our range approaches either, so we can defer — but
  a future decision raising volume above ~12/week at 2 days/week would land on the disputed
  region and must reopen this.
- **Session-length tolerance is unmeasured.** The claim that a 2-day full-body session is
  "liveable" is an assertion. `references/cold-start-first-block.md` establishes that
  over-prescription costs adherence; it does not quantify a session-length threshold, and
  nothing found does.
- **Exercise selection and ordering are out of scope.** The split says which muscles on which
  day; it does not say which exercises. ACSM 2009's sequencing guidance (multi-joint before
  single-joint, large before small) is the only thing found here that touches it, and `S1.3`
  owns that question.
- **7-day and 1-day training are essentially unstudied** in this literature. Volume-equated
  trials top out at 6 sessions/week and bottom out at 1-2. Claims about either are
  extrapolation, including ours.
- **Fatigue accumulation across a block is not modelled anywhere above.** Every trial is 6-12
  weeks with fixed frequency; none tests whether a 6-day template is more likely to require a
  deload than a 3-day one at equal volume.

**Provenance caveat.** Two figures here came through secondary summaries rather than primary
text: Remmert's per-session point of undetectable superiority (~11 fractional sets), which is
load-bearing for the rejection of 1 day/week in `TD-002`; and the Ramos-Campo effect sizes,
where the direction and I2=0% are corroborated across two sources but the individual MD and p
values are single-sourced. Verify the Remmert figure if the 1-day rejection is ever challenged.

**Where it touches the product.**

- **The frequency-to-split mapping in the generator.** The table above is the concrete input,
  and `TD-003` is the ruling drawn from it. Cite `TD-003` for the mapping and
  `per-muscle-training-frequency` for why the mapping is free.
- **Supported frequency range and `FrequencyOutOfRange`.** Recorded as `TD-002`: 2-6 supported,
  1 and 7 rejected.
- **The UI must not claim a split is superior.** If the generator ever offers a choice between
  templates at the same frequency, the honest framing is scheduling and preference. A claim of
  greater growth would contradict Ramos-Campo (2024) directly.
- **Fractional set counting is required** for the volume figures in the table to mean anything —
  see `references/per-muscle-training-frequency.md`.
