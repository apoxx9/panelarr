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
# Four-part assembly version (e.g. 1.1.2.123); empty keeps the 1.0.0.* dev default
ARG VERSION=""

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
        -p:RunAnalyzers=false \
        ${VERSION:+-p:AssemblyVersion=$VERSION} && \
    dotnet publish ./src/NzbDrone.Mono/Panelarr.Mono.csproj \
        -c Release \
        -f net10.0 \
        -r linux-${TARGETARCH:-x64} \
        --self-contained false \
        -o /build \
        --no-restore \
        -p:RunAnalyzers=false \
        ${VERSION:+-p:AssemblyVersion=$VERSION}

# ── Runtime image ──────────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime

LABEL maintainer="Panelarr Team" \
      org.opencontainers.image.title="Panelarr" \
      org.opencontainers.image.description="Comic book management application" \
      org.opencontainers.image.url="https://github.com/apoxx9/panelarr" \
      org.opencontainers.image.source="https://github.com/apoxx9/panelarr"

# Install prerequisites + gosu for privilege drop
RUN apt-get update && apt-get install -y \
    curl \
    sqlite3 \
    gosu \
    && rm -rf /var/lib/apt/lists/*

# Create default panelarr user (entrypoint adjusts UID/GID at runtime)
RUN groupadd -f -g 1000 panelarr && \
    useradd -o -u 1000 -g 1000 -m panelarr

WORKDIR /app
COPY --from=backend /build .
COPY --from=frontend /app/_output/UI ./UI
COPY docker/entrypoint.sh /entrypoint.sh

# Create data directories
RUN mkdir -p /config /comics /downloads && \
    chown -R panelarr:panelarr /config /comics /downloads /app

ENV PANELARR_CONFIG_DIR=/config \
    PANELARR_DATA_DIR=/config \
    ASPNETCORE_URLS=http://+:8787

EXPOSE 8787

VOLUME ["/config", "/comics", "/downloads"]

HEALTHCHECK --interval=30s --timeout=10s --start-period=60s --retries=3 \
    CMD curl -f http://localhost:8787/ping || exit 1

ENTRYPOINT ["/entrypoint.sh"]
CMD ["--nobrowser", "--data=/config"]
