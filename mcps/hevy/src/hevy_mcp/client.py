"""HTTP access to the Hevy API.

This module is the server's only door to the network, and it exposes a single verb:
`get`. Keeping it that way is what makes the whole server read-only by construction --
no tool can mutate the account even by mistake.
"""

from typing import Any, Optional

import httpx

from . import config


def get(path: str, params: Optional[dict[str, Any]] = None) -> Any:
    """Issue a read-only request against the Hevy API and return the parsed body."""
    clean = {k: v for k, v in (params or {}).items() if v is not None}
    with httpx.Client(timeout=config.REQUEST_TIMEOUT_SECONDS) as client:
        response = client.get(
            f"{config.BASE_URL}{path}",
            params=clean,
            headers={"api-key": config.api_key(), "Accept": "application/json"},
        )
    if response.status_code == 401:
        raise RuntimeError("Hevy rejected the API key (401). Check HEVY_API_KEY.")
    if response.status_code == 404:
        raise RuntimeError(f"Not found: {path}")
    response.raise_for_status()
    return response.json()


def page_params(page: int, page_size: int, max_page_size: int = config.MAX_PAGE_SIZE) -> dict:
    """Build Hevy's pagination query, clamped to what the endpoint accepts."""
    return {"page": max(1, page), "pageSize": clamp(page_size, 1, max_page_size)}


def clamp(value: int, low: int, high: int) -> int:
    return max(low, min(value, high))
