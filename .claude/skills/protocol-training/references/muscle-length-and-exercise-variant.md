---
topic: muscle-length-and-exercise-variant
confidence: contested
bearing: Two meta-analyses disagree about whether long-muscle-length training grows more. Small effect at best; not enough to make it a selection rule, but enough to justify preferring a lengthened-biased variant as a free tie-break when two exercises fill the same slot equally well.
sources:
  - Maeo S, Wu Y, Huang M, et al. (2023). Triceps brachii hypertrophy is substantially greater after elbow extension training performed in the overhead versus neutral arm position. Eur J Sport Sci 23(7):1240-1250. https://onlinelibrary.wiley.com/doi/10.1080/17461391.2022.2100279
  - Maeo S, Huang M, Wu Y, et al. (2021). Greater Hamstrings Muscle Hypertrophy but Similar Damage Protection after Training at Long versus Short Muscle Lengths. Med Sci Sports Exerc 53(4):825-837. https://doi.org/10.1249/MSS.0000000000002523
  - Wolf M, Androulakis-Korakakis P, Fisher J, Schoenfeld B, Steele J (2023). Partial vs full range of motion resistance training - A systematic review and meta-analysis. Int J Strength Cond 3(1). https://journal.iusca.org/index.php/Journal/article/view/182
  - Varovic D, Wolf M, Steele J, Schoenfeld BJ, Grgic J, Mikulic P (2024). Regional Hypertrophy with Resistance Training - Does Muscle Length Matter? A Systematic Review and Meta-Analysis. SportRxiv. https://sportrxiv.org/index.php/server/preprint/view/464
  - Muscle hypertrophy from partial repetition at long vs. short muscle length - A systematic review and meta-analysis (2025). Sport Sciences for Health. https://link.springer.com/article/10.1007/s11332-025-01586-5
  - Lengthened partial repetitions elicit similar muscular adaptations as full range of motion repetitions during resistance training in trained individuals (2025). PeerJ. https://pubmed.ncbi.nlm.nih.gov/39959841/
last-reviewed: 2026-08-23
---

**What is claimed.** That training a muscle at long lengths — overhead triceps extensions rather
than pushdowns, seated rather than prone leg curls, deep rather than shallow ranges, "lengthened
partials" — produces more hypertrophy than the short-length equivalent, and that exercise
selection should therefore be biased toward lengthened positions.

**What the evidence actually shows.** A genuinely split literature, in which the largest effects
come from the narrowest comparisons.

*The within-subject biarticular trials are striking and consistent.* Maeo et al. (2023): 21
adults, 12 weeks, cable elbow extension overhead in one arm and neutral in the other, 70% 1RM,
5 sets, 2x/week — whole triceps grew ~19.9% overhead versus ~13.5% neutral, and the long head,
which the overhead position lengthens, grew far more (reported around +28.5%). Maeo et al.
(2021): 20 adults, same design, seated (hip-flexed, hamstrings long) versus prone leg curl —
whole hamstrings +14% versus +9%, the biarticular heads +8-24% versus +4-19%, and the
*monoarticular* head, whose length does not change with hip position, +10% versus +9%. The
monoarticular null is what makes these trials persuasive: the effect appears exactly where the
mechanism predicts it and nowhere else.

*The meta-analyses do not reproduce the effect at that magnitude, and disagree with each other.*

- Wolf et al. (2023), partial versus full ROM: a trivial SMD of 0.13 (95% CI -0.01 to 0.27)
  favouring full ROM overall — but subgroup analysis suggested a benefit to *partial ROM at long
  muscle lengths* over full ROM (SMD -0.28, 95% CI -0.81 to 0.16, an interval that plainly
  crosses zero). Their framing is that what matters is the muscle length trained, not the
  distance the weight travels.
- The 2025 Sport Sciences for Health meta-analysis of partial reps at long versus short length
  pooled 8 studies and found a significant but small effect favouring long length (ES = 0.283,
  95% CI 0.04-0.52, p = 0.036), across mostly quadriceps sites.
- Varovic et al. (2024) pooled 12 studies on *regional* hypertrophy and reached the opposite
  conclusion: trivial effects across proximal, mid and distal sites (SMDs 0.04 to 0.09,
  estimated with relatively high precision), concluding that training at longer mean muscle
  length does not produce greater regional hypertrophy — explicitly bounded to the contrasts
  typically used in the literature, an average 21.8% difference in mean muscle length.
- A 2025 PeerJ trial found lengthened partials elicited *similar* adaptations to full ROM in
  trained individuals.

**These are not all answering the same question**, and that is most of the disagreement. Maeo
contrasts two positions of a biarticular muscle at equal ROM — the largest achievable length
difference. Varovic pools whatever length contrast the included studies happened to use, and
says so. The 2025 meta compares partial ranges. A fair synthesis: the effect is real where the
length contrast is large and the muscle is biarticular, and shrinks to trivial at the contrasts
ordinary exercise selection produces.

**What it does not settle.**

- **Whether the effect survives outside biarticular muscles.** Maeo's own monoarticular control
  says it does not, by the same mechanism. Most muscle groups a programme touches are not
  biarticular in the relevant way.
- **The magnitude that would apply to us.** Best case for the general claim is ES ~0.28 with a
  CI reaching 0.04; a competent meta-analysis puts it at 0.04-0.09. Set against ~0.24%
  additional muscle thickness per weekly set (Pelland 2025, in
  `references/per-muscle-training-frequency.md`), the plausible range of this effect is
  comparable to a couple of sets a week. That is not nothing and it is not a programming
  principle.
- **It is a claim about exercise *variants*, not slots.** Nothing here says a programme needs a
  lengthened-position exercise; it says that between two candidates for the same slot, one may
  be slightly better.
- **Load, stability and preference are unmeasured confounds at the gym.** Overhead extensions
  and seated leg curls both require specific equipment; the trials used a cable machine and a
  leg curl machine respectively. A user without them cannot act on this at all — and `TD-004`
  assumes exactly such a user.
- **Short durations, small samples, healthy young adults.** 12 weeks, n around 20
  within-subject.
- **Nothing found tests whether biasing an entire programme toward lengthened positions produces
  more growth than a conventional one.** Every trial isolates one muscle.

**Provenance caveat.** The Maeo 2023 per-head percentages (+28.5% long head, ~19.9% vs ~13.5%
whole triceps) could not be opened at source — both publishers returned 403. The direction and
the monoarticular control in Maeo 2021 are corroborated across sources; the exact percentages
are not.

**Where it touches the product.**

- **Not a selection rule in `M1`.** The evidence does not support requiring a lengthened-position
  exercise per muscle group, and encoding one would over-apply a contested small effect.
- **A defensible free tie-break.** Where the catalogue holds two exercises for the same movement
  pattern, equipment class and muscle map, preferring the lengthened-biased one costs nothing
  and is weakly supported. If used, it lives in `preference_rank` as catalogue ordering, not as
  a generator branch — the distinction matters because the ordering is revisable without
  touching the generator.
- **It argues against adding a `lengthened_position` attribute now.** A contested attribute in
  the schema invites a future session to branch on it. `TD-005` records the omission rather than
  the column.
- **Reopens if a large, well-powered ROM/length meta-analysis lands**, or if the Varovic-versus-
  2025 disagreement resolves. It is the most actively moving area in this note set.
