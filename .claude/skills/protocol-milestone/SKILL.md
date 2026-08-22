---
name: protocol-milestone
description: "Turn a milestone from docs/ROADMAP.md into an executable plan: list the decisions the repo has not made, route training judgements to /protocol-training and technical ones into ADR records, then emit the step plan with its tests, dependency order and acceptance criteria. Use before building a package of features; for one change inside an existing tier, use /protocol-feature instead."
argument-hint: "[M<N> | <the milestone to plan>]"
disable-model-invocation: true
---

The planning half of product work in `protocol`. `/protocol-feature` builds one change well;
this skill decides what the changes are, in what order, and against which decisions — for a set
of features large enough that improvising the order would cost a rewrite.

Three skills, three jobs, no overlap:

| Skill | Job |
|-------|-----|
| `/protocol-milestone` | Decide what gets built and in what order. Produces `plan.md`. |
| `/protocol-feature` | Build it. Consumes `plan.md`, writes `progress.md`. |
| `/protocol-harness` | Work on the tooling itself. |

Everything written stays in English (root standard 1). The conversation follows the engineer.

## Paths

- Roadmap: `docs/ROADMAP.md`
- Decision records: `docs/decisions/ADR-###-<slug>.md`
- Milestone: `docs/milestones/M<N>-<slug>/plan.md` and `progress.md`

Directories appear with their first file. An empty one is a promise, not a corpus.

## The two decision families

They are kept apart, and merging them would blunt root standard 15.

| Family | Records | Lives | Gate before it can be written |
|--------|---------|-------|-------------------------------|
| `ADR-###` | How the system is built — library, layout, protocol, schema shape | `docs/decisions/` | The options considered, and the rejected one, in writing |
| `TD-###` | What the system asserts about training — a rep range, a rest interval, a volume threshold | `/protocol-training` | A sourced knowledge note behind it |

If a single id could mean either "we chose Postgres" or "the range is 6–10 reps", then standard
15 no longer says anything, because only one of those is subject to a research gate.

## Procedure

### 1. Read the milestone

Read `docs/ROADMAP.md` and locate the milestone. If the section is empty or the milestone is
not there, stop and say so — the roadmap is the input, and a plan derived from nothing is a
plan of invented scope. Settle the capabilities with the engineer, write them there, then run
this skill again.

Copy the capability bullets into the plan **verbatim**. They are literal keys (see the roadmap
itself); a reworded bullet silently breaks the coverage check.

Read the root `CLAUDE.md` and the `CLAUDE.md` of every tier the milestone touches. Read
existing `ADR` records — `grep -l 'binds:.*<tier>' docs/decisions/*.md` finds the ones that
bind the tier you are about to touch. A decision already made is followed, not re-opened.

### 2. List the decisions that do not exist yet

Before any step is written, enumerate what the repo has not decided. Sort each one into a
family:

- **A training judgement** — anything that answers "how should someone train?". Route it to
  `/protocol-training`: follow the record if one exists, research and record one if not. Never
  decide one here, and never let one reach a step's technical actions without a `TD-###`.
- **A technical decision** — anything that answers "how do we build it?". It becomes an `ADR`
  (next step).
- **Already settled** — a standard in a `CLAUDE.md`, or an existing record. Cite it and move on.

Ask the engineer about the ones where different answers produce materially different work, with
a recommendation. Decide the rest and say what you decided — the same rule `/protocol-feature`
step 2 already applies, hoisted to the level of a set of features.

### 3. Write the ADR records

One file per decision, `docs/decisions/ADR-###-<slug>.md`, numbered sequentially and never
reused.

```markdown
---
id: ADR-###
title: <what was decided>
status: active | superseded-by ADR-###
binds: [backend, frontend, mcps, cross-cutting]
decided: <YYYY-MM-DD>
---

**Context.** What forced a choice. The constraint, not the wish.

**Options.**

### A — <name>
- What it is, concretely enough to be built from.
- **Pros:**
- **Cons:**

### B — <name>
- …

**Recommendation.** <letter> — and why.

**Decision.** <letter>

**Divergence.** _(only when Decision differs from Recommendation)_ Why the recommendation was
not taken, and what that costs.

**Revisions.** _(append-only; a parameter changing inside the same option)_
- <YYYY-MM-DD> — <what changed and why>
```

