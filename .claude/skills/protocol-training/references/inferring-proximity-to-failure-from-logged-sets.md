---
topic: inferring-proximity-to-failure-from-logged-sets
confidence: thin
bearing: There is no evidence that proximity to failure can be recovered from weight and reps alone, and the corpus should stop hoping. What is well measured is that a lifter's own RIR estimate is accurate to ~1 repetition when genuinely close to failure — which makes asking cheaply beat inferring cleverly, and makes TD-010's stated gap unclosable by analysis.
sources:
  - Hackett DA, Cobley SP, Davies TB, Michael SW, Halaki M (2017). Accuracy in Estimating Repetitions to Failure During Resistance Exercise. J Strength Cond Res 31(8):2162-2168. https://pubmed.ncbi.nlm.nih.gov/27898640/
  - Hackett DA, Cobley SP, Halaki M (2018). Estimation of Repetitions to Failure for Monitoring Resistance Exercise Intensity. J Strength Cond Res 32(5):1352-1359. https://pubmed.ncbi.nlm.nih.gov/29466270/
  - Refalo MC, Remmert JF, Pelland JC, Robinson ZP, Zourdos MC, Hamilton DL, Fyfe JJ, Helms ER (2024). Accuracy of Intraset Repetitions-in-Reserve Predictions During the Bench Press Exercise. J Strength Cond Res 38(3):e78-e85. https://doi.org/10.1519/JSC.0000000000004653
  - Gauging proximity to failure in the bench press - generalized velocity-based vs. %1RM-repetitions-to-failure approaches (2025). https://pmc.ncbi.nlm.nih.gov/articles/PMC11934800/
  - Hickmott LM, Chilibeck PD, Shaw KA, Butcher SJ (2022). The Effect of Load and Volume Autoregulation on Muscular Strength and Hypertrophy. Sports Med Open 8:9. https://pmc.ncbi.nlm.nih.gov/articles/PMC8762534/
  - Kolinger D et al. (2024). Fatigue and Metabolic Responses during Repeated Sets of Bench Press Exercise to Exhaustion. https://pmc.ncbi.nlm.nih.gov/articles/PMC11057609/
  - Mitter B, Holbling D, Bauer P, Stockl M, Baca A, Tschan H (2023). Modelling the relationship between load and repetitions to failure in resistance training - a Bayesian analysis. Eur J Sport Sci 23(6):1032-1041. https://onlinelibrary.wiley.com/doi/10.1080/17461391.2022.2089915
last-reviewed: 2026-08-23
---

**What is claimed.** That a logged set carrying only weight and repetitions contains enough
information to recover how close the lifter was to failure — from the repetition drop-off across
sets (12/12/12 against 12/9/7), from session-to-session change, or from a load-repetition model
— and that a progression engine can therefore act on effort without the user reporting it.

**What the evidence actually shows.**

*No study tests this inference.* That is the finding, and it is an absence rather than a null.
The proximity-to-failure literature is built the other way round: the experimenter fixes the load
and the target, the participant reports an estimate, and the set is continued to actual failure
to score it. Nobody has taken a corpus of ordinary logged sets and asked what RIR they imply, and
no validation of a set-shape heuristic exists in either direction.

*What is measured, and measured well, is the lifter's own estimate.* Hackett et al. (2017), 81
adults across a broad experience range: error in estimated repetitions to failure was **about 1
repetition when the true remainder was 0-5, and above 2 repetitions when it was 7-10**.
Critically, **training experience did not affect accuracy** — the widely repeated claim that a
novice cannot judge proximity to failure is not supported here. Hackett et al. (2018), 48 adults,
adds that the estimate is worst on the *first* set of an exercise and improves by the third
(2.0 to 0.6 repetitions on chest press), correlates strongly with the truth (r = 0.59-0.87), and
beats RPE for the same purpose (r = 0.32-0.42). Refalo et al. (2024), 24 trained lifters at 75%
1RM: absolute error **0.65 +/- 0.78 repetitions** at 1-3 RIR, unrelated to sex, experience or
relative strength.

**The shape of that error is exactly wrong for us.** `TD-010` prescribes 2-3 RIR, which sits at
the boundary where self-report is still good. But the failure mode when a user is *further* from
failure than they think is invisible and grows: someone who believes 3 and is at 7 reports 3.

*Velocity works, and needs hardware we will never have.* The generalised RIR-velocity
relationship predicted proximity to failure to **1.3 +/- 0.7 repetitions**. The
%1RM-repetitions-to-failure approach — the one needing no device, closest to what a log could
compute — **overestimated RIR by about 2.8-2.9 repetitions at 60% 1RM**, and the RTF-velocity
route was worse (about 5.7). Hickmott's review states the boundary plainly: velocity loss "cannot
precisely quantify proximity to failure". **The device-free inference route is the least accurate
one that has been tested**, and the between-person spread in repetitions-to-failure at a given
%1RM (Mitter 2023) is why.

*The one signal a log really carries is the drop-off, and it is a fatigue signal.* Ten trained
men benching at 65% 1RM to exhaustion with two minutes rest produced **16.0 / 9.9 / 6.3**
repetitions across three sets. Any set-to-failure sequence at fixed load falls steeply, so a flat
sequence (12/12/12) is decent evidence the sets were **not** near failure, while a falling one is
consistent with near-failure work. That asymmetry is real and mechanically grounded — and it is
confounded by rest actually taken, load selection, exercise, and by a user who simply stops at
the prescribed number. **Under a fixed repetition target, the prescription itself destroys the
drop-off signal.**

*Is anyone doing it?* Not from unlabelled logs. Commercial systems either ask (RIR/RPE fields) or
measure (velocity devices). The one large-scale log-mining paper found depends on sets being
**explicitly user-labelled as near-failure** — exactly the label we would not have.

**What it does not settle.**

- **It does not say drop-off inference is wrong** — it says nobody has tested it. A flat-sequence
  heuristic could be a reasonable engineering prior; it is simply not a finding, and shipping it
  as one would breach the standard this corpus exists to enforce.
- **It says nothing about a lifter estimating RIR unsupervised.** Every accuracy figure comes
  from a lab with an investigator present and the set continued to failure for scoring. Whether
  accuracy survives self-report in a gym, months in, with no feedback, is unmeasured.
- **It gives no threshold** at which an inferred RIR would be good enough to move a load.
- **Nothing addresses a partially labelled history** — some sets with RPE, most without — which
  is the realistic Hevy import.
- **Repetitions-to-failure vary enormously between people at the same %1RM**, so any population
  model applied to one user carries an unquantified personal offset.

**Where it touches the product.**

- **It answers `TD-010`'s stated largest gap with a negative.** The gap between prescribed and
  performed RIR cannot be closed by analysing logged sets. It closes by asking, or by designing
  so that it does not matter.
- **Asking is cheap and defensible.** One RIR field per exercise buys about one repetition of
  accuracy at the proximities we prescribe — better than every device-free inference tested.
  Whether the friction is worth it is a product judgement, and `TD-001`'s "observe, do not ask"
  posture is what it trades against.
- **A progression trigger keyed on "hit the top of the repetition range" is ambiguous and stays
  ambiguous.** If it ships, the record must say it acts on **completion**, not on effort.
- **It argues against building any velocity or RIR-estimation feature.** The accurate method
  needs hardware; the hardware-free methods are off by three to six repetitions.
- **The flat-sequence heuristic is the only thing here worth prototyping**, and only as a
  low-confidence flag — "these sets look easy" — never as an input to arithmetic.
