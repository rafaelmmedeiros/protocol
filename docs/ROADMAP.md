# Roadmap

The capability spine. It says what the system is meant to be able to do, in what order, and
what "done" means for each block of work. It is a statement of intent — nothing more.

Written and read through `/protocol-milestone`, which turns a milestone here into an executable
plan under `docs/milestones/`.

## How to read this file

A **milestone** is a set of capabilities that are worth shipping together and are worthless
apart. `M1` is not "the first sprint"; it is the smallest set after which the product does
something it could not do before.

Each milestone carries:

- **Depends on** — the milestones whose capabilities it builds on. A milestone with no
  dependency can start today.
- **Capabilities** — one bullet per thing the system will be able to do, written as a
  capability and not as a task. "Generate a training week from a user's preferences", not
  "create the `WeekGenerator` class".
- **Deliverables** — what a person can observe when the milestone is finished. It is the
  sentence the milestone is judged against.
- **Not in this milestone** — the capabilities a reader would reasonably expect here and will
  not find, so that their absence reads as a decision rather than an oversight.

## Two rules that make this more than prose

**A capability bullet is a literal key.** A milestone plan quotes its bullets verbatim, and the
plan is checked against this file character for character. That is what makes "every capability
of this milestone is covered by some step" a statement that can be proven rather than felt. A
bullet is therefore edited here with the same care as a database column: rewording one after a
plan quotes it breaks the link, and the plan has to be re-checked.

**No status lives here.** Not `in progress`, not `done`, not a percentage. A milestone that has
been planned has a directory under `docs/milestones/`; the `progress.md` inside it owns the
status, one entry per step. Status recorded in two places is status that disagrees with itself,
and the roadmap is the copy nobody updates.

## Milestones

### M1 — A training week from a training profile

**Depends on:** nothing. The walking skeleton carries it.

The first package of functionality, and the first time the product does what it exists to do:
the user describes how they intend to train, and the system builds a week of sessions from it.
It is the smaller half of the setup — the personal half. The equipment half is `M2`, and until
it lands the generator programmes against one stated assumption about the gym rather than
against a described one.

**Capabilities:**

- Capture a training profile: the goal, how many days a week the user will train, and how long
  a session can last
- Turn a weekly frequency into a split, using a template the literature supports
- Generate a week of sessions from a training profile
- Prescribe sets, repetitions and rest for every exercise in a generated session
- Show a generated week in the app, session by session, before it exists anywhere else

**Deliverables:** a signed-in user fills in a training profile and reads back a week of
sessions built from it. Every number on that screen — the split, the set count, the repetition
range, the rest interval, the exercise choice — traces to a `TD-###`, and none of them was
recalled.

**Not in this milestone:**

- Describing the equipment available. That is `M2`, and `M1` programmes against a single
  documented assumption about what a gym has, decided and recorded rather than assumed
  silently.
- Reading training history out of Hevy. The first week is generated from a profile, not from
  what the user has actually been doing — history-aware programming is what makes the product
  worth its name, and it is the milestone after equipment, not before it.
- Writing anything back into Hevy. The week is read on screen. Pushing it into the logging
  surface is its own milestone, with its own failure modes.
- Progressing a week into the next one. `M1` generates a week, not a block.

### M2 — A week the user can live with

**Depends on:** `M1`. It replaces the assumption `M1` programmes against and gives the user the
first say in what they are asked to do.

`M1` proved the system can build a defensible week. `M2` is about the week being **performable
and acceptable** to the person reading it — which is a different property and, on this corpus's
own evidence, the one that decides whether they train at all. The strongest finding behind
exercise variety is motivation rather than growth, and the strongest predictor of long-term
adherence is consistency in the first weeks. A week nobody follows is worth less than a
slightly worse week somebody does.

It also closes the two gaps `M1` left surfaced rather than patched: three muscle groups with no
direct exercise under the assumed gym, and the `knee_flexion` hole `TD-004` names.

**Capabilities:**

