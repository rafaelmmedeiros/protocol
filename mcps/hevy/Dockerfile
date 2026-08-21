FROM python:3.12-slim

WORKDIR /app

# Install dependencies first so edits to the source do not invalidate the layer
COPY requirements.txt .
RUN pip install --no-cache-dir -r requirements.txt

# Install the package itself
COPY pyproject.toml README.md ./
COPY src ./src
RUN pip install --no-cache-dir --no-deps .

# Credential env var (provided at runtime via --env-file)
ENV HEVY_API_KEY=""

CMD ["hevy-mcp"]
