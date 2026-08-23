---
topic: ranking-exercise-variants
confidence: thin
bearing: The general "this variant is better" factor cannot be defended as a growth claim and survives only as a performability claim. The personal-fit factor has an evidence base — for adherence and affect, not hypertrophy — and Damas 2019 shows individual variation is overwhelmingly between *people*, not between people and protocols, which is the interaction a personal ranking would need. One curated draw order that asserts nothing about growth, plus a later user-level filter derived from logged behaviour. Never a weighted blend.
sources:
  - Damas F, Angleri V, Phillips SM, et al. (2019). Myofibrillar protein synthesis and muscle hypertrophy individualized responses to systematically changing resistance training variables in trained young men. J Appl Physiol 127(3):806-815. https://journals.physiology.org/doi/full/10.1152/japplphysiol.00350.2019
  - Baz-Valle E, et al. (2019). The effects of exercise variation in muscle thickness, maximal strength and motivation in resistance trained men. PLoS One 14(12):e0226989. https://pmc.ncbi.nlm.nih.gov/articles/PMC6934277/
  - Haugen ME, et al. (2023). Free-weight vs. machine-based strength training. BMC Sports Sci Med Rehabil 15:103. https://pmc.ncbi.nlm.nih.gov/articles/PMC10426227/
  - Larsen S, et al. (2025). Dumbbell versus cable lateral raises for lateral deltoid hypertrophy. Front Physiol 16:1611468. https://www.frontiersin.org/journals/physiology/articles/10.3389/fphys.2025.1611468/full
  - Effects of imposed and self-selected exercise on perceptual and affective responses, muscle function, quality, and functionality of strength training in older women and men - a randomized trial (2024). https://pmc.ncbi.nlm.nih.gov/articles/PMC11653500/
  - Nuzzo JL, et al. (2025). Within-individual design for assessing true individual responses in resistance training-induced muscle hypertrophy. https://pmc.ncbi.nlm.nih.gov/articles/PMC11825802/
  - ACSM (2026). Position Stand - Resistance Training Prescription - An Overview of Reviews. https://pmc.ncbi.nlm.nih.gov/articles/PMC12965823/
last-reviewed: 2026-08-23
---

**What is claimed.** That a variant's rank has two components — how good the variant is on average
(general quality), and how well the individual gets on with it (personal fit) — and that a
generator should compose them into a score.

**What the evidence actually shows.**

*Factor 1, as a growth claim, has no support and some direct contradiction.* Every candidate
mechanism for "this variant is objectively better" has been tested and returned null for
hypertrophy: constant tension and resistance profile (Nunes 2020; Larsen 2025, BF below 0.01 for
the null in trained lifters), modality (Haugen 2023, SMD -0.055, p=0.751), accommodating
resistance (2023 systematic review, 12 studies). ACSM 2026 found insufficient data on varied
exercise selection and no consistent load effect on hypertrophy. **There is no version of factor 1
phrased as growth that this literature will carry.**

*Factor 1 survives on axes that are not growth claims — and they are unequal.*

| Axis | Status | Can it justify a rank? |
|---|---|---|
| Load increment granularity | Arithmetic against ACSM's sourced 2-10% band | **Yes** — the strongest, and objective |
| Loadability at the target RIR | Mechanism, obvious | **Yes** — a variant that cannot take a load fails `TD-009`/`TD-010` outright |
| ROM afforded | Geometrically plausible, unmeasured across implementations | Weakly — mechanism only |
| Stability demand | No evidence; best proxy (Haugen) is null | **No** — `TD-005` omitted it and should keep doing so |
| Technical / skill demand | Convention only | **No** — this is `skill_level` through the back door, which `TD-001` refuses |
| Fatigue cost per unit stimulus | No evidence, no unit, no measurement | **No** — `TD-005` already names this the worst offender |
| Setup time | Not a training claim; real for `TD-012`'s clock | Only as an operations input, if ever measured |
| Injury risk | No trial found | **No** |

So the defensible general factor reduces to **performability and progressibility, not quality.**

*Factor 2 has a real evidence base, and it is about adherence, not growth.* Baz-Valle 2019 is the
cleanest datum: varied exercise selection produced a significant moderate improvement in intrinsic
motivation with no hypertrophy difference. The self-selected-intensity literature points the same
way — the 2024 RCT in older adults found self-selected load enhanced muscular fitness and was
perceived as more enjoyable, with no muscle-architecture difference over 12 weeks. **Autonomy
improves affect and plausibly adherence; it has not been shown to improve tissue.**

*The interaction a personal ranking would need has been looked for and is small.* Damas et al.
(2019) trained both legs of the same trained young men with systematically different training
variables. Between-subject variability was **40-fold greater** than within-subject, between-leg
variability (CSA 37.8% vs 0.9%; myofibrillar protein synthesis 3.30% vs 0.08%). That is the
closest thing to a test of "different exercises suit different people": **the variation lives in
the person, not in the person-by-protocol pairing.** Nuzzo et al. (2025) add the methodological
point that most claimed individual responses cannot be distinguished from measurement error
without exactly this kind of within-individual design.

**What it does not settle.**

- **Nobody has tested a person-by-variant interaction directly for hypertrophy.** Damas
  manipulated training variables, not exercise variants, and n was small. Absence of a demonstrated
  interaction is not a demonstration of its absence.
- **Nothing measures whether a preferred exercise is trained harder.** The plausible route by which
  preference becomes growth — taking a liked exercise closer to failure, skipping the session less
  — is mechanism.
- **Nothing supports a magnitude for any defensible axis.** Even the increment argument gives an
  ordering, not a score. **Any number attached to factor 1 is invented.**
- **Anatomical or morphological individualisation** — that a person's leverages make a variant
  genuinely better for them — was not found tested in any training trial.
- **A derived preference signal is untested as a signal.** That logged behaviour identifies
  exercises a user does badly with is an inference the product would be making, not one the
  literature has validated.

**Where it touches the product.**

- **`preference_rank` keeps the semantics `TD-005` gave it — a curated draw order — and must not
  become a quality score.** The exact permitted wording is fixed in `TD-015`.
- **Two factors must not be blended into one number.** Factor 1 has an ordering but no magnitude;
  factor 2 has evidence for a different outcome. A weighted sum lets an invented weight on a
  null-effect term override a real preference. The sane composition is **filter then order**:
  personal fit is a hard include/exclude at the user level, applied over an unchanged curated
  order.
- **Factor 2 is not modelled now.** `TD-004` already records the better route — derive it from
  imported history rather than ask — and asking creates a preference field with nothing behind it.
- **The UI constraint in `references/exercise-selection-within-a-movement-pattern.md` stands.**
  "Best exercise for X" remains unsupportable; "our default choice for this slot" is supportable
  and is a different sentence.
