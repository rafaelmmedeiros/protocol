"""MCP server instance and tool registration.

Tools live in `hevy_mcp.tools.*`, one module per Hevy resource group. Each module
exposes `register(mcp)`; this module owns the server object and wires them together.
"""

from mcp.server.mcpserver import MCPServer

from . import config
from . import __version__
from .tools import account, body, exercises, routines, workouts

REGISTRARS = (account, workouts, routines, exercises, body)


def build_server() -> MCPServer:
    config.load_env_file()
    mcp = MCPServer("hevy-readonly", version=__version__)
    for module in REGISTRARS:
        module.register(mcp)
    return mcp


def main() -> None:
    build_server().run()
