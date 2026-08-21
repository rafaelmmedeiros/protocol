# Harness Backlog — Evolution Mode

The living registry for evolving this repo's harness: `CLAUDE.md`, the `.claude/` skills and
settings, `.mcp.json`, and the build / dev loops. Managed through the `/protocol-harness` skill.

Scope is the whole repo, not one tier: today the MCP servers under `mcps/`, and the backend and
frontend still to come. A pain from any of them goes in the same tables here.

**Rules of this backlog:**

- **Pains require evidence.** Every pain cites a real observed case from work in this repo —
  never "would be nice to have". Without a case, it is an idea.
- **Wins are tracked too.** Knowing what already works prevents regressing it when the harness
  is refactored.
- **Proportional effort.** Small fixes resolve inline and just flip the row to `done`.
  Substantial ones get `.claude/harness/<Name>/DESIGN.md`.
- **Settled rules graduate.** A rule that hardens moves to `CLAUDE.md`; a discrete reusable
  fact goes to memory. This file is deliberation and tracking, not the home of settled rules.

Status values: `open` · `in-progress` · `done` · `parked`.

## Pains

| ID | Evidence (real observed case) | Status | Fix |
|----|-------------------------------|--------|-----|
| —  | _nothing recorded yet_        | —      | —   |

## Wins

| ID | What worked | Keep because |
|----|-------------|--------------|
| —  | _nothing recorded yet_ | — |

## Ideas

| ID | Idea | Status | Note |
|----|------|--------|------|
| I1 | Add a prune mode that reviews standing rules and drops the ones that stopped earning their keep. | parked | Deliberately deferred: `CLAUDE.md` is still small enough to read whole. Revisit when it accumulates procedure rather than facts. |
