# Training knowledge: where the domain lives and how it grows

Status: implemented. Backlog row `P5`.

## Problem

The product's whole reason to exist is judgement about strength training. Hevy logs; this
system reasons. But the repo has nowhere to put the reasoning's substrate. Root `CLAUDE.md`
already commits to a shape — "this is domain knowledge and belongs in a skill, so that it
enters a session only when a feature actually consults it. It gets written when that feature
exists" — and that sentence answers *when* and *where it loads*, and nothing else. It does not
say what a unit of that knowledge looks like, what admits a claim into it, who challenges a
claim, or how a product decision that deliberately ignores the literature gets recorded as
deliberate rather than as ignorance.

## Evidence

This is the first pain here recorded without a lived case, and the gate deserves to be named
rather than quietly stepped over. What stands in for the case is a documented commitment with
no mechanism behind it: the sentence above exists, the first trainer feature is next, and on
the day it is built there is no answer to "where does the set and rep scheme come from, and
what justifies it". The cost profile is the one every other standard in `CLAUDE.md` was adopted
for — cheap now, expensive to retrofit. Sourcing a claim while researching it costs minutes;
reconstructing the source for a number already baked into a generator, months later, means
re-deriving every program that number produced. That is the same argument standard 7 makes
about training history, applied to the reasoning that reads it.

The engineer's framing, which the design has to satisfy:

- Nothing ships without a scientific basis.
- But not everything in the science applies — effect sizes exist that are real and irrelevant
  at this product's scale.
- The engineer trains and understands the domain, but is not an expert. Their contribution is
  usability and complexity judgement, and they need a counterweight that knows the literature.
- Growth is incremental. A simple trainer first, evolving. The corpus must not be dumped in one
  pass; it must arrive one question at a time.
- What is learned becomes part of the harness, not part of a session.

## The distinction that drives everything

Two things are being conflated by the phrase "domain knowledge", and keeping them in one place
is what would rot:

1. **What the literature supports.** Contested, dated, effect-sized, source-bearing. Not ours.
   It changes when the science changes, or when we read more of it.
2. **What this product does.** A decision, ours, often a deliberate simplification of (1)
   because complexity has a usability cost. It changes when we decide differently.

A file that mixes them cannot answer the question that actually matters later: *did we do it
this way because that is what the evidence says, or because we chose to?* Every simplification
looks like ignorance to a future reader unless the record says otherwise — and a future session
will helpfully "fix" a simplification it mistakes for a gap.

So: two shapes, two directories, and decisions cite knowledge by link.

## Proposal

### Where it lives

```
.claude/skills/protocol-training/
  SKILL.md                     the index and the procedure: how to consult, how to add
  references/<topic>.md        knowledge notes — what the literature supports
  decisions/TD-###-<slug>.md   training decisions — what this product does
```

A skill, per the existing ruling: it must not enter a session that is not doing training work.
`SKILL.md` stays an index — a table of what exists and a one-line bearing for each — so
invoking it is cheap and only the notes a question actually touches get read. That is the
progressive disclosure the corpus needs in order to keep growing without every training feature
paying for the whole of it.

### A knowledge note

```markdown
---
topic: set-volume
confidence: settled | contested | thin
bearing: <one line — does this change anything we would build, at our scale?>
sources:
  - <citation, with a link that resolves>
last-reviewed: 2026-08-22
---

**What is claimed.**
**What the evidence actually shows.** Effect size, population, and how far it generalises.
**What it does not settle.** The boundary of the claim — read first by anyone building on it.
**Where it touches the product.** Which decisions depend on it.
```

`confidence` is the honest tier, not a hedge: `settled` means broad agreement across
meta-analyses; `contested` means competent people disagree and we will have to choose anyway;
`thin` means mechanism or practitioner consensus with no direct evidence — writable, but a
decision resting only on `thin` notes must say so.

`bearing` is where "not all of the science is applicable" gets teeth. A note whose honest
bearing is "real, and too small to matter for a first trainer" is a *useful* note — it closes
the question and stops it being reopened.

### A training decision record

```markdown
---
id: TD-001
title: <what was decided>
status: active | superseded-by TD-0NN
knowledge: [references/set-volume.md, ...]
decided: 2026-08-22
---

**Decision.**
**Why this and not what the literature would suggest.** Named explicitly when they differ.
**What it costs.** The accuracy or generality given up for usability.
**How it shows up in code.** The rule, threshold or number, and where it lives.
**When to revisit.** The signal that would reopen it.
```

**Decision records are append-only; a decision changes by a new record superseding the old,
never by editing it.** This is not tidiness borrowed from the migration rule — it is forced by
standard 7. Programs this system generated were generated under the decision in force at the
time, and the training history that records them is itself append-only. A record edited in
place makes every program produced under its earlier version unexplainable.

### What admits a claim

