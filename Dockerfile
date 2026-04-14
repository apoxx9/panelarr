# Multi-arch Dockerfile for Panelarr
# Supports: linux/amd64, linux/arm64, linux/arm/v7

ARG BUILDPLATFORM
ARG TARGETPLATFORM
ARG TARGETARCH

# ── Frontend build ─────────────────────────────────────────────────────────────
FROM --platform=${BUILDPLATFORM:-linux/amd64} node:20-slim AS frontend

WORKDIR /app

COPY package.json yarn.lock .yarnrc tsconfig.json ./
COPY frontend/ ./frontend/

RUN yarn install --frozen-lockfile && yarn build

# ── Backend build ──────────────────────────────────────────────────────────────
FROM --platform=${BUILDPLATFORM:-linux/amd64} mcr.microsoft.com/dotnet/sdk:10.0 AS backend

ARG TARGETARCH

WORKDIR /app

COPY src/ ./src/
COPY Logo/ ./Logo/

# Restore and publish for the target architecture
RUN dotnet restore ./src/Panelarr.sln && \
    dotnet publish ./src/NzbDrone.Console/Panelarr.Console.csproj \
        -c Release \
        -f net10.0 \
        -r linux-${TARGETARCH:-x64} \
        --self-contained false \
        -o /build \
        --no-restore \
        -p:RunAnalyzers=false

# ── Runtime image ──────────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime

ARG PUID=1000
ARG PGID=1000

LABEL maintainer="Panelarr Team" \
      org.opencontainers.image.title="Panelarr" \
      org.opencontainers.image.description="Comic book management application" \
      org.opencontainers.image.url="https://github.com/apoxx9/panelarr" \
      org.opencontainers.image.source="https://github.com/apoxx9/panelarr"

RUN groupadd -f -g "${PGID}" panelarr && \
    useradd -o -u "${PUID}" -g "${PGID}" -m panelarr

# Install prerequisites
RUN apt-get update && apt-get install -y \
    curl \
    sqlite3 \
    && rm -rf /var/lib/apt/lists/*

WORKDIR /app
COPY --from=backend /build .
COPY --from=frontend /app/_output/UI ./UI

# Create data directory
RUN mkdir -p /config /comics && \
    chown -R panelarr:panelarr /config /comics /app

USER panelarr

ENV PANELARR_CONFIG_DIR=/config \
    PANELARR_DATA_DIR=/config \
    ASPNETCORE_URLS=http://+:8787

EXPOSE 8787

VOLUME ["/config", "/comics"]

HEALTHCHECK --interval=30s --timeout=10s --start-period=60s --retries=3 \
    CMD curl -f http://localhost:8787/api/v1/system/status || exit 1

ENTRYPOINT ["dotnet", "Panelarr.dll"]
CMD ["--nobrowser", "--data=/config"]
