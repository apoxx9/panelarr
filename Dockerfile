FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /app

COPY src/ ./src/
RUN dotnet build ./src/Panelarr.sln -c Release --no-restore -o /build 2>/dev/null || \
    dotnet restore ./src/Panelarr.sln && \
    dotnet build ./src/Panelarr.sln -c Release -o /build

# ── Runtime image ──────────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/aspnet:6.0 AS runtime

ARG PUID=1000
ARG PGID=1000

RUN groupadd -g "${PGID}" panelarr && \
    useradd -u "${PUID}" -g panelarr -m panelarr

# Install prerequisites
RUN apt-get update && apt-get install -y \
    curl \
    sqlite3 \
    && rm -rf /var/lib/apt/lists/*

WORKDIR /app
COPY --from=build /build .

# Create data directory
RUN mkdir -p /config /comics && \
    chown -R panelarr:panelarr /config /comics /app

USER panelarr

ENV PANELARR_CONFIG_DIR=/config \
    PANELARR_DATA_DIR=/config \
    ASPNETCORE_URLS=http://+:8787

EXPOSE 8787

VOLUME ["/config", "/comics"]

ENTRYPOINT ["dotnet", "Panelarr.dll"]
CMD ["--nobrowser", "--data=/config"]