The harness already learned this lesson in the technical domain and wrote it down as win W2:
the framework's own shipped docs beat recalled knowledge, and the design that came from reading
them was different from the plausible one that came from memory. The training domain is the
same failure mode with worse consequences, because a wrong training claim produces a program
that a person runs.

So the gate mirrors the backlog's evidence gate for pains:

- **A knowledge note requires sources that resolve.** Recalled knowledge does not open a note.
  Research means the literature and reviews of it, fetched, not remembered.
- **A note states what it does not settle.** A claim with no stated boundary is not finished.
- **A decision may rest on `contested` or `thin` knowledge — and must say which.**
- **No training judgement in code without a `TD` citation.** Any threshold, ratio, progression
  step or rep range in the backend carries the record id in a comment. This is the rule that
  would graduate to `CLAUDE.md`, and it is also the verification below.

### How it grows

One question at a time, and only a question a feature is actually asking. The trigger is the
same as everything else here: the trainer needs to pick a scheme, so that question gets
researched, one or two notes get written, one record decides. Nothing else in exercise science
enters that session. The corpus is a sediment of features built, and its shape at any moment is
a map of what the product has actually had to reason about.

### The critique and research capability

The engineer asked for three things: knowledge, decisions, and critique. The first two are the
files above. The third is an agent, and it is the one role a skill cannot fill — a skill runs
inside the session that already decided what it wants to hear.

Two jobs, and they arrive at different times because one of them is unblocked and the other is
not:

- **Researcher — now.** Given a question, reads sources on the web, returns a draft note in the
  shape above with citations, confidence and boundary. It runs in its own context, so a feature
  session gets the note rather than forty pages of reading. This is unblocked and useful on the
  first trainer feature.
- **Critic — after the first trainer feature.** Reads a proposed decision or a built feature
  against the corpus and argues the other side: the claim overreaches, the note it rests on is
  `thin`, the simplification costs more than recorded. Deliberately deferred, because a critic
  with an empty corpus critiques from model memory — precisely what the gate above forbids. Its
  trigger is the first time a training decision is made and the engineer cannot tell whether it
  is right.

## Alternatives considered

- **A single `domain/` directory at the repo root.** Rejected: nothing loads it. `CLAUDE.md`
  would have to carry a pointer, and the corpus grows exactly like the tier detail that pain
  P3 already had to cut out of the root file.
- **Decisions as a separate top-level home from the notes.** Rejected: the link between what
  the science says and what we did is the whole value, and split homes let it go stale.
- **Everything in `CLAUDE.md`.** Rejected by the existing ruling and by P3: this is consulted
  knowledge, not a convention that must be remembered. Only the citation rule is a convention,
  and only that graduates.
- **Building the corpus up front from a periodization curriculum.** Rejected: it is the thing
  the engineer explicitly ruled out, and it would fill the repo with knowledge no feature has
  asked for, dated from the day it was written.

## Affected files

| File | Change |
|------|--------|
| `.claude/skills/protocol-training/SKILL.md` | New — index and procedure |
| `.claude/skills/protocol-training/references/` | New — empty until the first question |
| `.claude/skills/protocol-training/decisions/` | New — empty until the first decision |
| `.claude/agents/training-researcher.md` | New — the researcher |
| `CLAUDE.md` | The citation rule graduates in; the "belongs in a skill" sentence points at the skill |
| `.claude/skills/protocol-feature/SKILL.md` | Step 1 gains: if the change makes a training judgement, consult `/protocol-training` first |

## Verification

There is no suite for a knowledge base, so the proof is a property of the first trainer feature
rather than a green run:

1. Every training number in the feature's code is greppable to a `TD` id, and every `TD` id
   resolves to a record.
2. Every record's `knowledge` links resolve to notes, and every note's sources resolve.
3. The feature was built by consulting the corpus, not by recalling — checkable by asking
   whether any claim in it appears in no note.

Failing (1) is the interesting one: a magic number with no citation is the exact failure this
design exists to prevent, and it is cheap to check.

## Outcome

Shipped as proposed, both forks confirmed with the engineer: the researcher agent now and the
critic deferred (backlog `I3`), and the decision records living beside the notes under the
skill. `.claude/skills/protocol-training/SKILL.md` carries the index, both templates and both
gates; `.claude/agents/training-researcher.md` is read-only on purpose — it drafts, and what
enters the corpus stays a decision rather than a fetch. Standard 15 graduated into
`CLAUDE.md`, and step 1 of `/protocol-feature` now routes a training judgement through the
skill before any of it is written.

`references/` and `decisions/` do not exist yet, and that is the design working: an empty
directory would be a promise, and the first note is written by the first feature that needs
one. Verification is therefore still pending by construction — it is a property of that
feature, and the check that matters is (1), a training number in the source with no `TD` beside
it.
