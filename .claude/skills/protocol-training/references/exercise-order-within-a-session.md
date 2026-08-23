---
topic: exercise-order-within-a-session
confidence: contested
bearing: Exercise order has no detectable effect on hypertrophy; what goes first gets the strength gain. Ordering is therefore free for growth — the specific convention we adopt is ours (`TD-007`), and deliberately placing a small muscle last costs nothing measurable, while pre-exhaustion is the one scheme with evidence pointing mildly against it.
sources:
  - Nunes JP, Grgic J, Cunha PM, et al. (2021). What influence does resistance exercise order have on muscular strength gains and muscle hypertrophy? A systematic review and meta-analysis. Eur J Sport Sci 21(2):149-157. https://onlinelibrary.wiley.com/doi/abs/10.1080/17461391.2020.1733672
  - Hermann T, et al. (2025). Front-Loading Fatigue - Does the pre-exhaustion method influence resistance training-induced muscular adaptations? SportRxiv. https://sportrxiv.org/index.php/server/preprint/view/564
  - Trindade TB, et al. (2019). Effects of Pre-exhaustion Versus Traditional Resistance Training on Training Volume, Maximal Strength, and Quadriceps Hypertrophy. Front Physiol. https://pmc.ncbi.nlm.nih.gov/articles/PMC6882301/
  - ACSM (2009). Position Stand - Progression Models in Resistance Training for Healthy Adults. Med Sci Sports Exerc 41(3):687-708. https://pubmed.ncbi.nlm.nih.gov/19204579/
  - ACSM (2026). Position Stand - Resistance Training Prescription for Muscle Function, Hypertrophy, and Physical Performance in Healthy Adults - An Overview of Reviews. https://pubmed.ncbi.nlm.nih.gov/41843416/
last-reviewed: 2026-08-23
---

**What is claimed.** That exercises should be sequenced multi-joint before single-joint and
large muscle group before small — ACSM's long-standing guidance — and, in the opposite
direction, that deliberately placing a small muscle last (or pre-exhausting a muscle with
isolation before its compound) exploits accumulated fatigue productively.

**What the evidence actually shows.**

*For hypertrophy, order does nothing measurable.* Nunes et al. (2021) pooled 11 studies of
good-to-excellent methodological quality, 268 participants: **ES = 0.03, p = 0.862** for
hypertrophy combining site-specific and indirect measures. That is a null with a point estimate
at zero, from the only meta-analysis on the question.

*For strength, order matters and the rule is specificity, not anatomy.* The same meta-analysis
found gains largest in whatever is performed at the start of a session, and the effect ran both
ways: multi-joint-first produced better strength gains **in the multi-joint exercises**
(ES = 0.32, p = 0.034), and single-joint-first produced better strength gains **in the
single-joint exercises** (ES = -0.58, p = 0.032). Across all strength tests pooled together
there was no overall difference between orders. **This is not "compounds first is better." It is
"whatever you put first improves most, at the tested task."** ACSM 2026's overview of reviews
reaches a compatible place: strength training performed at the beginning of a session enhanced
voluntary strength gains, while set structure and sequencing did not consistently differentiate
outcomes.

*So the ACSM 2009 rule is a strength/quality rule applied to a hypertrophy problem.* ACSM 2009
recommends large before small, multi-joint before single-joint, higher before lower intensity —
and its stated rationale is preservation of exercise intensity and maximising performance of the
multi-joint exercises, not hypertrophy. Read as written, it does not conflict with Nunes; it is
answering a different question. **There is no evidence that the conventional order grows more
muscle, and this must not be laundered.** It is the same failure mode as "train each muscle
twice a week" in `references/per-muscle-training-frequency.md`: a finding about one outcome
wearing another outcome's clothes.

*The engineer's case — a small muscle placed last, deliberately pre-fatigued.* Nunes covers this
directly: the small muscle placed last will gain less strength in its own exercise (ES = -0.58
in the reverse direction) and no less size (ES = 0.03). Fatigue reduces the load and reps that
exercise can carry, but volume-load reduction of this kind does not translate into a hypertrophy
deficit in the pooled data. **Placing arms or delts last is defensible and costs nothing this
literature can detect.** It is not, however, positively beneficial — nothing shows pre-fatigue
*adds* stimulus, and a decision should not claim it does.

*Pre-exhaustion — isolation immediately before its compound — is the one variant with evidence
pointing against it.* Hermann et al. (2025), 48 resistance-trained participants (mean 22.5y),
8 weeks, leg extension/squat and hamstring curl/RDL pairings: traditional order showed slightly
greater improvements in muscle size and body composition, with acknowledged statistical
uncertainty; strength, endurance and power were comparable. The authors' conclusion is that
traditional order is the better option for hypertrophy. Trindade et al. (2019) reached the same
practical place: pre-exhaustion raises isolation-exercise activation and lowers the compound's
load without a hypertrophy payoff. **Pre-exhaustion is not supported. It is not clearly harmful
either — the effect is small and uncertain — but there is no reason to build it.**

**What it does not settle.**

- **One meta-analysis, 268 participants, mixed hypertrophy measurement quality.** Several
  included studies used indirect measures. This is why the note is `contested` rather than
  `settled`: the null is clean but thinly powered, and a small order effect would not be visible
  in it.
- **Any ordering rule the product adopts is convention.** This is the important boundary. The
  evidence licenses "order is free for growth"; it does not license the specific table anyone
  writes. Multi-joint-first is defensible on technique quality under fatigue, injury risk in
  loaded spinal and overhead patterns, and load preservation — **none of which is measured in
  any source here.** That reasoning is `thin`, and it is why the convention itself lives in
  `TD-007` rather than in this note.
- **Session length and adherence are untested as ordering consequences.** Nothing found
  addresses whether a fatiguing order costs completion.
- **Safety is not covered by any of this.** No source found tests injury outcomes by sequence.
  The case for not placing a heavy barbell squat after twenty sets is a safety argument made
  without a study.
- **Trained young adults, 6-12 weeks.** Same population limit as the rest of the corpus.
- **Nothing tests order across a *week*** — only within a session. Whether the same muscle
  should lead on one day and trail on another is unstudied.

**Where it touches the product.**

- **Ordering is a deterministic convention the generator applies, citing `TD-007`, not the
  literature.** The literature's contribution is the permission slip: no ordering is better for
  growth, so pick one for reasons of fatigue, safety and technique and say so.
- **A small muscle placed last is explicitly allowed** and carries no evidential cost. The
  engineer's instinct is compatible with the evidence; it is simply not a benefit.
- **Pre-exhaustion is rejected** — no pairing of an isolation exercise immediately before a
  compound for the same muscle in `M1`.
- **It constrains the schema.** Ordering must be computable from stored attributes — an
  `order_class` on the exercise (`TD-005`) — rather than hardcoded per template, or the
  convention becomes unrevisable when this note changes.
- **`TD-003`'s templates say which muscles on which day; this says the within-day sequence is
  free for growth.** Together they close session structure and leave `S1.4` only the
  prescription.
