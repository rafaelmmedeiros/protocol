---
id: TD-017
title: RPE is Hevy's, RIR is ours, and the domain carries no uncertainty about effort
status: active
knowledge: [references/rir-based-rpe-scale-anchors.md, references/inferring-proximity-to-failure-from-logged-sets.md, references/proximity-to-failure-and-hypertrophy.md]
decided: 2026-08-23
---

**Decision.** Three parts, and the third is the one that will be under pressure later.

**1. The domain unit is repetitions in reserve, as an integer.** There is no RPE anywhere inside
the system, no fractional RIR, and no interval — no `RirMin`/`RirMax` pair, no confidence field,
no nullable "approximately". A repetition in reserve is a count of repetitions, and half a
repetition is not a thing that can be performed. Hevy's scale represents uncertainty; ours does
not represent it, because representing it would be Hevy's shape reaching the domain wearing a
different name (root standard 17).

**2. Inbound — Hevy's RPE to our RIR, resolved at the boundary.**

| Hevy RPE | Hevy's own descriptor | RIR stored |
|---|---|---|
| 10 | *couldn't have done more reps with proper form* | **0** |
| 9.5 | could have **maybe** done 1 more rep | **0** |
| 9 | could have done 1 more rep | **1** |
| 8.5 | could have done 1 more rep, **maybe** 2 | **1** |
| 8 | could have done 2 more reps | **2** |
| 7.5 | could have done 2 more reps, **maybe** even 3 | **2** |
| 7 | could have done 3 more reps | **3** |
| 6 | could have done 4**+** more reps | **4** — a floor, see costs |

Arithmetically this is `RIR = 10 - ceil(RPE)` across the whole scale. Stated in the terms the
engineer used, it is **discard the "maybe"** — and those are the same rule, because every hedge
in Hevy's wording sits on the upper value ("1 more rep, maybe 2", never "2, maybe 1"). A missing
`rpe` maps to nothing, not to a default.

**3. Outbound — our RIR to Hevy's RPE, `RPE = 10 - RIR`.** Exact, integer to integer, no
interpretation. `TD-010`'s 3 / 2 / 2 writes as RPE **7 / 8 / 8**, and Hevy's descriptors at those
anchors say what the prescription says. **Only the inbound direction resolves anything;** the
outbound one is lossless, which is the asymmetry worth remembering when someone proposes making
them symmetric.

**This direction has no consumer, and that is by design rather than by circumstance.** A Hevy
routine set carries no `rpe` field, because `rpe` is *feedback* — reported after a set by the
person who performed it — and a plan does not carry an observation. `ADR-016` records why writing
a prescribed target into a field that means feedback would be actively harmful here: the workout
would inherit our own number, and the distance between prescribed and performed reserve, which is
the signal this whole conversion exists to expose, would collapse to zero and read as perfect
adherence. **Read that record before wiring this direction into anything.**

**Why Hevy's descriptors and not the published table.** They disagree, and by a whole repetition
at 9.5 — Zourdos 2016 defines it as *no further repetitions but could increase load* (0 RIR, a
statement about the weight) where Hevy says *maybe 1 more rep* ([0,1]). That is precisely the
anchor a progression engine reads as "add load". The tie goes to Hevy because **the user's report
means what the user was shown when they tapped it.** This is reasoning from the reporting
situation, not a finding, and the knowledge note grades it as such.

It also removes a real risk rather than accepting one. The note's provenance caveat is serious:
rows 7.5, 7 and the 5-6 grouping of the published table could not be verified against the
original, which is behind a paywall and available only as an unextractable scan. **Reading the
labels the app displays makes the unverified rows non-load-bearing.** Anyone reinstating the
academic table inherits that gap and should say so in a new record.

**Why the resolution runs toward less reserve.** Every half point collapses to the lower
repetition count, meaning the system assumes the set was *harder* than it may have been. The
alternative — collapsing upward, assuming more reserve — makes the system conclude the load was
comfortable and add weight. The asymmetry recorded in
`references/volume-progression-across-a-block.md` and
`references/load-increment-granularity-and-progression.md` points one way: under-progressing
costs a little growth on a curve with diminishing returns, while over-progressing drives a
programme prescribed at 2-3 RIR toward failure and breaks `TD-010`'s premise. Rounding toward
less reserve is the direction whose failure mode is recoverable.

**What it costs.**

- **Up to one repetition, on every half point, by construction.** A user who reports 8.5 meaning
  "genuinely nearer 2" is stored as 1. This is deliberate and it is a systematic bias, not noise:
  it never cancels across a history, it accumulates in one direction.
- **The 6 row costs more than the others, and its error is unbounded.** "4+" stored as 4 caps
  nothing — a lifter with 8 in reserve reports the same 6. Every other row is wrong by at most
  one repetition; this one has no ceiling, and it sits in the region
  `references/inferring-proximity-to-failure-from-logged-sets.md` already identifies as least
  accurate (error above 2 repetitions when the true remainder is 7-10). **A logged 6 is a floor,
  not a measurement, and a progression rule that weighs it like a 9 is treating an absence of
  information as information.** Nothing in this record fixes that; it names it for the record
  that will have to.
- **The half points are discarded on a guess about whether they carry information.** Nobody has
  tested whether a lifter reporting 8.5 is distinguishable from one reporting 8. If they are, the
  system is throwing away a real signal; if they are not, it is discarding noise. The import
  corpus will answer this before the literature does — and the raw Hevy payload is retained, so
  the question stays answerable.
- **It hard-codes a reading of another product's UI copy.** If Hevy rewords its descriptors, this
  mapping is silently wrong and nothing fails. That is the cost of choosing the displayed label
  over the published table, and it is accepted knowingly.

**Pre-empting the objection this record exists to survive.** A future session will look at the
mapping, notice that 8.5 means [1,2], conclude that storing 1 discards information, and propose a
range or a fractional value as the careful thing to do. It is not. It reintroduces Hevy's
uncertainty into a domain that deliberately has none, spreads the boundary across every consumer
of RIR instead of keeping it in one mapper (root standard 17), and — on the day the logging
surface is ours — leaves a concept in the model that exists only because a third party's scale
once had half points. **Read this paragraph before adding the field.** What may legitimately
change is the *direction* of the resolution or the treatment of the 6 row, and either is a new
record.

**How it shows up in code.**

- Two functions, one per direction, in the Hevy integration at the backend boundary — never in
  `Training/`. The domain has no symbol containing "RPE".
- The inbound one is total over Hevy's eight anchors and rejects anything outside them rather
  than rounding an unexpected value into range: a new anchor is a decision, not an input.
- A prescription's RIR stays the integer `TD-010` already defines; a performed set gains its own
  integer RIR, distinct from the prescribed one, because the gap between them is the signal.
- The raw payload is retained as imported (root standard 7), so a changed mapping can be
  recomputed rather than re-fetched.

**When to revisit.**

- **The import corpus has enough logged sets to show whether the half points are used.** If the
  user's history clusters on integers, the collapse is free and this record's largest cost
  evaporates. That is a query, not a study.
- **A progression rule is written.** It will have to decide what weight a 6 carries, and this
  record deliberately does not.
- **Hevy changes its descriptors**, which nothing will detect automatically.
- **The logging surface becomes ours.** At that point the scale is a product decision rather than
  a mapping, the "maybe" can be asked for or not, and what gets deleted is the mapper — not the
  model.
