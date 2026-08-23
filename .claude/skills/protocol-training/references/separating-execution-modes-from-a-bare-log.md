---
topic: separating-execution-modes-from-a-bare-log
confidence: thin
bearing: **No.** Two lifters executing the same prescription at opposite effort levels cannot be told apart from weight, repetitions, set order and session timestamps — the modes are non-identifiable rather than merely noisy, because a fixed repetition target censors the observation. Do not build a mode classifier, and stop treating the flat-sequence flag as worth prototyping. The intervention that costs nothing is prescribing a range terminated on effort, which removes the censoring instead of asking the user to declare it.
sources:
  - Impellizzeri FM, Shrier I, McLaren SJ, Coutts AJ, McCall A, Slattery K, Jeffries AC, Kalkhoven JT (2023). Understanding Training Load as Exposure and Dose. Sports Med 53:1667-1679. https://pmc.ncbi.nlm.nih.gov/articles/PMC10432367/
  - Impellizzeri FM, Marcora SM, Coutts AJ (2019). Internal and External Training Load - 15 Years On. Int J Sports Physiol Perform 14(2):270-273. https://doi.org/10.1123/ijspp.2018-0935
  - Lyristakis P, Wundersitz D, Cousins S, Zadow E, Gordon BA (2026). The Relationship Between Subjective and Objective Intensity Across a Multiset Resistance Training Program. J Strength Cond Res, online ahead of print. https://doi.org/10.1519/jsc.0000000000005615
  - Bachero-Mena B, et al. (2025). Velocity-based analysis of sets to failure in the bench press in women. J Hum Kinet. https://doi.org/10.5114/jhk/190387
  - Shimano T, Kraemer WJ, Spiering BA, et al. (2006). Relationship between the number of repetitions and selected percentages of one repetition maximum in free weight exercises in trained and untrained men. J Strength Cond Res 20(4):819-823. https://doi.org/10.1519/R-18195.1
  - Simao R, de Salles BF, Figueiredo T, Dias I, Willardson JM (2012). Exercise order in resistance training. Sports Med 42(3):251-265. https://doi.org/10.2165/11597240-000000000-00000
  - Willardson JM, Burkett LN (2006). The effect of rest interval length on the sustainability of squat and bench press repetitions. J Strength Cond Res 20(2):400-403. https://pubmed.ncbi.nlm.nih.gov/16686571/
  - Alonso-Aubin DA, et al. (2024). Self-selected versus fixed inter-set rest in the back squat. J Funct Morphol Kinesiol 9(4):200. https://doi.org/10.3390/jfmk9040200
  - King G, Azeem M, Noblitt S, Zand R, Valafar H (2025). Rep Smarter, Not Harder - AI Hypertrophy Coaching with Wearable Sensors and Edge Neural Networks. arXiv:2512.11854. https://arxiv.org/abs/2512.11854
  - Refalo MC, Helms ER, Hamilton DL, Fyfe JJ (2023). Influence of Resistance Training Proximity-to-Failure on Neuromuscular Fatigue. Sports Med Open 9:10. https://pmc.ncbi.nlm.nih.gov/articles/PMC9908800/
last-reviewed: 2026-08-23
---

**What is claimed.** That the *shape* of a logged repetition sequence identifies how the lifter
executed the prescription. Given "3 sets of 12" at a fixed load, a falling sequence (12/10/8) is
said to mean constant effort with repetitions traded away, and a flat sequence (12/12/12) to mean
a fixed repetition target with effort climbing across sets — RPE 6, 7, 8 — so the early sets were
submaximal and the load should rise. The strong form: a progression engine can classify execution
mode from weight, repetitions, set order, exercise and session timestamps, with no effort field.

**What the evidence actually shows.**

*Nobody has published this method, in any field.* No resistance-training log-mining literature
exists in the biomedical index at all. The one computational attempt found anywhere is King et al.
(2025), and it is the shape of the answer: to classify near-failure state they attached a wrist
IMU, segmented repetitions with a neural network, and reached **F1 = 0.82** — a sensor, a model,
and 0.82, for a binary a bare log is being asked to produce for free.

*The general form of the question has a settled answer, and it is no.* Effort is internal load;
weight × repetitions is external load. Impellizzeri et al. (2023): "a given external (training)
load can correspond to **different internal loads between and within individuals at different
times**", because internal dose is mediated by genetics, metabolism, susceptibility and changes in
state. Estimating internal from external is described as what researchers do *when the internal
measure is unavailable* — an acknowledged approximation, not a recovery. **The external-to-internal
map is one-to-many by the field's own framework**, and two execution modes differing only in
internal load are exactly the case it says is unresolvable.

