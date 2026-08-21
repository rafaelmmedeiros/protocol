# mcps

MCP servers, one per directory. The repo-wide standards in the root `CLAUDE.md` apply here too
— this file only covers what is specific to this tier.

## These are harness, not runtime

An MCP server here exists so data can be explored from a session while designing a feature. It
is never part of the product's request path: the system reaches Hevy through its own outbound
client in `backend/`. If you find yourself wiring an MCP server into the application, stop —
that is the wrong tier.

The corollary is that these servers can stay small and blunt. They answer questions during
development; they do not carry production concerns.

## Layout

```
mcps/<name>/
  src/<pkg>/          src-layout package, installed by uv/pip (never a loose script)
    config.py         API constants, .env loading, credential lookup
    client.py         the only network access for that server
    server.py         builds the MCPServer instance, registers the tool modules
    tools/            one module per resource group, each exposing register(mcp)
  pyproject.toml      declares a console script named <name>-mcp
  Dockerfile          builds protocol-mcps/<name>:latest
  .env / .env.example credentials; .env is gitignored at the repo root
```

Conventions for each new server: package under `src/`, network access confined to one module,
tools grouped by resource and registered through `register(mcp)`, and read and write split into
separate servers (`hevy` / `hevy-write`) so a read-only server stays read-only by construction.

`C:/Users/rafae/Projects/MCPs` holds unrelated servers on an older, flatter version of this
shape — reference only, not a destination for code.

## Running and building

Docker is the default: `.mcp.json` spawns each server with `docker run -i --rm`, so Claude Code
starts the container itself at session start. Nothing runs between sessions.

```
docker compose build            build every MCP image (compose project protocol-mcps)
docker compose build hevy       rebuild one after changing its source
uv run --directory D:/projects/protocol/mcps/hevy hevy-mcp    dev loop, skips the rebuild
```

The root `docker-compose.yml` exists to BUILD images, not to run them: an stdio server needs
its stdin wired to one client, so `docker compose up` would start a container talking to
nobody. `.mcp.json` passes `--label com.docker.compose.project=protocol-mcps` so the ad-hoc
containers land in the same Docker Desktop group as the build.

Dependencies come from `uv.lock` in both paths — the image installs with `uv sync --locked`, so
a container and the `uv run` dev loop resolve to the same versions. Add one with `uv add`,
never by hand-editing a pin, and commit the updated lock.

**Docker caches the source at build time**: after editing a server, `docker compose build
<name>` or the restarted session keeps serving the old code. Changes to `.mcp.json` also only
take effect on restart.

## hevy invariants

- **Read-only**: every tool goes through `client.get`, the module's single network verb. Hevy's
  write endpoints (POST/PUT on workouts, routines, body_measurements) belong in a separate
  `mcps/hevy-write` server, never in this one.
- **SDK note**: `mcp` 2.x renamed `FastMCP` to `MCPServer` (`mcp.server.mcpserver`) and returns
  snake_case fields (`server_info`, `input_schema`, `is_error`). The servers under
  `C:/Users/rafae/Projects/MCPs` still import `mcp.server.fastmcp` because their Docker images
  pinned 1.x — do not copy that import into new code.
- **Paging**: Hevy caps `pageSize` at 10 on every list endpoint except `exercise_templates`
  (100), and has no server-side search — hence the paging helper `hevy_recent_workouts` and the
  client-side `search` filter on `hevy_list_exercise_templates`.
- **Shapes worth knowing**: sets carry `weight_kg`, `distance_meters`, `duration_seconds` and a
  nullable `rpe`; timestamps are UTC ISO 8601; `type` distinguishes `warmup` from `normal`, so
  working volume is not simply every set. These are the same canonical units the root standards
  require, which is why they were adopted.

## Testing

There is none yet, and no command should be invented for it. When these servers get tests,
write the convention here.
