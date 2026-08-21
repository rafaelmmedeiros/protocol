"""Workout tools -- logged training sessions."""

from typing import Any

from mcp.server.mcpserver import MCPServer

from .. import client, config


def register(mcp: MCPServer) -> None:
    @mcp.tool()
    def hevy_workout_count() -> Any:
        """Get the total number of workouts logged on the account."""
        return client.get("/workouts/count")

    @mcp.tool()
    def hevy_list_workouts(page: int = 1, page_size: int = 10) -> Any:
        """List workouts, newest first. page_size is capped at 10 by the Hevy API."""
        return client.get("/workouts", client.page_params(page, page_size))

    @mcp.tool()
    def hevy_get_workout(workout_id: str) -> Any:
        """Get one workout's complete details (exercises, sets, weights, reps) by id."""
        return client.get(f"/workouts/{workout_id}")

    @mcp.tool()
    def hevy_recent_workouts(limit: int = 20) -> Any:
        """Get the most recent workouts in one call, paging over the API's 10-per-page cap.

        Use this instead of hevy_list_workouts when analysing a training block.
        limit is capped at 200.
        """
        limit = client.clamp(limit, 1, config.MAX_PAGE_SIZE * config.MAX_PAGES_PER_CALL)
        workouts: list[Any] = []
        for page in range(1, config.MAX_PAGES_PER_CALL + 1):
            batch = client.get("/workouts", client.page_params(page, config.MAX_PAGE_SIZE))
            items = batch.get("workouts", []) if isinstance(batch, dict) else []
            workouts.extend(items)
            if len(workouts) >= limit or len(items) < config.MAX_PAGE_SIZE:
                break
        return {"count": len(workouts[:limit]), "workouts": workouts[:limit]}

    @mcp.tool()
    def hevy_workout_events(
        since: str = "1970-01-01T00:00:00Z", page: int = 1, page_size: int = 10
    ) -> Any:
        """List workout update/delete events since an ISO 8601 timestamp, newest first.

        Useful for incremental syncing without refetching every workout.
        """
        return client.get(
            "/workouts/events", {"since": since, **client.page_params(page, page_size)}
        )
