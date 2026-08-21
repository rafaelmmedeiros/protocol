"""Body measurement tools -- weight, body fat and the rest of the log."""

from typing import Any

from mcp.server.mcpserver import MCPServer

from .. import client


def register(mcp: MCPServer) -> None:
    @mcp.tool()
    def hevy_list_body_measurements(page: int = 1, page_size: int = 10) -> Any:
        """List body measurements (weight, body fat, etc). page_size is capped at 10."""
        return client.get("/body_measurements", client.page_params(page, page_size))

    @mcp.tool()
    def hevy_get_body_measurement(date: str) -> Any:
        """Get the body measurement recorded on a date (YYYY-MM-DD)."""
        return client.get(f"/body_measurements/{date}")
