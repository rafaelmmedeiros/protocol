---
id: TD-020
title: Grip does not change what a curl trains; the forearm gets a direct exercise instead
status: active
knowledge: [references/grip-and-forearm-involvement-in-elbow-flexion.md, references/exercise-variant-and-implementation.md, references/indirect-only-volume-and-the-coverage-floor.md]
decided: 2026-08-24
---

**Decision.** Two halves, and the second is what makes the first affordable.

**The muscle attribution of curls stays as `M1` set it: `Forearms` is a secondary on every curl,
supinated, neutral or pronated.** Grip does not enter the model, and no curl is reclassified as a
forearm exercise on the strength of how the hand is turned.

**The forearm gets a direct exercise instead.** A wrist curl and a reverse wrist curl are seeded,
with `Forearms` as their primary. They train the forearm through a joint action — wrist flexion and
extension — that no curl reaches at any grip.

**Why this and not what the literature would suggest.** Here the literature and the decision agree,
which is worth saying plainly because the *proposal* went the other way and was well-motivated.

`M4` set out to narrow the supinated curls' attribution — drop `Forearms` from them, keep it on
neutral and pronated ones — so that a hammer curl and a reverse curl would differ from a bicep curl
in the model and could earn catalogue rows. The premise was that a supinated grip hands the work to
the biceps and spares the brachioradialis. **The premise is contradicted, and by the study that
tests it most directly.** Caufriez 2018 varies nothing but forearm rotation and finds the
brachioradialis very active in all three positions, with activity *slightly higher in supination*.
Uysal 2026, in the actual barbell curl, finds brachioradialis activation exceeding biceps activation
in the eccentric phase **particularly under the supinated grip**. Chen 2025 points the other way and
only partly, at 1 kg, isometrically, with the direction flipping by elbow angle.

There is a second objection that would hold even if the EMG had come out the other way, and it is
the stronger one: **all three measure acute activation, not growth.**
`exercise-variant-and-implementation` already holds that within-movement variants are null for
hypertrophy in four of five trials. Building a muscle map on EMG differences would be using
activation as a stand-in for training effect — which this corpus rejects elsewhere, and cannot
selectively accept where it happens to be convenient.

**What it costs.**

- **Three movements the engineer actually trains stay unmodelled**: `Hammer Curl (Dumbbell)` (31
  logged), `Reverse Curl (Barbell)` (28) and `Cross Body Hammer Curl` (16). They are, in everything
  this model represents, the dumbbell and barbell curls already seeded. They appear in the coverage
  report as movements this system does not represent rather than vanishing, and that is the honest
  reading: we cannot tell them apart, so we say so.
- **The engineer's felt experience is overruled by an EMG null.** A hammer curl does feel like a
  different exercise, and that feeling is not being called wrong — it is being called *not evidence
  of a different training effect*, which is a narrower claim. If a within-grip hypertrophy trial
  ever exists, this record is the one to reopen.
- **The forearm now costs slots.** `Forearms` has been in the `Upper`, `Pull` and `FullBody` session
  scopes since `M1` and in the uniform 6.0 weekly target (`TD-014`), but with no direct exercise it
  could only ever be fed by 0.5-weighted secondaries. With a wrist curl available it competes for
  slots like any other muscle, so a generated week now spends time on it that previously went
  elsewhere. That is a real change to every week generated for a user with a barbell, taken
  deliberately: the muscle was always in the model's target, and `indirect-only-volume-and-the-
  coverage-floor` puts the expensive failure precisely where nothing covers a muscle directly.

**How it shows up in code.**

- `ExerciseCatalogue.Pull.cs` keeps `Forearms` as a secondary on all nine curl rows, and carries the
  absence of the three hammer/reverse movements as a comment citing this record rather than as
  silence.
- `MovementPattern.WristFlexion` and `MovementPattern.WristExtension` exist so the wrist curls are
  not variants of `ElbowFlexion`. Two patterns rather than one, because the extensor side of the
  forearm is not the flexor side.
- `WeekPlan.UncoveredMuscles` no longer reports `Forearms` for a user with a barbell and a bench.

**When to revisit.**

- **A hypertrophy trial comparing grips within the same movement.** Not EMG. That is the only
  evidence that would move this, and none of the three sources is it.
- **The engineer finding generated weeks worse for the forearm slots.** The cost above is stated as
  a prediction; if wrist curls start displacing work that mattered more, the question is whether
  every muscle in the vocabulary deserves the same target — which is
  `muscle-group-specific-volume-requirements` (thin) and a different record.
- **One domain exercise being allowed to carry several Hevy template ids.** That is `ADR-002`
  territory and would let the three unmodelled movements import as the curls they duplicate. It
  would not change this decision; it would change what the coverage report says.
