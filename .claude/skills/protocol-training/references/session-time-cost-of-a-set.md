---
topic: session-time-cost-of-a-set
confidence: thin
bearing: Supplies the arithmetic that converts a session's minutes into a number of exercises. The model is a time-and-motion calculation, not a training finding — but it is calibrated against two trials that measured session duration directly, and lands within 1-8% of both. Its single load-bearing result is that rest is 74-79% of a session's clock.
sources:
  - Iversen VM, Norum M, Schoenfeld BJ, Fimland MS (2021). No Time to Lift? Designing Time-Efficient Training Programs for Strength and Hypertrophy - A Narrative Review. Sports Medicine 51:2079-2095. https://pmc.ncbi.nlm.nih.gov/articles/PMC8449772/
  - A Comparison of Affective Responses Between Time Efficient and Traditional Resistance Training (2022). https://pmc.ncbi.nlm.nih.gov/articles/PMC9243264/
  - Effects of one long vs. two short resistance training sessions on training volume and affective responses in resistance-trained women (2022). https://pmc.ncbi.nlm.nih.gov/articles/PMC9557220/
  - Schoenfeld BJ, Ogborn DI, Krieger JW (2015). Effect of Repetition Duration During Resistance Training on Muscle Hypertrophy - A Systematic Review and Meta-Analysis. Sports Medicine 45:577-585. https://link.springer.com/article/10.1007/s40279-015-0304-0
  - Chaves SFN et al. (2020). Effects of resistance training with controlled versus self-selected repetition duration on muscle mass and strength in untrained men. PeerJ 8:e8697. https://peerj.com/articles/8697/
  - Acute Effect of Fixed vs. Self-Selected Rest Interval Between Sets on Physiological and Performance-Related Responses (2024). JFMK 9(4):200. https://pmc.ncbi.nlm.nih.gov/articles/PMC11503322/
last-reviewed: 2026-08-23
---

**What is claimed.** That a resistance-training session's duration can be predicted from its
prescription — that sets, rest and transitions add up to a number of minutes, and therefore that
a number of minutes can be inverted into a number of exercises.

**What the evidence actually shows.** There is no time-motion literature on resistance training
in the sense that exists for team sports. What exists is a handful of trials that reported
measured session durations alongside a fully specified protocol, and those are enough to
calibrate an additive model and check it.

*Two calibration points.* Both are crossover trials in trained lifters, both prescribed three
sets per exercise to failure at ~9RM with two minutes of rest between every set **and** between
exercises, and both reported total session time and total repetitions.

| Trial | Exercises x sets | Total reps | Rest intervals | Measured session |
|---|---|---|---|---|
| Affective responses (2022), 29 trained | 8 x 3 = 24 sets | 227 | 23 x 120 s = 46.0 min | **58 +/- 3 min** |
| One long vs two short (2022), 23 trained women | 6 x 3 = 18 sets | 169 | 17 x 120 s = 34.0 min | **46 min** |

Subtracting prescribed rest from measured duration leaves 12 minutes in both trials, for 227 and
169 repetitions respectively — **3.2 s and 4.3 s per repetition**, the second inflated because it
also absorbs whatever warm-up occurred. The additive model
`session = sum(set durations) + sum(rest) + overhead` reproduces the first trial to within 1%
(57.4 predicted vs 58 measured) at 3 s/rep.

*Repetition duration is the small term, and it is free for growth.* Schoenfeld, Ogborn and
Krieger's meta-analysis found similar hypertrophy across repetition durations of **0.5 to 8
seconds**, with inferiority appearing only above ~10 s/rep. Chaves et al. randomised a controlled
2s:2s tempo against a fully self-selected duration in untrained men for 8 weeks and found no
difference in 1RM or cross-sectional area. So the model can use a self-selected cadence of
roughly 3 s/rep — the value both trials back-calculate to — without a training cost. Its
uncertainty (2-4 s/rep) moves a 21-set session by about 3.5 minutes.

*The load-bearing result is the proportion.* Prescribed rest was **79%** of measured session
duration in the first trial and **74%** in the second. Every other term — repetitions,
transitions, warm-up — is together a fifth of the clock. **Any time model that gets rest right is
approximately right; any that gets rest wrong cannot be rescued by tempo.**

*Transitions between exercises are the term with no evidence.* In both calibration trials the gap
between exercises was prescribed as one further rest interval and the model needs no extra term
to fit — but both were supervised laboratory sessions with equipment reserved. Finding a rack,
loading a bar, queuing for a machine and adjusting a seat are unmeasured. Nothing found
quantifies them in a commercial gym.

*People do not rest what they are told, and it does not seem to matter.* A 2024 crossover in 13
trained men found self-selected rest averaged **97 +/- 24 s** where the fixed condition was 120 s
on the opening set, then lengthened set by set until it exceeded 120 s by the last — with no
difference in mean propulsive velocity, velocity loss, muscle oxygenation or RPE. Prescribed rest
is therefore a planning number, not an observed one: real sessions drift short early and long
late, and roughly cancel.

**What it does not settle.**

- **Transition and setup time is an engineering constant, not a measured one.** It is the single
  term with no source behind it, and the term that grows fastest with the number of exercises. A
  model assuming zero setup will systematically under-predict an exercise-heavy session.
- **Both calibration trials trained to failure.** Sets stopped short of failure are shorter,
  because the slowest repetitions of a set are the last ones. A model calibrated on failure sets
  over-predicts set duration at 2-3 RIR (`TD-010`), by an unknown amount.
- **Warm-up is not separated out** in either trial. The 12-minute non-rest residual absorbs it.
- **No trial randomised session duration** and measured anything. Everything above is
  description; nothing licenses a claim that any particular duration is better.
- **Nothing here is a training finding.** It is arithmetic over prescriptions, checked against two
  measurements. It should not be graded above `thin` however well it fits.

**Where it touches the product.** The minutes-to-slots conversion (`TD-012`), the validation
bounds on `session_duration_seconds`, and the cost model a time-constrained generator minimises
against (`TD-013`). The 74-79% figure is why a rest field is the first thing a time budget
touches.