The gates:

- **`Recommendation` and `Decision` are separate fields, always.** The point of the record is
  the gap between them. A decision that matched its recommendation says so cheaply; one that
  did not is the only place the reasoning survives.
- **A rejected option is written before it is rejected.** An options list with one entry is not
  a decision, it is a note.
- **Records are append-only.** A parameter changing inside the same option is a `Revisions`
  bullet. A different option winning is a **new record**, with the old one's `status` set to
  `superseded-by` and nothing else in it touched. Root standard 7's reasoning applies here for
  the same reason it applies to training records: work was produced under the decision in force
  at the time, and editing the record in place makes that work unexplainable.
- **`binds:` is the index.** There is no index file. It is a frontmatter field precisely so
  that `grep` answers "what binds this tier" without a second surface to keep in sync.

### 4. Emit the plan

Write `docs/milestones/M<N>-<slug>/plan.md`. Its shape:

```markdown
# M<N> — <name>

## Objective

One paragraph: what the system can do when this is finished.

## Capabilities

Verbatim from `docs/ROADMAP.md`:

- <bullet>
- <bullet>

## Open questions

- <anything still undecided that a step below depends on>

_(Execution does not start while this section is non-empty.)_

## Steps

### S<N>.1 — <name>

**Description:** one or two sentences.

**Technical actions:**

1. <action> (per `ADR-04`)
2. <action> (per `TD-002`)
3. <action> (standard 6 — the training week starts on Monday)

**Tests:**

| Artifact | Layer | Test file |
|----------|-------|-----------|
| … | Unit / Integration / E2E | … |

**Depends on:** S<N>.x, or none

**Acceptance criteria:**

- <observable, not "it works">

## Specifications

Only the ones this milestone needs — a data model, an API contract, an error code table.
Nothing speculative.

## Dependency order

The steps as a graph, then the linearised order they will be executed in.

## Deliverables

- [ ] one line per step
- [ ] the verification ladder from `/protocol-feature`, green
- [ ] every capability bullet above covered by at least one step
```

Four rules the plan is checked against before it is finished:

- **No orphan action.** Every technical action cites an `ADR-###`, a `TD-###`, or a numbered
  standard from a `CLAUDE.md`. An action with no citation is an undeclared decision, which is
  the failure mode this whole skill exists to prevent.
- **The tests table is written before the code exists.** Deciding which test files exist is a
  planning act; discovering them afterwards is how a step ships untested.
- **Every capability bullet is covered by at least one step**, and the coverage is checked
  against the roadmap's literal text.
- **A judgement's research comes before anything that consumes it.** If a step needs a number
  that `/protocol-training` does not yet hold, the research is its own earlier step. A generator
  built before its corpus is a generator built from recalled numbers — indistinguishable later
  from a researched one, which is the whole of standard 15.

One consequence of that last rule worth stating because it is counter-intuitive: **a
preferences or configuration schema is decided late, not first.** A preference is an input to
whatever consumes it; only once the consumer's variables are known is it knowable what to ask.
Modelling it first is the reliable route to a corrective migration, and standard 10 makes
migrations forward-only.

### 5. The gate

`## Open questions` must be empty before implementation starts. That is the whole gate — one
section, checked by reading it.

It is deliberately not a validation pipeline with typed issue categories and a machine-checked
`clean`/`dirty` verdict. That design was examined (see `.claude/harness/MilestoneWorkflow/DESIGN.md`)
and rejected as sediment from a much larger document set: what it buys over reading one section
does not pay for five commands per milestone.

The first pass of a genuinely new milestone should produce a **non-empty** section. An empty one
on under-decided work means the listing in step 2 was not looking.

### 6. Hand off

Execution is `/protocol-feature`, milestone mode. It reads `plan.md` as a contract, works one
step at a time, and writes `progress.md`. This skill makes no code changes.

## Proportional effort

- **A change inside an existing tier** → not this skill. `/protocol-feature`.
- **A capability that needs one decision and one step** → an `ADR` (or a `TD`) and a
  conversation. A milestone directory for a single step is ceremony.
- **A set of features with an order, dependencies and undecided ground** → all six steps above.

If this skill ever feels heavy for the work in front of it, that is a harness pain — log it
through `/protocol-harness` rather than working around it quietly.