- Describe the equipment actually available, replacing the single assumed gym `M1` programmes
  against
- State a preference between variants of the same movement, and have the generator honour it
  where honouring it does not cost coverage
- Substitute one exercise in a generated week for another that trains the same thing
- Show how long a generated session is expected to take, before it is trained

**Deliverables:** a signed-in user describes their gym, swaps a prescribed exercise they will
not do for one they will, sees the estimated duration of each session, and regenerates a week
that no longer contains movements their gym cannot perform. Every rule about what a preference
may and may not override traces to a `TD-###`.

**Not in this milestone:**

- Reading training history out of Hevy. Preference here is **stated**, not derived —
  `TD-004` records that deriving it from logged training is the better answer, and that is the
  milestone after this one.
- Progressing a week into the next one. Progression needs observed performance, not a described
  gym; see the note under `M3`.
- Writing anything back into Hevy.

### M3 — Closing the loop with Hevy

**Depends on:** `M2`.

The milestone where the week stops being a screen and becomes something trained from. The system
writes the generated week into Hevy as routines, the user trains from them in the gym, and a sync
brings back what actually happened — matched to the session that prescribed it.

**Reading and writing ship together, and that is a correction.** An earlier version of this
milestone excluded writing. A live experiment then established that the association between a
routine and the workout it produced is **Hevy's own**, carried on `routine_id`. So an import that
was never preceded by a push can only match at the level of an exercise, never at the level of a
prescribed session — which is precisely the comparison the product exists to make. The two halves
are one loop, and shipping the read half alone would ship the half that cannot be evaluated.

**Capabilities:**

- Connect a Hevy account with a personal API key the system can use and never reveals
- Push a generated week into Hevy as routines, remembering which routine belongs to which session
- Import training history out of Hevy, reconciling records that changed upstream
- Match a logged workout to the session that prescribed it, and read what was performed against
  what was prescribed
- Derive the available equipment from what has actually been trained, rather than from a
  description

**Deliverables:** a signed-in user saves their Hevy key, generates a week, pushes it, trains from
it in the gym, presses sync, and reads back each session with what was prescribed beside what was
performed — including the repetitions in reserve they reported, converted inbound by `TD-017`.

**Not in this milestone:**

- Progressing a week into the next one, and prescribing a working load. Both are `M6`, both read
  the same observed data, and neither is possible before this milestone has produced any.
- Prescribing effort into Hevy as data. A routine set has **no `rpe` field**, and that is Hevy
  modelling it correctly rather than a gap: RPE is feedback, reported after a set, and a plan does
  not carry an observation (`ADR-016`). The conversion runs inbound only, and the prescribed
  reserve reaches the user as displayed text.

### M4 — A catalogue that recognises the training

**Depends on:** `M3`. It is `M3`'s own measurement that makes this the next milestone rather than
progression.

The first import against a real account read 757 workouts and **5,186 logged exercises, of which
3,798 — 73% — are movements this catalogue does not model**: 126 distinct ones, dominated by the
selectorised machines `TD-004` excluded by assumption. Seated leg curl 162 times, hip abduction
132, leg press 131, leg extension 112.

A generator drawing from 36 rows against a lifter who trains 126 is not programming their week; it
is programming the quarter of it that it can see. And every one of those movements is volume it
does not count, so the arithmetic every other record rests on is reading a fraction of the truth.

**Progression waits for this and not the other way round.** Prescribing a load for exercises that
make up a quarter of someone's training would progress a quarter of their week, and the missing
three quarters would keep arriving as unexplained history.

**Capabilities:**

- Widen the exercise catalogue from what has actually been logged, ordered by how often each
  movement is trained
- Name equipment at the granularity an individual machine needs, so a session can prescribe one
- Record the convention imported load is expressed in, and count volume the same way on both sides
  of the loop
- Report what the catalogue still cannot explain, as the measure of how far it is from the training
- Erase everything belonging to one user on request, leaving the shared catalogue untouched

