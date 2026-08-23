---
topic: repetition-range-and-load-for-hypertrophy
confidence: settled
bearing: Rep range is free for hypertrophy across roughly 5-30 reps when sets are taken near failure, so the range prescribed into a slot is chosen for RIR accuracy, joint stress and session time — not for growth. Any per-order_class rep table is convention resting on a genuine null.
sources:
  - Schoenfeld BJ, Grgic J, Van Every DW, Plotkin DL (2021). Loading Recommendations for Muscle Strength, Hypertrophy, and Local Endurance - A Re-Examination of the Repetition Continuum. Sports 9(2):32. https://pubmed.ncbi.nlm.nih.gov/33671664/
  - Lopez P, Radaelli R, Taaffe DR, et al. (2021). Resistance Training Load Effects on Muscle Hypertrophy and Strength Gain - Systematic Review and Network Meta-analysis. Med Sci Sports Exerc 53(6):1206-1216. https://pubmed.ncbi.nlm.nih.gov/33433148/
  - Schoenfeld BJ, Grgic J, Ogborn D, Krieger JW (2017). Strength and Hypertrophy Adaptations Between Low- vs. High-Load Resistance Training - A Systematic Review and Meta-analysis. JSCR 31(12):3508-3523. https://pubmed.ncbi.nlm.nih.gov/28834797/
  - Effects of Resistance Training Performed with Different Loads in Untrained and Trained Male Adult Individuals on Maximal Strength and Muscle Hypertrophy - A Systematic Review (2021). https://pmc.ncbi.nlm.nih.gov/articles/PMC8582674/
  - Halperin I, Malleron T, Har-Nir I, et al. (2022). Accuracy in Predicting Repetitions to Task Failure in Resistance Exercise - A Scoping Review and Exploratory Meta-analysis. Sports Medicine 52:377-390. https://link.springer.com/article/10.1007/s40279-021-01559-x
  - ACSM (2026). Position Stand - Resistance Training Prescription - An Overview of Reviews. https://pubmed.ncbi.nlm.nih.gov/41843416/
last-reviewed: 2026-08-23
---

**What is claimed.** That hypertrophy has an optimal repetition range — classically 6-12 — and
that heavy compounds and light isolation work should therefore occupy different parts of a
"repetition continuum."

**What the evidence actually shows.**

*Load is close to irrelevant for hypertrophy once sets are taken near failure.* Lopez et al.
(2021), a network meta-analysis, found **no differences in hypertrophy between low, moderate and
high load** in overall or subgroup analyses, while strength was clearly load-dependent (high and
moderate load superior to low, SMD 0.60-0.63 and 0.34-0.35 respectively). Schoenfeld et al.
(2017), 21 studies, reached the same place for whole-muscle measures — a non-significant trend
favouring high load (ES 0.82 vs 0.39) that did not survive as a difference between conditions.
ACSM 2026, over 137 reviews, states it flatly: **loads from 30% to 100% of 1RM all produced
hypertrophy provided sets were taken close to failure.**

*The systematic review of load studies gives the boundary precisely.* Across 23 studies, 15
showed no significant hypertrophy difference between loads when training was taken to muscular
failure; the equivalence held across roughly **30-90% 1RM**; and **loads at or below 20% 1RM
were ineffective**. The critical clause is repeated in every source: the equivalence is
conditional on effort. Without near-failure effort, lower loads lose.

*So the practical rep window is about 5-30 reps, with the ends behaving differently for reasons
other than growth.* Schoenfeld et al. (2021) argue exactly this: adaptations can be obtained
across a wide loading spectrum, and the classical continuum's hypertrophy claim is unsupported.
What differs at the ends is everything except hypertrophy — very heavy sets carry higher joint
and technical demand; very light sets take longer, are perceptually harder, and require going
closer to failure to work at all.

*One measurable thing does track rep range: how accurately a lifter can judge proximity to
failure.* Halperin et al. (2022), 13 publications / 12 studies / 414 participants, found
prediction accuracy improved as the number of repetitions to failure decreased, with no
statistically significant accuracy differences once a set was under about **12 repetitions**
(roughly above 70% 1RM). This is the strongest evidence-based reason to prefer a lower rep range
on a slot where the RIR prescription must actually be hit.

**What it does not settle.**

- **It does not license any specific per-slot rep table.** The finding is a null across a wide
  band; picking 6-10 for one slot and 10-15 for another is a convention the null merely permits.
- **The equivalence depends on near-failure effort, and our first block is deliberately not near
  failure.** This is the single most load-bearing interaction in this note: at 3 RIR, a 15-20 rep
  set at a light load is a materially weaker stimulus than the literature's low-load-to-failure
  condition, and **none of these meta-analyses tested that combination.** It is the argument for
  keeping first-block rep ranges toward the lower and middle of the permitted band.
- **Most subjects are untrained** (80.6% in the load systematic review). Trained-specific data on
  low loads is sparse.
- **Nothing tests rep range against adherence or enjoyment** in a way that would settle whether a
  25-rep set is worse tolerated than an 8-rep set for a real user.
- **Time cost is arithmetic, not evidence.** A 20-rep set takes longer than an 8-rep set; no
  source here quantifies that against outcome.
- **Fibre-type-specific and regional effects** at load extremes remain unresolved; whole-muscle
  measures may be hiding them.

**Where it touches the product.**

- **The rep range prescribed into a slot is a free variable for growth**, and should be chosen
  for RIR accuracy (Halperin), joint and technical demand, and session time. Recorded as
  `TD-009`.
- **It gives a defensible reason for `order_class` to carry a rep range at all**: not because
  compounds grow muscle at 6-10 and isolations at 12-15, but because a `compound_primary` slot is
  where RIR accuracy and technique matter most, and a lower rep range helps both.
- **It forbids the marketing version of the claim.** No generated programme may assert that a rep
  range was chosen because it grows more muscle.
- **It bounds the catalogue**: nothing beyond ~30 reps, since loads at or below 20% 1RM are
  ineffective.
