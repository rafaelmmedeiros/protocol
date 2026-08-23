---
topic: weekly-set-volume-for-hypertrophy
confidence: settled
bearing: Fixes the shape of the volume curve that TD-001's 4-12 bound was drawn from — most of the available growth is bought by the first ~4-10 fractional weekly sets, and the region above ~12 is where meta-analyses stop agreeing. Directly sets the number prescribed in `TD-008`.
sources:
  - Pelland JC, Remmert J, Robinson Z, Hinson S, Zourdos MC (2025/2026). The Resistance Training Dose Response - Meta-Regressions Exploring the Effects of Weekly Volume and Frequency on Muscle Hypertrophy and Strength Gains. Sports Medicine 56:481-505. https://pubmed.ncbi.nlm.nih.gov/41343037/
  - Schoenfeld BJ, Ogborn D, Krieger JW (2017). Dose-response relationship between weekly resistance training volume and increases in muscle mass. J Sports Sci 35(11):1073-1082. https://pubmed.ncbi.nlm.nih.gov/27433992/
  - Baz-Valle E, Balsalobre-Fernandez C, Alix-Fages C, Santos-Concejero J (2022). A Systematic Review of the Effects of Different Resistance Training Volumes on Muscle Hypertrophy. J Hum Kinet 81:199-210. https://pubmed.ncbi.nlm.nih.gov/35291645/
  - Remmert J et al. (2025). Is There Too Much of a Good Thing? Meta-Regressions of the Effect of Per-Session Volume on Hypertrophy and Strength. https://sportrxiv.org/index.php/server/preprint/view/537
  - ACSM (2026). Position Stand - Resistance Training Prescription for Muscle Function, Hypertrophy, and Physical Performance in Healthy Adults - An Overview of Reviews. https://pubmed.ncbi.nlm.nih.gov/41843416/
last-reviewed: 2026-08-23
---

**What is claimed.** That weekly set volume is the dominant programmable driver of hypertrophy,
and that there is a number of weekly sets per muscle group a programme should target.

**What the evidence actually shows.**

*The direction is not in dispute and the curve is concave.* Pelland et al. (2025/2026), 67
studies and 2,058 participants (79.1% male, mean age 25.2), Bayesian multi-level
meta-regression: the posterior probability that the volume slope exceeds zero is 100% for
hypertrophy, and the best-fitting model for fractional weekly sets is a **square-root** function
— monotonically increasing with continuously diminishing returns. At the sample's average
fractional weekly volume of **12.25 sets**, one additional set was worth about **0.24%**
additional hypertrophy. A square-root model has no plateau: it never turns down, and it never
stops rising either. **Anyone citing this paper for an "optimum" is reading a shape it does not
have** — the honest statement is "the marginal set is worth less as volume rises," not "volume
plateaus at X."

*Because the curve is concave, the low sets are the valuable ones.* Schoenfeld, Ogborn & Krieger
(2017), 15 studies / 34 treatment groups, found roughly **0.37% per weekly set** as a linear
approximation and observed that even 4 or fewer weekly sets per muscle produced substantial
hypertrophy, with a suggested threshold near 10 weekly sets for near-maximal growth. Combined
with Pelland's per-set estimate at 12.25 sets being lower than Schoenfeld's
average-over-the-whole-range estimate, the two agree on shape: the marginal set is worth more at
5 sets than at 15.

*The professional-body position has converged on ~10.* ACSM's 2026 overview of reviews (137
systematic reviews, over 30,000 participants) treats volume as the primary hypertrophy driver,
with a minimum threshold near **10 sets per muscle group per week** and a dose-response
continuing above it. This is stated as a threshold for near-maximal growth, not as a minimum for
any growth — Schoenfeld's 4-set finding is the relevant number for a floor.

*Above ~12 sets the meta-analytic literature stops agreeing.* Baz-Valle et al. (2022), 7 studies
in trained men (at least 1 year training, 18-35), comparing moderate (12-20 weekly sets) against
high (over 20): no difference for quadriceps (p=0.19) or biceps brachii (p=0.59). Their
recommendation of 12-20 weekly sets is explicitly for **young trained men** and rests on a small
pool. Pelland's model would keep predicting gains above 20; Baz-Valle's trials do not detect
them. This is the region `references/training-status.md` already flagged as contested, and it
sits entirely above `TD-001`'s bound.

*Per-session volume is a separate ceiling.* Remmert et al. (2025) put the point of undetectable
outcome superiority near **11 fractional sets per session per muscle**. Nothing in a 4-8
weekly-set prescription across 2+ sessions approaches it.

*All of this is in fractional sets*, the Baz-Valle/Pelland convention adopted as `TD-006`.

**What it does not settle.**

- **There is no evidence-derived "correct" weekly number, only a curve.** The choice of a point
  on it trades marginal growth against session time, fatigue and adherence — none of which these
  models contain. A prescription of 6 and a prescription of 10 are both defensible readings of
  the same evidence.
- **Where returns stop being worth the cost is a value judgement, not a finding.** The
  square-root model is silent on it by construction.
- **Nothing here is per-muscle-group.** See
  `references/muscle-group-specific-volume-requirements.md`.
- **The measured sites are mostly quadriceps and elbow flexors/extensors.** Delts, calves,
  forearms, erectors and glutes are barely represented in the pooled data.
- **Durations are 6-12 weeks; subjects are overwhelmingly young men.** A first block is within
  that window, which is the one place this literature is well matched to us.
- **Volume as *prescribed* is not volume as *performed*.** Every study equated what was assigned
  and supervised; none models a user skipping the last two slots.

**Provenance caveat.** The 0.24%-per-set figure at 12.25 fractional sets and the square-root fit
come from the abstract and from search-surfaced summaries — the results section could not be
retrieved. **No estimated marginal mean at a specific set count should be quoted from this note**;
the figure behind those was never seen. The same 0.24% appears in
`references/cold-start-first-block.md`, so the corpus is at least internally consistent.

**Where it touches the product.**

- **It supplies the number `TD-001` deliberately refused to pick**, inside its 4-12 fractional
  bound — recorded as `TD-008`. The concavity is the argument that the lower half costs less
  than it looks: moving from 8 to 12 buys roughly 1% of muscle thickness over a block at
  Pelland's per-set estimate.
- **It sets a hard floor, not just a target.** Below ~4 fractional sets the prescription leaves
  the region where any of these models were fitted.
- **It constrains nothing about how the sets are distributed** — that is
  `references/per-muscle-training-frequency.md` and `TD-003`.
- **It makes the fit-to-time-budget cut (`S1.5`) a training decision, not a cosmetic one.**
  Cutting a slot moves a muscle down a curve whose steepest region is exactly where a
  conservative first block sits.
