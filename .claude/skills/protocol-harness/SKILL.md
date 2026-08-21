---
name: protocol-harness
description: "Evolution Mode for this repo's own harness: capture pains, wins and ideas about how we work here into .claude/harness/BACKLOG.md, discuss them, and fix them at proportional cost. The harness grows only where real work hurt — nothing is built ahead of the pain."
argument-hint: "[pain|win|idea \"text\" | review | <topic>]"
disable-model-invocation: true
---
Evolution Mode for the `protocol` harness. This skill is the explicit trigger for working *on
the tooling* — `CLAUDE.md`, `.claude/` skills and settings, `.mcp.json`, the build and dev loops —
rather than on the product itself. Announce that we are entering **Evolution Mode** so it is
clear the work is meta.

"The product" is today the MCP servers under `mcps/`, and a backend and a frontend are coming.
The harness covers all of them: a pain from any part of the codebase belongs in the same
backlog. Nothing is pre-built for the parts that do not exist yet — when they land, they bring
their own pains and the harness grows there.

Language: everything written stays in English (root `CLAUDE.md`, rule 1) — this file, the
backlog, design docs, commit messages. The conversation follows the engineer's lead.

## Why this exists

This repo starts with no skills and no workflow, and that is deliberate. Structure is added
only after a concrete case where its absence cost something — the same way the architecture
here grows one server at a time instead of being scaffolded up front. Capturing the pain is
what makes the fix proportional; without a recorded case we would be guessing at problems.

## Source of truth

- **The observed pain is the source of truth.** A pain enters the backlog only if it cites a
  real case from actual work in this repo. "Would be nice" is an idea, not a pain.
- **`.claude/harness/BACKLOG.md`** is the source of truth for what is open / in-progress /
  done / parked. Read it from disk, never from memory.
- **Settled rules graduate out.** Once a fix hardens into a standing rule it moves to
  `CLAUDE.md`; a discrete reusable fact goes to memory. The backlog is deliberation and
  tracking — not the permanent home of rules.

## Proportional effort

- **Small** (a wording fix in `CLAUDE.md`, one line in `.mcp.json`, a single command in the
  dev loop) → do it now, flip the row to `done` with a one-line outcome. No ceremony.
- **Substantial** (changes a convention, touches every server or a whole tier, needs a new
  skill or a schema)
  → write `.claude/harness/<Name>/DESIGN.md` (problem, evidence, proposed change, affected
  files, verification), discuss it, then implement.
- The skill itself is subject to this rule. If it ever feels heavy, that is a pain to log here.

## Input

The argument is: $ARGUMENTS

- **No argument** → Phase 1 (overview + menu).
- **`pain "<text>"` / `win "<text>"` / `idea "<text>"`** → Phase 2 (capture).
- **`review`** → Phase 3 (review and pick).
- **Anything else** → treat it as a `<topic>`: match it against existing rows first, then
  Phase 4 (work an item).

## Paths

- Backlog: `D:\projects\protocol\.claude\harness\BACKLOG.md`
- Design docs: `D:\projects\protocol\.claude\harness\<Name>\DESIGN.md`

---

## Phase 1: Overview + menu

1. Announce Evolution Mode.
2. Read the backlog. Render a compact summary: pains by status, count of wins and ideas, and
   the open / in-progress pains listed out — those are the actionable ones.
3. Offer: capture something · review and pick · work a specific item.

## Phase 2: Capture

1. Read the backlog.
2. **For a pain, apply the evidence gate.** The text must describe an observed case ("when I
   rebuilt the hevy image after editing client.py, the container still served the old code").
   If it reads as a wish with no case behind it, say so and offer to record it as an `idea`
   instead. Never write a pain row without evidence.
3. Append a row to the matching table with the next ID (`P{n}` / `W{n}` / `I{n}`), status
   `open`, Fix column `—`.
4. Confirm in one line what was recorded and where.

## Phase 3: Review

1. Read the backlog. Rank the open / in-progress pains by how often and how badly they bite,
   using the evidence text as the signal. Present them numbered.
2. Recommend the highest-leverage one, but let the engineer pick. Flag any better left
   `parked` (e.g. it only pays off once the backend exists).
3. On a pick, continue into Phase 4.

## Phase 4: Work an item

1. Read the backlog, locate the row by ID or topic match. No match → offer Phase 2 first.
2. Judge the size with the engineer (see Proportional effort) and take that path.
3. Substantial items: flip the row to `in-progress` and link the design doc; on completion set
   `done` and record the outcome in the design doc.
4. Then ask whether anything hardened into a standing rule → propose promoting it to
   `CLAUDE.md`, or saving a memory if it is a discrete fact.
5. Harness changes are committed like any other change here, in English.

## Not here yet — on purpose

These exist in the older AMLabs harness and were deliberately left out until this repo produces
a case for them. Adding one is itself a backlog item:

- **Prune mode** (policing rules that stopped earning their keep) — `CLAUDE.md` is still small
  enough to read whole; revisit once it has accumulated procedure rather than facts.
- **A product pipeline** (discovery → implement → review). There is no such flow here yet, and
  none should be invented before the work demands it — including for the backend and frontend
  still to come.
- **Tier-specific conventions** (backend layout, frontend conventions, a test gate). They are
  written when that tier exists and its first pain shows up, not in anticipation.
- **Per-improvement design docs as the default.** They are the exception path, not the norm.
