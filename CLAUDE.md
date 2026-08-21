# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Status

Early. There is no build, lint, or test tooling yet — do not invent commands that don't exist.
The overall architecture still lives in the engineer's head; follow their direction rather than
assuming a structure.

## Rules

1. All tooling and code — source, identifiers, comments, docs, commit messages, CLI output — is written
   in English. Conversation with the engineer may be in Portuguese or English.

## Layout

```
mcps/<name>/          one MCP server per directory; more are coming
  src/<pkg>/          src-layout package, installed by uv/pip (never a loose script)
    config.py         API constants, .env loading, credential lookup
    client.py         the only network access for that server
    server.py         builds the MCPServer instance, registers the tool modules
    tools/            one module per resource group, each exposing register(mcp)
  pyproject.toml      declares a console script named <name>-mcp
  Dockerfile / docker-compose.yml   builds mcp-<name>:latest
  .env / .env.example credentials, .env gitignored at the repo root
.mcp.json             registers every server for this project
```

Conventions that should hold for each new server: package under `src/`, network access
confined to one module, tools grouped by resource and registered through `register(mcp)`,
read and write split into separate servers (`hevy` / `hevy-write`) so a read-only server
stays read-only by construction. `C:/Users/rafae/Projects/MCPs` holds unrelated servers on
an older, flatter version of this shape -- reference only, not a destination for code.

## Running an MCP server

Docker is the default: `.mcp.json` spawns each server with `docker run -i --rm`, so Claude Code
starts the container itself at session start. Nothing runs between sessions.

```
docker compose build            # build every MCP image (project: protocol-mcps)
docker compose build hevy       # rebuild one after changing its source
uv run --directory D:/projects/protocol/mcps/hevy hevy-mcp    # dev loop, skips the rebuild
```

The root `docker-compose.yml` exists to BUILD images, not to run them -- an stdio server needs
its stdin wired to one client, so `docker compose up` would start a container talking to nobody.

Grouping: compose project `protocol-mcps`, images `protocol-mcps/<name>:latest`. `.mcp.json`
passes `--label com.docker.compose.project=protocol-mcps` so the ad-hoc containers land in the
same Docker Desktop group as the build.

Dependencies come from `uv.lock` in both paths: the image installs with `uv sync --locked`,
so a container and the `uv run` dev loop resolve to the same versions. Add a dependency with
`uv add`, never by hand-editing a pin, and commit the updated lock.

**Docker caches the source at build time**: after editing a server, `docker compose build <name>`
or the restarted session keeps serving the old code. Changes to `.mcp.json` also only take effect
on restart.

## mcp-hevy invariants

- Read-only: every tool goes through `client.get`, the module's single network verb. Hevy's
  write endpoints (POST/PUT on workouts, routines, body_measurements) belong in a separate
  `mcps/hevy-write` server, never in this one.
- SDK note: `mcp` 2.x renamed `FastMCP` to `MCPServer` (`mcp.server.mcpserver`) and returns
  snake_case fields (`server_info`, `input_schema`, `is_error`). The servers under
  `C:/Users/rafae/Projects/MCPs` still import `mcp.server.fastmcp` because their Docker images
  pinned 1.x -- do not copy that import into new code.
- Hevy caps `pageSize` at 10 on every list endpoint except `exercise_templates` (100), and has
  no server-side search -- hence the paging helper `hevy_recent_workouts` and the client-side
  `search` filter on `hevy_list_exercise_templates`.

## Direction

Skills, agents, and further tooling are planned for later. Update this file as those land.
