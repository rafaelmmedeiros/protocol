---
topic: rir-based-rpe-scale-anchors
confidence: contested
bearing: Hevy's RPE anchors are the Zourdos/Helms RIR-based scale, so the field is directly readable as repetitions in reserve — but `RIR = 10 - RPE` is exact only at 7, 8, 9 and 10 and is not a published formula. The half points are intervals, not halves of a repetition, and Hevy's own descriptors disagree with the published ones at 9.5 and at 6. `TD-010`'s 3/2/2 maps to RPE 7/8/8 with no interpretation at all.
sources:
  - Zourdos MC, Klemp A, Dolan C, Quiles JM, Schau KA, Jo E, Helms E, Esgro B, Duncan S, Garcia Merino S, Blanco R (2016). Novel Resistance Training-Specific Rating of Perceived Exertion Scale Measuring Repetitions in Reserve. J Strength Cond Res 30(1):267-275. https://pubmed.ncbi.nlm.nih.gov/26049792/
  - Helms ER, Cronin J, Storey A, Zourdos MC (2016). Application of the Repetitions in Reserve-Based Rating of Perceived Exertion Scale for Resistance Training. Strength Cond J 38(4):42-49. https://pmc.ncbi.nlm.nih.gov/articles/PMC4961270/
  - Helms ER, Byrnes RK, Cooke DM, et al. (2018). RPE vs. Percentage 1RM Loading in Periodized Programs Matched for Sets and Repetitions. Front Physiol 9:247. https://www.frontiersin.org/journals/physiology/articles/10.3389/fphys.2018.00247/full
  - Bastos V, Machado S, Teixeira DS (2024). Feasibility and Usefulness of Repetitions-In-Reserve Scales for Selecting Exercise Intensity - A Scoping Review. Percept Mot Skills. https://journals.sagepub.com/doi/10.1177/00315125241241785
  - Rate of Perceived Exertion Based on Repetitions in Reserve Versus Percentage of One-Repetition Maximum in Cardiac Rehabilitation - A Pilot Study (2025). J Cardiovasc Dev Dis 12(1):8. https://pmc.ncbi.nlm.nih.gov/articles/PMC11766398/
  - Exercise type, training load, velocity loss threshold, and sets affect the relationship between lifting velocity and perceived repetitions in reserve in strength-trained individuals (2025). https://pmc.ncbi.nlm.nih.gov/articles/PMC12360324/
  - Objective Accuracy in Estimating Repetitions in Reserve in the Back Squat - An Analysis between Experienced vs. Novice Subjects (2025). https://pmc.ncbi.nlm.nih.gov/articles/PMC13215226/
  - Hevy. How to Calculate and Log RPE During Training. https://www.hevyapp.com/features/how-to-calculate-rpe/
  - Hevy. The RPE Scale Explained. https://www.hevyapp.com/rpe-scale/
last-reviewed: 2026-08-23
---

**What is claimed.** That the RPE field Hevy exposes — 6, 7, 7.5, 8, 8.5, 9, 9.5, 10 — is the
RIR-based RPE scale of Zourdos et al. (2016) rather than a Borg perceived-exertion scale, that
each anchor has a published repetitions-in-reserve meaning, and that `RIR = 10 - RPE` is that
meaning.

**What the evidence actually shows.**

*The identification is correct.* The anchor set is the signature of the RIR-based scale: a 1-10
range, half points present only from 7.5 upward, repetition descriptors at the top and effort
descriptors at the bottom. Borg's scales are 6-20 or CR10 and carry no repetition descriptors at
all. Helms et al. (2016) state the design rationale directly — repetition descriptors for scores
of 5-10, perceived-effort descriptors for 1-4, and 5-6 deliberately left as a *grouped range*
(4-6 RIR) "as it is easier for athletes to give a range of RIR when RIR is greater than 3."
**The half-point structure is not decoration; it is the scale conceding that its own resolution
is worse than one repetition.**

*The anchors, as published.* Zourdos 2016 Table 1: 10 "Maximum effort"; 9.5 "No further
repetitions but could increase load"; 9 "1 repetition remaining"; 8.5 "1-2 repetitions
remaining"; 8 "2 repetitions remaining"; 7.5 "2-3 repetitions remaining"; 7 "3 repetitions
remaining"; 5-6 "4-6 repetitions remaining"; 3-4 "Light effort"; 1-2 "Little to no effort".
Helms et al. (2018) corroborates the top of that table in peer-reviewed prose, near-verbatim
through 8.

*The half points are intervals, and 9.5 is not even that.* 8.5 and 7.5 name spans — [1,2] and
[2,3] repetitions — not fractional repetitions. 9.5 is a different construct: **0 RIR with load
headroom**, a statement about the weight rather than about repetitions. Reading 9.5 as "half a
repetition left" is a category error, and placing it at 0.5 puts it on an axis it does not live
on. **A repetition in reserve is a count.**

*Published variants disagree at the bottom.* A 2025 cardiac-rehabilitation trial reproduces a
version running to 5 with half points throughout — 6 = "four more", 5 = "five more" — and gives
10 as "no further repetitions could be performed" rather than "maximum effort". That variant
makes `RIR = 10 - RPE` exact at every whole point; Zourdos's original, with its grouped 5-6 row,
does not. Both are peer-reviewed. **Which version a "6" came from changes its meaning from
"exactly 4" to "somewhere in 4-6".**

