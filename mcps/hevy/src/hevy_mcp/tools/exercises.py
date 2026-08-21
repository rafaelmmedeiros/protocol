"""Exercise tools -- the movement catalogue and per-exercise history."""

from typing import Any, Optional

from mcp.server.mcpserver import MCPServer

from .. import client, config


def register(mcp: MCPServer) -> None:
    @mcp.tool()
    def hevy_list_exercise_templates(
        page: int = 1, page_size: int = 100, search: Optional[str] = None
    ) -> Any:
        """List exercise templates (page_size capped at 100).

        `search` filters the returned page by title substring, case-insensitive - the
        Hevy API has no search parameter, so this narrows results client-side.
        """
        result = client.get(
            "/exercise_templates",
            client.page_params(page, page_size, config.MAX_PAGE_SIZE_TEMPLATES),
        )
        if search and isinstance(result, dict):
            needle = search.lower()
            templates = [
                t
                for t in result.get("exercise_templates", [])
                if needle in str(t.get("title", "")).lower()
            ]
            return {**result, "exercise_templates": templates}
        return result

    @mcp.tool()
    def hevy_get_exercise_template(exercise_template_id: str) -> Any:
        """Get one exercise template by id."""
        return client.get(f"/exercise_templates/{exercise_template_id}")

    @mcp.tool()
    def hevy_exercise_history(
        exercise_template_id: str,
        start_date: Optional[str] = None,
        end_date: Optional[str] = None,
    ) -> Any:
        """Get the logged history for one exercise, optionally between two ISO 8601 dates.

        This is the tool for progression questions ("how has my squat moved?").
        """
        return client.get(
            f"/exercise_history/{exercise_template_id}",
            {"start_date": start_date, "end_date": end_date},
        )
