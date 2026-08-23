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
- **That identifier is necessary and not sufficient.** A logged workout carries `routine_id`,
  but it can be `null` — an observed session in this account is titled `Pull`, is plainly
  structured, and has no routine on it. So the match cannot depend on it, and whatever fallback
  is chosen — date plus `exercise_template_id` plus set structure is the obvious one — is lossy
  and is a decision that needs a record rather than an implementation.
- **Sets carry a `type`, and `warmup` is one of its values.** Counting warm-up sets as
  fractional volume would inflate every number the system produces (`TD-006`). The import
  filters on this or it is wrong from the first row.
- **A set carries `rpe`.** It is null in the observed data, but it is the channel that would
  close the gap `TD-010` names as the largest in the whole prescription: a prescribed RIR is not
  necessarily the RIR performed, and nothing observes the set.
- **Logged exercises will fall outside the catalogue and outside the assumed gym.** The same
  account logs `Iso-Lateral Row (Machine)`, which `TD-004` excludes by assumption. That is the
  loud failure `TD-004` chose over a silent one, and it is the signal that deriving equipment
  from history beats assuming it.
