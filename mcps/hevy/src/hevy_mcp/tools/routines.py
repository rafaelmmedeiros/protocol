"""Routine tools -- saved workout plans and their folders."""

from typing import Any

from mcp.server.mcpserver import MCPServer

from .. import client


def register(mcp: MCPServer) -> None:
    @mcp.tool()
    def hevy_list_routines(page: int = 1, page_size: int = 10) -> Any:
        """List saved routines (workout plans). page_size is capped at 10."""
        return client.get("/routines", client.page_params(page, page_size))

    @mcp.tool()
    def hevy_get_routine(routine_id: str) -> Any:
        """Get one routine's full definition by id."""
        return client.get(f"/routines/{routine_id}")

    @mcp.tool()
    def hevy_list_routine_folders(page: int = 1, page_size: int = 10) -> Any:
        """List routine folders. page_size is capped at 10."""
        return client.get("/routine_folders", client.page_params(page, page_size))

    @mcp.tool()
    def hevy_get_routine_folder(folder_id: str) -> Any:
        """Get one routine folder by id."""
        return client.get(f"/routine_folders/{folder_id}")
