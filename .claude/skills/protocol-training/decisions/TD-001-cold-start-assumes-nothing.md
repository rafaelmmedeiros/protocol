---
id: TD-001
title: The generator assumes nothing about a user it has never observed, and starts everyone conservative
status: active
knowledge: [references/training-status.md, references/cold-start-first-block.md]
decided: 2026-08-23
---

**Decision.**

`M1` infers no training status, asks for none, and runs no calibration. Every user with the
same goal and the same availability receives the same week.

Three parts, and each is a refusal of something that was on the table:

1. **No status variable, inferred or declared.** Not a profile field (`ADR-004` already removed
   it), and not a value derived from anything else either. Nothing in the generator branches on
   how experienced a user is.
2. **No calibration in week one.** No AMRAP, no RIR-anchored test session, no
   estimate-your-1RM step before the first prescribed week. The first week the user sees is
   training, not measurement.
3. **The first week sits in the lower half of the effective volume range.** `TD-001` does not
   pick the number — that is `S1.4`'s job — but it binds it: the prescription lands in the
   4-12 fractional weekly sets per muscle group region, and a number above 12 may not cite
   this record. Below ~4 is under the minimum effective dose and equally out of bounds.

**Why this and not what the literature would suggest.** For once these do not diverge, which is
worth stating explicitly so a later reader does not mistake this for a usability compromise.
The literature does not describe a better-targeted first block that we are declining to build:
ACSM's 2026 overview, built on 137 systematic reviews, does not differentiate hypertrophy
protocols by training status at all, and ACSM 2009 — the source of the
novice/intermediate/advanced vocabulary — prescribed identical hypertrophy loading for novice
and intermediate. There is also nothing to key on: training status has no validated definition
(Buckner 2017), the one published classification model is an unvalidated proposal, and
individual response variance within a single status is enormous (Hubal 2005: -2% to +59% biceps
CSA on one identical programme). A status label would be a made-up input to a prescription that
would not use it.

The calibration refusal is the one place where a defensible alternative existed, and it is
declined on `thin` evidence. Novices misestimate proximity to failure by 4-5 repetitions
(Steele 2017), so a week-one calibration is noisiest exactly where the system knows least — and
an accurate one has to be taken near failure, making the hardest, least enjoyable session in the
programme the first one a user meets.

**What it costs.**

- **A true advanced trainee gets a week below what they could productively handle.** Held
  against the trained-specific 12-20 weekly sets (Baz-Valle 2022), a start at the lower end is
  roughly one tier light. At ~0.24% additional muscle thickness per weekly set (Pelland 2025),
  four sets low for four weeks is a fraction of a percent — recoverable, and erased once
  escalation exists. This user's real complaint will be that the week feels easy, and they will
  be right.
- **A true beginner gets a week that is safe but not tailored.** They receive the same
  prescription as someone with five years of training. The evidence says this costs them very
  little physiologically; what it costs is the sense of being programmed *for*.
- **Both costs are one-directional and deliberate.** Starting light costs recoverable
  hypertrophy; starting heavy costs adherence, and in a cohort of 522,994 app users only 18.1%
  of beginners were still training at six months, with consistency in the first 28 days the
  strongest predictor of whether they stayed. Growth foregone can be made up in week five. A
  user who quit in week two cannot.
- **The honest summary: this is a bet, not a finding.** No study tests cold-start strategies
  against each other. `references/cold-start-first-block.md` is graded `thin` and this record
  inherits that grade.

**How it shows up in code.**

- `Training/WeekGenerator` takes a `TrainingProfile` and a catalogue, and nothing else. There is
  no status parameter, no experience enum, and no branch keyed on either — the absence is the
  implementation, so a reviewer adding such a parameter later should be sent here.
- The generator produces one week and does not periodize across weeks. Escalation is out of
  `M1`'s scope entirely.
- The weekly set volume `S1.4` chooses carries `TD-001` beside its own record id where the bound
  is what is being justified.
- No generated session contains a test, an AMRAP, or a set prescribed to momentary failure as a
  measurement.

**When to revisit.** Any of these reopens it:

- **The system can observe response.** Once enough logged training exists to read rate of
  progression per user, the case for assuming nothing weakens — that is the observation
  `ADR-004` was holding out for, and it supersedes this record rather than amending it.
- **Escalation lands.** The moment a second week is generated from the first, "how fast to
  escalate" becomes a live question this record explicitly does not answer.
- **A trial tests cold-start strategies directly.** The `thin` grade is the whole reason to
  watch for one.
- **A user population arrives that this evidence does not cover** — older adults, return from
  layoff, injury history. Everything cited is healthy, mostly young adults.