*The two trajectories are not two distributions that overlap — one of them is censored.* Under a
fixed repetition target the repetitions cannot exceed the target, so the observation is not a noisy
measurement of capacity; it is capacity **truncated at a constant**. The flat sequence is produced
by construction and carries no information about the effort behind it. **That is why this is
non-identifiability rather than low power: no volume of data separates a censored constant from an
uncensored one sitting at the same value.** The single exception is **undershoot** — a set logged
*below* the prescribed number is unambiguous, because the censoring operates only downward. It
says the lifter could not reach the target; it does not say why.

*The constant-effort baseline a discriminator would need is exercise-dependent and has never been
measured at three sets.* Refalo (2023) is the only fixed-RIR set-by-set data available: −25% to
−33% across **six** sets, at 3 RIR, four minutes' rest, **bench press only**. No source reports the
set-1-to-set-3 figure. What the literature does establish is that the expectation must differ by
exercise:

- **Shimano et al. (2006)**, 8 trained and 8 untrained men, sets to failure at 60/80/90% 1RM: a
  significant **intensity × exercise interaction**, more repetitions in back squat than bench press
  or arm curl at 60% 1RM, converging at 80-90%. Attributed to "the amount of muscle mass used".
- **Willardson & Burkett (2006)**: significant set-1-to-set-5 decline in both squat and bench at
  every rest interval; per-exercise magnitudes not retrievable.
- **Ribeiro et al. (2014)** (in this corpus): the fatigue index and its trainability differ between
  bench press and arm curl in the same subjects.

**The muscle-dependence of the drop-off is supported in direction and unmeasured in magnitude** —
the worst possible state for a heuristic needing a per-exercise threshold.

*Session position is a third explanation for the same observation, and it is the best-evidenced
one.* Simao et al. (2012), the Sports Medicine review of exercise order: "exercise order affects
repetition performance over multiple sets, indicating that the total repetitions, and thus the
volume, is greater when an exercise is placed at the beginning of an RT session" — **regardless of
muscle group size**. So a flat sequence late in a session has at least three live explanations:
muscle-specific fatigue resistance, prior local fatigue from position, and genuinely submaximal
early sets. The log distinguishes none of them. This corpus's `exercise-order-within-a-session`
note covers order's effect on *growth* (null) and says nothing about its effect on the *repetition
trace*; this fills that gap, and fills it against the discriminator.

*The between-session noise floor is the same size as the within-session signal.* Bachero-Mena et
al. (2025), 16 women, two sessions: maximum repetitions at a fixed load had a between-session
**CV of 16.1% at 50% 1RM and 20.8% at 80% 1RM**. Typical within-session drop-offs on
fatigue-resistant movements run below that. **For most exercises the signal being sought is
smaller than the session-to-session wobble on any single repetition count in it.** That is 16
women and repetitions-to-failure rather than sets stopped short, so it transfers loosely — but
nothing plausible about the transfer shrinks a 16-21% CV to the few percent a discriminator would
need.

*Even a measured effort report is uninterpretable without a per-exercise expectation.* Lyristakis
et al. (2026): **104 participants, 8 weeks, 3 sessions a week, 50% or 80% 1RM**. Set number and
repetition number significantly predicted RPE (p < 0.001) — **so the claimed effort rise across
sets is real and measured**. But exercise dominated it: shoulder press averaged **17.2 ± 2.3**
against calf raise **13.3 ± 2.6** under identical relative-intensity prescriptions, a gap of
roughly 1.5 within-exercise standard deviations. The authors conclude percentage-1RM prescription
controls the stimulus more reliably than RPE does. **If a directly reported effort number needs
per-exercise calibration before it means anything, a number inferred from repetition shape needs
more, not less.**

*Timestamps do not help and may hurt.* The best quantity a `start_time`/`end_time` pair yields is
session duration over set count — an average conflating rest with plate changes, warm-ups, machine
queues and phone time, attributable to no single exercise. Rest is also self-selected and variable:
Alonso-Aubin et al. (2024) measured **97.3 ± 23.7 s** self-selected between the first two back
squat sets at 80% 1RM. Rest causally moves the quantity the discriminator reads, which this corpus
already holds. Worse, the two modes plausibly *cause* different rest — a lifter determined to hit
12 has a reason to rest longer. **That makes rest a common consequence of the mode rather than an
independent signal, so conditioning on it can manufacture the association it appears to explain.**
That last step is reasoning, not a finding.

