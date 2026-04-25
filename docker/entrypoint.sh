#!/bin/bash
set -e

PUID=${PUID:-1000}
PGID=${PGID:-1000}

groupmod -o -g "$PGID" panelarr 2>/dev/null || true
usermod -o -u "$PUID" panelarr 2>/dev/null || true

chown -R panelarr:panelarr /config /app
chown panelarr:panelarr /comics /downloads 2>/dev/null || true

exec gosu panelarr dotnet Panelarr.dll "$@"
