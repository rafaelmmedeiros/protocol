# Milestone Workflow — Design

Status: proposed
Backlog row: `P6`
Date: 2026-08-22

## Problem

The product is about to grow its first real feature set: a user sets preferences, and the
system generates a training week — five sessions or three, built on templates the literature
supports, with prescribed rest. That is not one feature. It is a dozen, they have an order,
most of them make training judgements, and every one of them needs a test.

The repo has nowhere to put any of that.

- **No capability spine.** What the minimum viable system does exists in a conversation and in
  one paragraph of the root `CLAUDE.md` ("Direction"). Nothing enumerates the capabilities,
  their order, or what "done" means for the set as a whole.
- **No home for a technical decision.** Established in the session of 2026-08-22: only
  training judgements (`TD-###`) have an append-only record. An architectural decision that is
  not a training judgement and did not come from a harness pain has no file. The frontend
  proxy architecture (browser -> web -> api rather than browser -> api with CORS) is a live
  example: it is recorded as harness win `W2` — which captures the *lesson* ("read the
  framework's own docs first") and not the *decision*, its rejected alternative, or the reason.
  Answering "why does the browser not call the API directly?" in six months means git
  archaeology.
- **No executable plan and no execution record.** `/protocol-feature` is a craft ladder for one
  change. It has no notion of a unit of work inside a larger set, no dependency order across
  units, and nothing that survives the session in which the work happened.

The cost profile matches every other standard this repo adopted early: cheap now, and paid
back as a data migration or a re-derivation later. A generator built from recalled numbers
cannot be distinguished afterwards from one built from researched ones — root standard 15
exists precisely because of that, and it currently has no plan artifact to be enforced in.

## Evidence

Same shape as `P5` (training knowledge): recorded before a lived case, because the case is
already scheduled and the cost of recording it late is a rewrite rather than an edit. Two
observed facts back it:

1. The first milestone (`M1`) was described in conversation and had no artifact to land in.
2. The architectural-decision gap was found by asking, in the same session, where decisions are
   recorded — and the answer was five partial homes and one hole.

## What was analysed

`D:/Projects/mba-ia-greenfield-project` (StreamTube — NestJS + Next monorepo, seven phases,
three executed). Read in full: `CLAUDE.md`, `docs/project-plan.md`, the workflow diagrams, the
`plan-pipeline` / `research` / `decide` / `implement-phase` skills, and the complete artifact
set of phase 03 (decisions doc, context, validation, plan, progress).

### Adopted — mechanism 1: the capability spine

`project-plan.md` holds phases with declared dependencies, capability bullets and deliverables.
What makes it more than prose: the bullets are **literal keys**. A slice declares
`covers_capabilities: ["<bullet verbatim>"]` and a validator hard-fails on a single character
of divergence, which makes "every capability of this phase is covered" a provable statement.

### Adopted — mechanism 2: decisions as first-class documents

`docs/decisions/technical-decisions-{slug}.md`, one `TD-NN` per decision, each with Scope,
Capability, Context, Options (A/B/C with pros and cons), **Recommendation** and **Decision** as
separate fields. The single most valuable pattern found: `phase-02-auth/TD-02` recommends
Passport and decides custom guards, with a Note stating the divergence and the cost accepted.
The rejected alternative has a home, and so does the reason.

Its `/decide` front door triages free text against existing decisions and classifies the change
as Revision (parameter change, same option — appended to a `**Revisions:**` block), Supersede
(option letter changes — new record), Reaffirm, or Greenfield. That is the same append-only
model `/protocol-training` already uses for `TD-###`.

### Adopted — mechanism 3: the executable plan and the execution record

The phase document decomposes into `SI-NN.X` units, each with Description, Technical actions,
Tests, Dependencies and Acceptance criteria. Two details carry the weight:

- **Every technical action cites its origin** — `(per phase-03-videos/TD-06)` or
  `(convenção herdada de entidades)`. No orphan actions.
- **The Tests section is a table** (`Artifact | Layer | Test file`) written *before* the code,
  so the plan decides which test files exist rather than discovering them afterwards.

Phase-level specifications (Data Model, API Contracts, Authorization Matrix, Error Catalog,
Events), a Dependency Map with a linearised order, and a Deliverables checklist complete it.

`progress.md` is the cheapest and most valuable artifact in that repo. One entry per SI with
status, test counts and observations — and the observations hold what nothing else captures.
From phase 03: the full suite hanging for ~16 minutes because a module import chain left a
BullMQ connection leaked through a skipped `afterAll`; an enum that survived a migration
revert and broke test isolation. Neither is in a commit, a comment or a test. Both are the
same class of knowledge as this repo's `W1`–`W6` and its in-line Docker traps, except captured
during execution instead of in a retrospective.

`implement-phase` executes it with discipline this repo already shares: one SI at a time, only
that SI's tests during the loop, the full suite at the end, a fix loop capped at three attempts
before stopping to ask, never weaken a test, never leave scope. It adds a hard stop between
SIs, and states outright that violating that stop is its most common failure mode.

### Rejected — mechanism 4: the anti-drift machine

Every artifact carries `sources_mtime:` recording the mtime of each upstream file; any stage
aborts when a source is newer, never auto-regenerating, always naming the next command. The
intent is right. The price is the rest of the system:

- Planning one phase costs five commands (`context` → `validate` → `resolve` → `validate`
  again until `status: clean` → `build`), plus research before and test-specs after.
- `validation.md` carries nine typed issue categories with a `clean|dirty` gate. Phase 03's is
  thirty lines to say `_None._` seven times.
- `plan-pipeline` (356 lines) needs set arithmetic over grep to discover a phase's slug,
  because phases became slices, slices formed a DAG, and the DAG needed a maturity gate and a
  sibling-restamp ordering rule.
- Six subagents exist so the documents can be read without exhausting context.

None of that is careless — it is sediment from a harness that grew to fight its own
complexity, under a smaller context window and a larger document set. This repo is one
engineer, one machine, one product. Importing it would be importing the cure for a disease we
do not have.

**Trigger to revisit:** the first time a plan is executed against a decision that had been
revised after the plan was written, and nobody noticed. That is the failure `sources_mtime`
exists to prevent; until it happens, append-only decisions plus citation are enough.

## Proposed change

Three artifacts, two commands, no subagents.

### A. `docs/ROADMAP.md` — the capability spine

Milestones (`M1`, `M2`, …) with declared dependencies, literal capability bullets and
deliverables. `M1` is preferences → a generated training week. This is what stops the minimum
feature set from existing only in a conversation.

Capability bullets are quoted verbatim by milestone plans, following the mechanism above.

### B. `docs/decisions/ADR-###-<slug>.md` — the missing home

Shape borrowed directly: Context · Options with pros and cons · Recommendation · **Decision** ·
append-only `Revisions`. A superseded record is marked, never edited — the same rule
`/protocol-training` already applies to `TD-###`.

Two families, deliberately not merged:

| Family | Records | Gate |
|--------|---------|------|
| `ADR-###` | How the system is built | Options considered, alternative rejected in writing |
| `TD-###` | What the system asserts about training | Sourced knowledge note behind it (`/protocol-training`) |

Merging them would blunt root standard 15: a single `TD-012` could mean "we chose Postgres" or
"the range is 6–10 reps", and only the second is subject to the research gate.

### C. `docs/milestones/M<N>-<slug>/` — plan and record

- **`plan.md`** — SIs in the adopted shape (actions citing `ADR-##` / `TD-###`, tests table,
  dependencies, acceptance criteria), plus the specifications the milestone needs, a dependency
  map and a deliverables checklist.
- **`progress.md`** — one entry per SI: status, test result, observations.

### D. Two commands, not five

- **`/protocol-milestone <M<N>>`** — reads the roadmap milestone, lists the decisions the repo
  has not made, routes training judgements through `/protocol-training` and technical ones into
  `ADR` records, then emits `plan.md`. It collapses the analysed pipeline's context / validate /
  resolve / build into one pass but **keeps the gate**: `plan.md` is born with an
  `## Open questions` section, and execution does not start while it is non-empty. That is
  `status: clean|dirty` without four commands and nine issue types.
- **`/protocol-feature`, extended** — gains a mode for executing a milestone plan: the per-SI
  loop, `progress.md`, the hard stop between SIs, the three-attempt fix loop. Its existing
  verification ladder already is the final verification, and is stronger than the four-item
  Definition of Done in the analysed repo. A third skill is not created.

## Consequence for M1's sequencing

Recorded here because it changes the plan before the plan exists.

Almost every line of "suggest a week of five sessions, or three, using templates the science
supports, with defined rest" is a training judgement, and root standard 15 forbids each one
from reaching the code without a `TD-###` beside it. At minimum: template choice per weekly
frequency, weekly volume per muscle group, rep ranges per goal, rest intervals per range,
exercise selection, exercise ordering, week-to-week progression. The corpus is deliberately
empty.

Two consequences the milestone plan must honour:

1. **The critical path of `M1` is research, not code.** Research SIs are sequenced before
   generator SIs, or the generator is born with recalled numbers — exactly what `P5` predicted.
2. **The preferences schema is decided last.** A preference is an input to the generator; only
   once the generator's consumed variables are known is it knowable what to ask the user.
   Modelling `UserPreferences` first is the reliable route to a corrective migration, and root
   standard 10 makes migrations forward-only.

## Affected files

| File | Change |
|------|--------|
| `docs/ROADMAP.md` | new — the capability spine, starting with `M1` |
| `docs/decisions/` | new directory + the `ADR` template |
| `docs/milestones/` | new directory + the `plan.md` / `progress.md` templates |
| `.claude/skills/protocol-milestone/SKILL.md` | new |
| `.claude/skills/protocol-feature/SKILL.md` | extended with the milestone-execution mode |
| `CLAUDE.md` | "Where documentation lives" gains `docs/`; a standard for `ADR-###` citation alongside standard 15 |
| `.claude/harness/BACKLOG.md` | `P6` flipped to `in-progress`, linked here |

## Verification

This is harness, so the proof is a run, not a suite — the same standard `W6` set (prove the
claim that matters, not an adjacent one):

1. `M1` is written into `docs/ROADMAP.md` and every capability bullet is a literal string a
   plan can quote.
2. `/protocol-milestone M1` produces a `plan.md` whose `## Open questions` is non-empty on the
   first pass — an empty one on a milestone this under-decided would mean the gate is not
   looking.
3. Every technical action in the resulting plan cites an `ADR-##`, a `TD-###` or an existing
   convention. An orphan action is a failure of the skill, not of the plan.
4. Executing the first SI produces a `progress.md` entry with an observation that is not
   already in a commit message.

## Open questions — settled 2026-08-22

- **Does `ADR` need a `Scope` field?** Yes, as a frontmatter `binds:` list — not a prose section
  and not an index file. `grep -l 'binds:.*<tier>' docs/decisions/*.md` answers "what binds this
  tier" with no second surface to keep in sync. `/protocol-training` pays for an index because a
  knowledge note is long and expensive to open; an `ADR` is not.
- **Does the roadmap carry status?** No. It is a statement of intent. A planned milestone has a
  directory under `docs/milestones/`; the `progress.md` inside owns the status, one entry per
  step. Status in two places disagrees with itself, and the roadmap is the copy nobody updates.

## Shipped 2026-08-22 — the scaffolding

Everything below exists; nothing of `M1` is written yet, which is the next move and the
engineer's input.

- `docs/ROADMAP.md` — the spine, its two rules (a capability bullet is a literal key; no status
  lives here), and an `M1` section awaiting its capabilities.
- `.claude/skills/protocol-milestone/SKILL.md` — the six-step procedure, the two decision
  families, the `ADR` template with its gates, the `plan.md` shape with the four rules it is
  checked against, and the `## Open questions` gate.
- `.claude/skills/protocol-feature/SKILL.md` — a **Milestone mode** section: read the plan as a
  contract, refuse to start on a non-empty `## Open questions`, one step at a time in dependency
  order, only that step's tests during the loop, a three-attempt fix loop, a `progress.md` entry
  and a stop between steps, the ladder as final verification.
- `CLAUDE.md` — standard 16 (`ADR-###`, append-only, sibling of 15 and never merged with it),
  `docs/` added to the documentation homes and to the layout, and `/protocol-milestone`
  described in the Harness section.

`docs/decisions/` and `docs/milestones/` do not exist yet: a directory appears with its first
file, per the rule `/protocol-training` already follows.

Verification items 2–4 remain open — they cannot run before `M1` has capabilities.
