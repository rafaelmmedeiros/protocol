---
topic: cold-start-first-block
confidence: thin
bearing: Directly decides M1's first-block volume and whether week one contains a calibration test — the evidence favours a conservative fixed start that escalates on observed performance, and argues against an AMRAP/RPE calibration week.
sources:
  - Pelland JC et al. (2025). The Resistance Training Dose Response - Meta-Regressions of Weekly Volume and Frequency. Sports Medicine. https://sportrxiv.org/index.php/server/preprint/view/460
  - ACSM (2026). Resistance Training Prescription for Muscle Function, Hypertrophy, and Physical Performance in Healthy Adults - An Overview of Reviews. https://acsm.org/resistance-training-guidelines-update-2026/
  - Predictors of long-term resistance exercise adherence among beginners - evidence from a large cohort of mobile app users (preprint). https://sportrxiv.org/index.php/server/preprint/view/709
  - Keogh JWL, Winwood PW (2017). The Epidemiology of Injuries Across the Weight-Training Sports. Sports Medicine 47:479-501. https://pubmed.ncbi.nlm.nih.gov/27328853/
  - Which resistance training is safest to practice? A systematic review (2023). J Orthop Surg Res 18:296. https://josr-online.biomedcentral.com/articles/10.1186/s13018-023-03781-x
  - Steele J et al. (2017). Ability to predict repetitions to momentary failure is not perfectly accurate, though improves with resistance training experience. PeerJ 5:e4105. https://pubmed.ncbi.nlm.nih.gov/29188142/
  - Objective Accuracy in Estimating Repetitions in Reserve in the Back Squat - Experienced vs. Novice Subjects. https://pmc.ncbi.nlm.nih.gov/articles/PMC13215226/
  - Enes A et al. (2024). Training volume increases or maintenance based on previous volume - effects on muscular adaptations in trained males. J Appl Physiol. https://journals.physiology.org/doi/full/10.1152/japplphysiol.00476.2024
  - Ekkekakis P et al. (2015). Differences in exercise intensity seems to influence affective responses in self-selected and imposed exercise - a meta-analysis. https://www.ncbi.nlm.nih.gov/pmc/articles/PMC4523714/
last-reviewed: 2026-08-23
---

**What is claimed.** That a generator with no observed history must either guess a starting
dose, measure one, or ask for a proxy — and that the choice has consequences at both extremes.

**What the evidence actually shows.** No study was found that tests cold-start prescription
strategies against each other. Everything below is assembled from adjacent evidence, and the
strategy conclusion is inference, not a finding. That is what the `thin` tier means here, and
it should not be upgraded without a trial that tests the question directly.

*The cost of starting too low is small and self-correcting.* The volume-hypertrophy curve is
steepest at the bottom: minimum effective dose is around 4 fractional weekly sets, and sets
5-10 carry the best return per unit of recovery cost (Pelland 2025). A trained lifter started at
8-10 sets is inside the range ACSM 2026 recommends for everyone (~10 sets/muscle/week) and
roughly one tier below the trained-specific 12-20 (Baz-Valle 2022). At ~0.24% additional growth
per set, the cost of being four sets low for four weeks is measured in fractions of a percent of
muscle thickness — and it disappears the moment the generator escalates.

*The cost of starting too high is not primarily injury.* Resistance training injury rates in
bodybuilding-style training are 0.24-1 per 1,000 training hours, the lowest of any
weight-training modality and below most recreational sports; the wide 0.21-18.9 range across the
literature is driven by strongman, Highland Games and competitive lifting, not hypertrophy work.
Starting a novice at 12 sets instead of 8 is not a meaningful injury exposure in this evidence.