**Deliverables:** a generated week draws on the movements the user actually trains, including
machines; the proportion of logged exercises the catalogue cannot explain is reported and has
fallen; and every muscle attribution added traces to `TD-005` rather than to a guess.

**Not in this milestone:**

- Progressing anything, or prescribing a load. That is `M6`, and it reads this milestone's output.
- Deriving a catalogue row automatically from a logged exercise. Muscle attribution is a training
  judgement (`TD-005`), and `TD-015` already records curation as recurring work rather than a
  one-off price — the import says *which* movements to add, never *what they train*.

**Two records this milestone has to touch.** `TD-004` assumes a gym with no selectorised machines
and will not survive contact with a user who trains on them constantly; it is superseded here
rather than reinterpreted. And `ADR-013` decided an equipment item may name an individual machine —
a decision that has never been exercised, because until now the catalogue contained no machine to
name.

**On the erase capability, and why it is written down rather than treated as a utility.** It
contradicts standard 7, which says training history is append-only and a correction arrives as a
new record. What makes it acceptable *today* is that almost nothing here is irrecoverable: a week
is deterministic and regenerable, imported history comes back from Hevy, and equipment and
preferences are cheap to retype. **That stops being true the moment the system stores a judgement
Hevy cannot return, which is exactly what `M6` does.** So it is a development affordance with an
expiry, and the record that admits it must say when it expires.

### M5 — A week the user can read and actually run

**Depends on:** `M4`. It is the first feedback session against the shipped product that makes this
the next milestone rather than progression.

Every number the generator produces is defensible and none of them is legible. The Week screen
shows a title, `sets x reps`, a reserve and a rest interval; the split, the ordering, the muscle a
slot exists to train, the volume it credits and the shortfall the generator already computed reach
nothing. Asked where a week came from, the product cannot answer, and the engineer had to read the
source to find out. **`M6`'s third capability is to say why a week differs from the one before it,
which is unbuildable while a single week cannot be read at all.**

The second finding is worse than a legibility gap, because it is silent and it compounds. **The
generator receives no record of what was performed** — it takes a profile, a catalogue, equipment
and preferences, and nothing else. So a fixed weekday split whose fourth session is the one life
keeps taking produces a permanent per-muscle deficit that no screen names and no later week
repairs: the same target is declared every Monday as though the missed session had happened. The
system already imports the workout that did not happen and already compares prescribed against
performed; it simply never feeds either back into what it prescribes next.

**Fixed weekdays are ours, not the literature's.** `TD-003` excludes rotating splits on one
argument, and it is an analysis argument: a week that does not align to the calendar week makes
"which week did this session belong to" unanswerable. That conflates the **measurement window**
with the **shape of the prescription**, and they separate cleanly — what was performed can still
be bucketed into Monday-anchored weeks (root standard 6) while the plan advances as a queue.
`per-muscle-training-frequency` is graded `settled` and closes the training half: with weekly
volume fixed, how it is distributed across days does not change growth. So the queue costs no
growth, and the honest price is elsewhere — `TD-014`'s target is **weekly**, and a plan that
stops being a calendar week makes "the dose of this week" a question that has to be re-answered.

**Capabilities:**

- Show what a prescribed exercise trains, so a session can be read instead of trusted
- Report direct and indirect set volume per muscle group against the week's target, naming where
  it falls short
- Say why an exercise fills a slot, and what substituting it would change
- Choose the split for a training frequency, rather than receiving the only one mapped to it
- Carry a session that did not happen into the next week, rather than regenerating past it
- Report what a muscle has accumulated across weeks when the same session is repeatedly missed

**Deliverables:** a signed-in user opens a generated session and can see, for every slot, what it
trains and why it is there; the week states the direct and indirect volume each muscle group
receives against its target, including the muscles that fall short; the user picks a split their
frequency supports instead of receiving the only one mapped to it; and a session missed one week is
the one waiting at the top of the next, with the deficit it accumulated reported rather than
silently carried. No screen presents a split, an ordering or a substitution as better for growth,
because `TD-003`, `TD-007` and `TD-016` each record that it is not.

