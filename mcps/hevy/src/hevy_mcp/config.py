"""Configuration and credential loading for the Hevy MCP server."""

import os
from pathlib import Path

BASE_URL = "https://api.hevyapp.com/v1"

REQUEST_TIMEOUT_SECONDS = 30.0

# Hevy caps pageSize at 10 on every list endpoint except exercise_templates (100).
MAX_PAGE_SIZE = 10
MAX_PAGE_SIZE_TEMPLATES = 100

# Upper bound for the paging helpers, so a bad argument cannot hammer the API.
MAX_PAGES_PER_CALL = 20

# Package root is src/hevy_mcp/, so the project directory is two levels up.
PROJECT_DIR = Path(__file__).resolve().parents[2]


def load_env_file() -> None:
    """Load the project's .env when running outside Docker (`uv run`).

    In Docker the credentials arrive via --env-file, so already-set vars win.
    """
    env_path = PROJECT_DIR / ".env"
    if not env_path.exists():
        return
    for line in env_path.read_text(encoding="utf-8").splitlines():
        line = line.strip()
        if not line or line.startswith("#") or "=" not in line:
            continue
        key, value = line.split("=", 1)
        os.environ.setdefault(key.strip(), value.strip().strip('"').strip("'"))


def api_key() -> str:
    """Return the Hevy API key, raising a fixable error when it is missing."""
    key = os.environ.get("HEVY_API_KEY", "").strip()
    if not key:
        raise RuntimeError(
            "HEVY_API_KEY is not set. Copy .env.example to .env and fill in the key "
            "from the Hevy app: Settings -> Developer (requires Hevy Pro)."
        )
    return key