*The cost of starting too high is adherence, and adherence is where the real asymmetry sits.*
In a cohort of 522,994 fitness-app users, only 18.1% of beginners were still adherent at six
months; median time to dropout was 14 weeks. Training consistency in the **first 28 days** was
the strongest predictor of long-term adherence. Longer sessions helped only users already
training frequently. Self-reported level tracked retention strongly (beginner 18.1%,
intermediate 28.6%, advanced 38.2% at six months). Separately, meta-analytic work on affective
response finds that imposed intensity above what a person would self-select degrades enjoyment
and predicts early withdrawal, particularly in previously inactive people. The asymmetry is
therefore: too low costs a fraction of a percent of hypertrophy that is recoverable; too high
costs a share of the 82% of beginners who are gone within six months, and that is not.

*A week-one calibration test is worse than it looks, for the population it would help most.*
Novices misjudge proximity to failure badly: less experienced trainees underpredicted
repetitions to failure by roughly 4-5 reps versus 1-2 for experienced ones (Steele et al. 2017,
n=141), so an RPE- or RIR-anchored calibration returns its noisiest reading exactly where the
system has least prior information. A small later study (n=16) found no experience difference at
RIR 1 and RIR 3, so this is contested — but accuracy improving nearer failure means a
calibration that is accurate is a calibration taken to or near failure, which is the
highest-fatigue, lowest-enjoyment session available, placed in the first 28 days that predict
everything. ACSM 2026 explicitly finds training to momentary failure is not necessary for
general fitness outcomes.

*Escalating from a conservative start has support that jumping to a high start does not.* In
trained men, increasing previously established weekly volume by 30% or 60% did not beat simply
maintaining it — the maintenance group showed greater aggregate strength gain (n=29 completers,
8 weeks). This is a small trial in trained subjects and a strength outcome, so it is weak, but
it points the same direction: more volume is not reliably better, and there is no evidence
penalty for arriving at a dose gradually.

**What it does not settle.**

- **The core question is untested.** Nobody has run conservative-start-and-escalate against
  calibrate-week-one against ask-and-trust. The recommendation above is an argument from four
  separate literatures, and should be held as a bet.
- **The adherence cohort is not this product.** It is app users at large, mostly self-directed,
  and it is a preprint. Whether a generated, escalating programme retains people better than a
  fixed one is not something that dataset can answer. Its clearest finding — early consistency
  predicts retention — is also correlational, and may simply identify people who were going to
  stick anyway.
- **How fast to escalate.** No source found gives a validated weekly increment. The only anchor
  is that the region above ~12 sets/muscle/week is where the evidence becomes contested (Aube
  2022 vs Baz-Valle 2022), which is a ceiling, not a rate. `M1` generates one week and does not
  escalate, so this is `M2`'s problem, not a gap in `TD-001`.
- **Whether self-reported level is usable as a weak proxy.** It predicts *adherence* in the app
  cohort. Nothing found establishes it predicts *response*, and Buckner (2017) is a direct
  argument that no single-variable label can. See `references/training-status.md`.
- **What "too high" costs a true beginner physiologically** — soreness, session RPE,
  unrecoverable fatigue — is not quantified in anything found here. The dropout argument stands
  in for it.
- **All of this is hypertrophy, healthy adults, mostly young.** No older-adult, injured or
  return-from-layoff population is covered.

**Where it touches the product.**

- **The `M1` first block.** Supports a conservative fixed starting volume near the bottom of the
  effective range, escalating later on observed logged performance, over a calibration week, a
  self-reported level, or a permanently fixed prescription. Recorded as `TD-001`.
- **Argues against putting an AMRAP or RIR-anchored test in week one**, which was a live option
  in the question `ADR-004` deferred here. If a calibration is wanted later, the evidence
  favours placing it after the first 28 days, when the user has both adherence momentum and
  some RIR experience.
- **Makes the first four weeks a product concern, not only a training one.** If early
  consistency is the strongest retention predictor available, then "did the user complete the
  session" is a first-class signal for a future generator, alongside load and reps.
- **Any decision resting on this must declare it rests on `thin` knowledge.** The direction is
  defensible; the specific starting number and escalation rate are a choice this corpus can
  bound but not justify.