*The other candidate signals have no validation.* Load relative to estimated 1RM needs a 1RM, hence
a load-repetition model whose between-person spread this corpus already records and which Shimano
shows is exercise-dependent too. A first-set-to-last-set ratio as a "fatigue index" is not a
validated construct in resistance training — the term returns Wingate power-decline and isokinetic
endurance measures, different quantities in different modalities. Session-to-session repetition
variance has no published interpretation. **No coaching software with published validation of any
log-only effort inference was found** — read as "none found via academic indices", not as a swept
field.

**What it does not settle.**

- **It does not claim the two modes are physiologically identical.** They are not. It shows the
  *observable* is the same under both for any muscle whose constant-effort drop-off is small, and
  censored under one of them regardless.
- **It gives no per-muscle drop-off table**, because none exists. A future note finding one would
  change the arithmetic here and not the censoring argument, which is structural.
- **It does not rule out separation with a richer log.** Per-set timestamps, per-set rest, or bar
  velocity change the problem — King reaches F1 0.82 with a sensor. **None of this applies to a
  logging surface of our own that records more than Hevy does.**
- **It does not test a stopping-rule question.** No study asks a lifter which stopping rule they
  use and scores it against behaviour, so the reliability of asking is assumed rather than
  measured. **That is the largest untested step here.**
- **Whether execution mode is stable per lifter is unmeasured.** Asking once depends on it. It is
  plausible — a habit learned from how someone was taught to read a programme — and unestablished.
- **Undershoot's meaning is not settled**, only its unambiguity as an event. Whether a missed
  target should cut load, hold it, or be ignored is separate, and the 16-21% CV argues loudly that
  a single miss is noise.

**Provenance caveat.** Lyristakis's set-number coefficient is reported as significant without a
beta in the accessible abstract, so "effort rises across sets" is directionally sourced and
unquantified; the paper is paywalled. Its 17.2 / 13.3 values imply a Borg 6-20 scale and the
abstract does not say so — the comparison holds under either scale, but the numbers should not be
quoted without confirming. Willardson's per-exercise magnitudes were not retrievable. The search
reached academic indices only and never the grey literature where coaching software would publish.

**Where it touches the product.**

- **It closes the mode-classifier question with a negative.** Nothing should read repetition shape
  and assert an execution mode.
- **It supersedes the one constructive suggestion in
  `references/inferring-proximity-to-failure-from-logged-sets.md`.** That note called the
  flat-sequence heuristic "the only thing here worth prototyping", as a low-confidence flag. It is
  **no longer worth prototyping at all**: a flat sequence is produced by fatigue-resistant muscles
  under constant effort, by session position (Simao), and by fixed-target execution, and the flag
  cannot see the first two. Its false-positive rate is unbounded on back and shoulder work.
- **It makes the progression trigger's bet sharper rather than safer.**
  `references/progression-trigger-under-constant-effort-execution.md` establishes that a last-set
  or all-sets "top of the range" trigger silently never fires under constant-effort execution. This
  note adds that **the product cannot detect which mode it is dealing with**, so the trigger must
  be defensible under *both*. **A first-set trigger has that property and the alternatives do
  not:** under fixed-target execution set 1 sits at the target and the trigger is censored, so it
  simply does not fire — safe; under constant-effort execution set 1 is the least fatigue-
  confounded reading — correct. It fails safe in one case and reads the best available number in
  the other.
- **It argues for prescribing a repetition range terminated on effort, rather than a number.** A
  fixed number is what creates the censoring. A range the lifter is told to terminate on effort
  within converts a censored observation into an uncensored one. **It is the only intervention
  identified that makes the log more informative without asking the user anything**, and this
  product already prescribes ranges (`TD-009`) into a field that already exists (`ADR-016`) — what
  changes is how the range is described, not the data model.
- **If effort is ever collected, per-exercise is the granularity**, not per-set and not global.
  Lyristakis's between-exercise spread against the within-exercise SD is the reason.
- **It hardens `TD-017`'s deferred problem rather than solving it.** With `rpe` null on every
  observed set, and inference ruled out here, the progression rule's input is **completion against
  target and nothing else**.