*And Hevy's labels are a third variant.* Hevy's help text is not Zourdos's. Its 9.5 reads "could
have **maybe** done 1 more rep" — an interval of [0,1] — where the published anchor says "no
further repetitions but could increase load", which is 0 with headroom. **Those are one whole
repetition apart, at precisely the anchor a progression engine would read as "add weight".** Its
6 reads "could have done 4+ more reps", unbounded above, rather than the grouped 4-6. Hevy also
attributes the scale to Tuchscherer rather than Zourdos — a fair claim about lineage, and
irrelevant to the anchors.

**One structural fact about Hevy's wording is worth more than the rest of this note.** Every half
point places its uncertainty on the **upper** value: "1 more rep, maybe 2", "2 more reps, maybe
even 3". Never the reverse. So discarding the hedge always yields the lower repetition count —
the reading in which the lifter was *closer* to failure.

*Validation against something objective exists, and it is moderate.* Zourdos (2016), 29 squatters
(15 experienced, 14 novice): average concentric velocity correlated inversely with RPE at
r = -0.88 experienced and r = -0.77 novice. That is construct validity for the scale as an
ordinal effort measure — **not** evidence that a given anchor equals a given repetition count. A
2025 analysis of 2,972 velocity/perceived-RIR pairs from 19 well-trained lifters found velocity
explained only about **30% of the variance in perceived RIR**, and the relationship shifted with
exercise, load and accumulated velocity loss — roughly one whole RIR of offset on bench press
between a 20-30% and a 40-60% velocity-loss condition, and about 0.1 RIR per successive set.
**The same anchor does not denote the same physical state across contexts.**

*Accuracy by anchor is not measured; accuracy by proximity is.* Bastos et al. (2024), a scoping
review, reports 32% of included studies finding predictions less accurate the further from
failure, and recommends RIR be reported close to failure whenever possible. A 2025 back-squat
study (n=16, 70% 1RM) had both experienced and novice groups **undershoot** targets at RIR 3 and
RIR 1 by about 1-1.3 repetitions, with no group difference (d = 0.03 and 0.18) — consistent with
`references/inferring-proximity-to-failure-from-logged-sets.md`, which already holds that
experience does not predict accuracy. **The low end is the weak end by construction:** the
scale's own authors grouped 5-6 rather than split it, because people cannot resolve it.

**What it does not settle.**

- **Nothing establishes that the half points carry information.** Whether a lifter reporting 8.5
  is meaningfully distinguishable from one reporting 8 or 9 has not been tested, and no published
  distribution of logged RPE values was found — so whether users cluster on the integers is
  unknown in either direction. The product's own import corpus will answer this faster than the
  literature will.
- **The 9.5 anchor has no repetition meaning at all** in the source, and cannot be placed on a
  RIR axis without a product decision.
- **The bottom anchor is genuinely ambiguous** across three published or deployed wordings:
  exactly 4, the interval [4,6], or "4 or more". It is also the region every accuracy study calls
  least reliable, so the ambiguity compounds with the error instead of cancelling it.
- **`RIR = 10 - RPE` is a property of the top of one version of the table, not a published
  formula.** It is exact at 7, 8, 9 and 10 in every version found, undefined at the half points,
  and contested at 6.
- **No evidence on whether the anchors hold at high repetitions and low loads.** Validation work
  is 60-90% 1RM on squat and bench. The velocity study found lifters *underestimated* RIR at
  lighter loads despite greater fatigue, which points at a drift — but 77% 1RM is still not the
  12-15 repetition territory `TD-009` allows on isolation work.
- **Nothing here is about hypertrophy.** This note defines a unit. What RIR to prescribe remains
  `references/proximity-to-failure-and-hypertrophy.md` and `TD-010`.

**Provenance caveat.** Zourdos 2016 Table 1 could **not** be retrieved from the primary source:
the JSCR full text is paywalled and the accessible copy is a scan whose table did not extract.
Rows 10 through 8 are corroborated verbatim by Helms et al. (2018) and are safe. **Rows 7.5, 7
and the 5-6 grouping rest on one secondary reproduction plus the structural logic of the scale.**
`TD-017` responds to this by reading Hevy's own descriptors rather than the published table,
which makes the unverified rows non-load-bearing — but anyone who reinstates the academic table
inherits this gap.

**Where it touches the product.**

- **It makes `TD-010` expressible in Hevy's own field with no interpretation.** RIR 3 / 2 / 2 is
  RPE **7 / 8 / 8**, and Hevy's descriptors at those anchors say the same thing the prescription
  does. The outbound direction needs the exact part of the table and nothing else.
- **It sets a hard rule for import: never derive a fractional RIR.** An imported 8.5 is the
  interval [1,2], not 1.5.
- **The user's report means what the user was shown**, which is an argument for Hevy's
  descriptors over the published ones wherever they conflict. That is reasoning, not a finding,
  and `TD-017` carries it.
- **A logged 6 is the weakest row in the table and should not weigh like a 9.** Its error is
  unbounded above rather than capped at one repetition, and it sits where the accuracy literature
  is worst. Any progression rule that treats anchors uniformly is treating a floor as a
  measurement.
- **It does not weaken `references/inferring-proximity-to-failure-from-logged-sets.md`.** A
  reported RPE is self-report and that note's ~1-repetition accuracy figure still governs. This
  note fixes what the reported number *means*; it does not make it more accurate.
- **It is a boundary mapping, not a domain model** (root standard 17). RPE with half points is
  Hevy's representation; the domain's unit is repetitions in reserve.