**Not in this milestone:**

- Prescribing a working load, or progressing a week into the next one. That is `M6`, and it reads
  this milestone's output the way this one reads `M4`'s.
- Reducing a week's volume on request. It is small, it answers a real need, and it is deliberately
  held: this milestone changes what "a week" is, and deciding how much of one to remove before
  that settles would be deciding it twice.
- **Inferring fatigue, a stall or a needed deload from logged repetitions.** Not deferred —
  ruled out, and the corpus is why. No trial defines, detects or intervenes on a stall; every rule
  in circulation is convention. The one direct deload trial found complete cessation *worse* for
  strength than training through. Between-session variation in repetitions at a fixed load runs
  **16-21%**, session position alone moves repetitions by about **25%**, and
  `separating-execution-modes-from-a-bare-log` answers the whole question with a one-word
  **"No."** The lifter who terminates a set on effort is a real sensor; that belongs in the
  prescription, which `TD-018` already makes, and never in a detector.

**Two records this milestone has to touch.** `TD-003` names "the user asks to choose" as the first
of its own revisit triggers, and this is that moment — it stops being a total mapping and becomes a
default. And `TD-016` currently asserts that slot count is `TD-012`'s minutes arithmetic, which is
**false**: the fill stops on `TD-014`'s volume target and consults the clock only to reject a slot
that will not fit, so session duration is inert from roughly 50 to 120 minutes. That correction is
being made **ahead of this milestone and outside it**, as one training record and one step, because
it is already wrong in production and a milestone directory for a single step is ceremony.

### M6 — Programming from what actually happened

**Depends on:** `M5`.

**This milestone has moved twice, and records written before each move still name it by the number
it had then.** It was `M4` until the catalogue milestone was inserted ahead of it — `ADR-016`,
`ADR-019` and `M3`'s plan and progress call it that — and `M5` until the readable-week milestone was
inserted ahead of it, which is what `ADR-024`, `ADR-025`, `ADR-026`, `M4`'s progress, root
standard 14 and `EraseUserData`'s doc comment call it. The decision records are append-only and are
left saying what they said; the always-loaded text and the code comments are corrected, because
nothing about them is a record of a decision taken at a time. This note is what makes the rest
readable. Nothing about the milestone changed except its position.

The milestone that makes the product worth its name: the system stops programming from what the
user *said* and starts programming from what they *did*.

**Capabilities:**

- Prescribe a working load for an exercise from what the user has actually lifted
- Progress a week into the next one from observed performance rather than from a schedule
- Say why a week differs from the one before it

**Deliverables:** a second generated week differs from the first because of what was logged
between them, and the reason it differs is readable.

**Why load and progression are one milestone.** They answer the same question — what to lift next
time — from the same observed data, and neither can be answered about a lifter the system has
never watched. Prescribing a load before the first sync would be the week-one calibration
`TD-001` refused, with nothing behind it.

**Why progression waits, and did not belong in `M2`.** The scheme is already researched —
double progression on ACSM's 2-10% rule, with repetition progression where the load increment is
too coarse, and no load carried across variants (`/protocol-training`,
`load-increment-granularity-and-progression`). What is missing is not the rule but the input.
Progressing without observing what was performed is adding sets by calendar, and ACSM's 2026
overview finds progression is not necessary for benefit at all outside continued long-term
progress. Blind progression is worse than none.

**What this milestone will have to decide and `M3` deliberately does not.** What triggers a step,
how large it is per `load_increment_kg` (`TD-015` deferred the column and said what would bring
it), whether a stall or a deload exists at all — and what weight a logged RPE of 6 carries, which
`TD-017` names as the weakest row in the conversion and leaves open on purpose.

**A precondition the trigger must not smuggle in.** Two people can satisfy the same prescription
by executing it in opposite ways, and a progression rule written without noticing that will work
for one of them and silently freeze the other.

