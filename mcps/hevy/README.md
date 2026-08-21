# hevy-mcp

Read-only MCP server for [Hevy](https://hevyapp.com), the workout logging app.
Every tool goes through `client.get`, the module's only network verb, so the server
cannot modify the account.

## Setup

1. Get an API key in the Hevy app: **Settings -> Developer** (requires Hevy Pro).
2. `cp .env.example .env` and fill in `HEVY_API_KEY`.

## Run

Registered in `D:/projects/protocol/.mcp.json` as `hevy`, which Claude Code starts as a
container. Build it from the repo root (compose project `protocol-mcps`):

```
docker compose build hevy
```

Rebuild after every source change -- the image carries a copy of `src/`. For a tight dev loop,
run it straight from source instead:

```
uv run --directory D:/projects/protocol/mcps/hevy hevy-mcp
```

## Layout

```
src/hevy_mcp/
  config.py     API constants, .env loading, credential lookup
  client.py     the only network access -- a single GET verb + paging helpers
  server.py     builds the MCPServer instance and registers every tool module
  tools/        one module per Hevy resource group, each exposing register(mcp)
```

Adding a tool means editing (or adding) a module under `tools/` and, for a new module,
listing it in `server.REGISTRARS`.

## Tools

| Tool | Endpoint |
| --- | --- |
| `hevy_user_info` | `GET /user/info` |
| `hevy_workout_count` | `GET /workouts/count` |
| `hevy_list_workouts` | `GET /workouts` |
| `hevy_get_workout` | `GET /workouts/{id}` |
| `hevy_recent_workouts` | `GET /workouts` (pages over the 10-per-page cap) |
| `hevy_workout_events` | `GET /workouts/events` |
| `hevy_list_routines` / `hevy_get_routine` | `GET /routines[/{id}]` |
| `hevy_list_routine_folders` / `hevy_get_routine_folder` | `GET /routine_folders[/{id}]` |
| `hevy_list_exercise_templates` / `hevy_get_exercise_template` | `GET /exercise_templates[/{id}]` |
| `hevy_exercise_history` | `GET /exercise_history/{templateId}` |
| `hevy_list_body_measurements` / `hevy_get_body_measurement` | `GET /body_measurements[/{date}]` |

The write endpoints Hevy exposes (`POST /workouts`, `PUT /routines/{id}`, `POST /body_measurements`, ...)
are deliberately absent; they belong in a separate `mcps/hevy-write` server.

## API notes

- Base URL `https://api.hevyapp.com/v1`, auth via the `api-key` request header.
- List endpoints cap `pageSize` at 10, except `exercise_templates` (100).
- `exercise_templates` has no server-side search, so `hevy_list_exercise_templates(search=...)`
  filters the fetched page by title client-side.
