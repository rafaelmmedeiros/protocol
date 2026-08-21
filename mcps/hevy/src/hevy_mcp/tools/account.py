"""Account-level tools."""

from typing import Any

from mcp.server.mcpserver import MCPServer

from .. import client


def register(mcp: MCPServer) -> None:
    @mcp.tool()
    def hevy_user_info() -> Any:
        """Get the authenticated Hevy user's account info."""
        return client.get("/user/info")