*Terminate on effort.* The product's only user stops a set when the movement slows — a perceived
velocity-loss criterion — rather than at a rep count. Under 3 sets of 8-12 that produces 12/10/8
or 11/10/9: effort held constant, repetitions falling as the muscle fatigues. Their own progression
signal is graded rather than binary — 13/12/11 reads as *close to going up*, and 15/13/11 against a
target of 12 reads as *this load is beaten*. They also read a flat 12/12/12 as evidence the first
two sets were not honest, which is the same asymmetry
`references/inferring-proximity-to-failure-from-logged-sets.md` already states from the other
direction.

*Chase the number.* Someone else takes 12 as the instruction, stops at 12, and produces 12/12/12
with the early sets well short of the prescribed proximity. Their sequence is flat because the
prescription made it flat, not because they were fresh.

**A trigger keyed on "the first set exceeded the range" fits the first lifter and never fires for
the second.** A trigger keyed on "every set reached the top" fits the second and essentially never
fires for the first, because the last set will not reach the top at constant proximity. Neither is
a rule about training; both are rules about an execution style, and the record that picks one has
to say which style it assumes and what happens to the other.

**What would separate them without inferring anything is the reported effort** — 12/12/12 at RPE 7
is a lifter saying there was room, 12/10/8 at RPE 8 a lifter saying there was not. **In practice it
is not there.** Four consecutive workouts read out of the live account carry `rpe: null` on every
set, including two logged from routines the user had just built. `TD-017` named a partially-filled
history as the realistic case; the observed case is an **empty** one, and the only `rpe` value this
project has ever seen came from a workout logged during a deliberate experiment. So the
disambiguator exists in the schema and not in the data, and any rule depending on it has to say
what it does when the field is absent — which is always.

**Inferring the execution style from the shape of the drop-off is the thing not to do:** the corpus
grades that heuristic as untested and fit for a low-confidence flag, never as an input to
arithmetic, and a progression step is arithmetic.
`references/progression-trigger-under-constant-effort-execution.md` strengthens the prior without
validating it — even 3-RIR sets at four minutes' rest fell by a quarter to a third, so a flat
sequence is hard to produce at genuine constant near-failure effort — and explicitly leaves the
grade at `thin`.

## The horizon

Beyond the numbered milestones, and deliberately unscheduled — recorded because decisions taken
today are cheap to shape and expensive to unmake, and because an append-only history cannot be
retrofitted.

1. **Local.** One user, one machine, until there is an MVP worth showing.
2. **Published.** Known Hevy users train against it, and their use is what sharpens the
   reasoning. The auth cookie's `SameSite` is already configurable for this (root `CLAUDE.md`).
3. **Its own logger.** The product eventually logs sets itself, and Hevy becomes an integration
   a user may or may not have rather than the ground everything stands on.

Nothing here is a commitment to a date, and no milestone is planned against it. What it does is
settle a class of question in advance: wherever a choice makes Hevy load-bearing, the answer is
the one that keeps it removable. That is why an exercise is ours with their identifier in a
column beside it (`ADR-002`), why their identifiers never become primary keys (standard 8), and
why what this system derives is stored rather than recomputed on demand (`ADR-003`).

### What matching prescribed against performed will need

Undecided, and recorded now because the shape of a generated week is already stored and is
append-only. Comparing what was prescribed against what was logged is the product's reason to
exist, and these are constraints on it that were read out of Hevy's live payload rather than
assumed:

- **A pushed routine's identifier has to be stored beside the week that produced it** — a
  routine id per session, a folder id per week, as external columns (standard 8). Nothing in
  `M1` writes them, and adding them later is a forward-only migration over rows that will
  legitimately have nulls.
- **`routine_id` populates, and it was tested rather than assumed.** A routine was created
  through the API, trained from, and the resulting workout came back carrying that routine's
  identifier. So the association is Hevy's own and the match is a lookup — **nothing of ours
  needs to ride in a title or a description**, and the fallback options this note previously
  worried about are moot. An earlier version claimed the field was unreliable because an
  observed workout had it `null`; the account held no routines at all, so that observation
  supported nothing.
