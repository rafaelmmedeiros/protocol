---
name: protocol-training
description: "The strength-training domain for this repo: what the literature supports (knowledge notes) and what this product does about it (training decision records). Invoke before writing any code that makes a training judgement — a rep range, a progression step, a volume threshold, a readiness call — and to add to the corpus when a feature asks a question it does not yet answer."
argument-hint: "[<the training question, or a decision to record>]"
---

The domain knowledge home for `protocol`. Everything here exists because a feature needed it;
nothing is here because it is true.

Two kinds of file, kept apart on purpose:

- **`references/<topic>.md` — knowledge notes.** What the literature supports. Sourced, dated,
  bounded. Not ours: it changes when the science changes or when we read more of it.
- **`decisions/TD-###-<slug>.md` — training decision records.** What this product does. Ours,
  often a deliberate simplification of the notes it cites, because complexity has a usability
  cost.

Keeping them apart is what lets a future reader answer the only question that matters later:
*did we do it this way because the evidence says so, or because we chose to?* Mixed into one
file, every simplification reads as ignorance — and a later session will helpfully "fix" a
choice it mistakes for a gap.

The directories appear with their first file. An empty one would be a promise, not a corpus.

## Index

Keep this table current — it is the whole reason invoking this skill is cheap. A session reads
the table, then only the notes its question actually touches.

### Knowledge

| Topic | Confidence | Bearing — does this change what we would build, at our scale? |
|-------|-----------|---------------------------------------------------------------|
| _(nothing yet)_ | | |

### Decisions

| ID | Decision | Status | Rests on |
|----|----------|--------|----------|
| _(nothing yet)_ | | | |

## Consulting

1. Read the index. If a note covers the question, read it — starting with **what it does not
   settle**, which is where an overreaching claim gets caught.
2. If a decision already covers it, follow the decision, not the note. The note is the input;
   the decision is the ruling.
3. If neither exists, the question is unanswered: research it (below) before writing the code
   that assumes an answer.

Never fill a gap from memory. A number recalled and shipped is indistinguishable in the source
from a number that was researched — that is exactly the failure this corpus exists to prevent.

## Adding a knowledge note

The `training-researcher` agent does this work: it reads sources in its own context and returns
a draft, so the feature session gets the note rather than the reading. Review the draft before
it lands — what enters the corpus is a decision, not a fetch.

```markdown
---
topic: <slug>
confidence: settled | contested | thin
bearing: <one line — does this change anything we would build, at our scale?>
sources:
  - <citation, with a link that resolves>
last-reviewed: <YYYY-MM-DD>
---

**What is claimed.**

**What the evidence actually shows.** Effect size, population, and how far it generalises.

**What it does not settle.** The boundary of the claim.

**Where it touches the product.** Which decisions depend on it.
```

The gates, and none of them is optional:

- **Sources that resolve.** Recalled knowledge does not open a note. The literature and reviews
  of it, fetched.
- **An honest confidence tier.** `settled` is broad agreement across meta-analyses; `contested`
  is competent people disagreeing, and we will have to choose anyway; `thin` is mechanism or
  practitioner consensus with no direct evidence. `thin` is writable — unmarked `thin` is not.
- **A stated boundary.** A claim with no "what it does not settle" is unfinished.
- **An honest bearing.** A note whose bearing is "real, and too small to matter for us" is a
  useful note: it closes the question and stops it being reopened. Not all of the science
  applies, and saying so is the note's job.

## Recording a decision

```markdown
---
id: TD-###
title: <what was decided>
status: active | superseded-by TD-###
knowledge: [references/<topic>.md, ...]
decided: <YYYY-MM-DD>
---

**Decision.**

**Why this and not what the literature would suggest.** Named explicitly when they differ.

**What it costs.** The accuracy or generality given up for usability.

**How it shows up in code.** The rule, threshold or number, and where it lives.

**When to revisit.** The signal that would reopen it.
```

- **A decision may rest on `contested` or `thin` knowledge — and must say which.** Waiting for
  settled evidence on a contested question is not neutrality; it is shipping nothing.
- **Records are append-only. A decision changes by a new record superseding the old, never by
  editing it.** Root standard 7 forces this: programs this system generated were generated
  under the decision in force at the time, and the history recording them is append-only. A
  record edited in place makes every program produced under its earlier version unexplainable.
  Set the old record's `status` to `superseded-by`, and leave everything else in it untouched.
- **Every training judgement in code cites its record** — root standard 15. The comment is the
  link; there is no second index to keep in sync.

## Growth

One question at a time, and only a question a feature is actually asking. The trainer needs to
pick a scheme, so that question gets researched, one or two notes get written, one record
decides — and nothing else in exercise science enters that session.

The corpus is a sediment of features built. Its shape at any moment is a map of what the
product has actually had to reason about, and that is the correct shape for it to have.
