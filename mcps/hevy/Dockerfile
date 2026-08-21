FROM python:3.12-slim

# uv installs straight from uv.lock, which is the single source of truth for
# dependency versions -- the dev loop (`uv run`) and this image resolve identically.
COPY --from=ghcr.io/astral-sh/uv:0.11.25 /uv /usr/local/bin/uv

WORKDIR /app

ENV UV_COMPILE_BYTECODE=1 \
    UV_LINK_MODE=copy \
    UV_PROJECT_ENVIRONMENT=/app/.venv

# Install dependencies first so edits to the source do not invalidate the layer
COPY pyproject.toml uv.lock ./
RUN uv sync --locked --no-dev --no-install-project

# Install the package itself
COPY src ./src
RUN uv sync --locked --no-dev

ENV PATH="/app/.venv/bin:$PATH"

# Credential env var (provided at runtime via --env-file)
ENV HEVY_API_KEY=""

CMD ["hevy-mcp"]