- **A routine's own notes do not survive into the workout.** The same experiment sent a note on
  the routine's exercise and the logged workout came back with it empty. If per-exercise
  metadata is ever needed, that is not the channel.
- **A workout inherits the routine's title.** Useful as a second confirmation, never as the key.
- **The fallback still has to exist, for workouts that came from no routine.** Training without
  starting from the routine is ordinary, and that history still matters — but only at the
  exercise level, which is where progression reads anyway.

- **Observed so far, the unbound case is not the exception — it is all of it.** Every workout read
  back from the live account carries `routine_id: null`, including two the user reports having
  built as routines and trained. That does not contradict the experiment, which was controlled and
  did populate the field; it does say the binding rate is an open empirical question rather than a
  formality, and `ADR-019` already names that rate as the evidence that would justify revisiting
  it.
- **Sets carry a `type`, and `warmup` is one of its values.** Counting warm-up sets as
  fractional volume would inflate every number the system produces (`TD-006`). The import
  filters on this or it is wrong from the first row.
- **A set carries `rpe`, and this account fills it.** The experiment above came back with
  `rpe: 9`. That matters more than it looks: `TD-010` names the gap between prescribed and
  performed RIR as the largest in the whole prescription, and
  `references/inferring-proximity-to-failure-from-logged-sets.md` establishes it **cannot** be
  recovered from weight and repetitions — device-free inference is off by three to six
  repetitions — while self-report lands within about one. So the gap closes by a field, the
  field already exists in Hevy, and the user already uses it. Whether to depend on it is a
  decision: it is optional, so a partially-filled history is the realistic case, and depending on
  it also trades against `TD-001`'s "observe, do not ask" posture.
- **RPE is theirs and RIR is ours, and `TD-017` maps between them in both directions.** Standard
  17 settles where the map lives; `TD-017` settles what it says. Inbound, every half point
  collapses toward the lower repetition count — discard Hevy's "maybe", which always sits on the
  upper value — so a reported 8.5 stores as 1 rather than as 1.5 or as a range. Outbound the map
  is exact, and `TD-010`'s 3 / 2 / 2 writes as RPE 7 / 8 / 8. **The domain represents no
  uncertainty about effort**, which is the part most likely to be argued with later: a repetition
  in reserve is a count, and reintroducing Hevy's half points as a fraction or an interval would
  be their shape reaching the model under a different name.
- **A logged `rpe` of 6 is the weakest row in that table and its error has no ceiling.** Hevy
  words it "4+ more reps", so it is a floor rather than a measurement, and it lands in exactly
  the region where `references/inferring-proximity-to-failure-from-logged-sets.md` puts
  self-report error above two repetitions. Whatever rule progresses from observed effort has to
  decide what a 6 weighs; `TD-017` deliberately does not.
- **The catalogue has to grow, and `M3` is what tells us where.** Building `S3.6` established that
  **every exercise in the catalogue is performable in the assumed gym**, because the catalogue was
  built for it (`TD-004`) — so equipment inferred from history can only ever help a user who
  narrowed their gym, and the real account's `Iso-Lateral Row (Machine)` arrives as a **catalogue
  gap** rather than as a suggestion. That gap report is the useful half today, and it is also the
  input a widening should be driven by: the exercises to add are the ones users actually log, named
  by their external identifier and counted, rather than the ones that seem missing in the abstract.
  Widening the catalogue is not itself a training judgement — but the muscle attribution of each
  new row is (`TD-005`), and `TD-015` already records that curation is recurring work rather than a
  one-off price.
- **Logged exercises will fall outside the catalogue and outside the assumed gym.** The same
  account logs `Iso-Lateral Row (Machine)`, which `TD-004` excludes by assumption. That is the
  loud failure `TD-004` chose over a silent one, and it is the signal that deriving equipment
  from history beats assuming it.
