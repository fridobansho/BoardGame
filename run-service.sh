#!/usr/bin/env bash
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"

# Ensure .NET is located
if [ -x "$HOME/.dotnet/dotnet" ]; then
    DOTNET_BIN="$HOME/.dotnet/dotnet"
elif [ -x "/var/home/david/.dotnet/dotnet" ]; then
    DOTNET_BIN="/var/home/david/.dotnet/dotnet"
elif command -v dotnet >/dev/null 2>&1; then
    DOTNET_BIN="$(command -v dotnet)"
else
    echo "Error: dotnet binary not found" >&2
    exit 1
fi

export DOTNET_CLI_TELEMETRY_OPTOUT=1
export DOTNET_SKIP_FIRST_TIME_EXPERIENCE=1
export DOTNET_NOLOGO=1
export DOTNET_CLI_WORKLOAD_UPDATE_NOTIFY_DISABLE=1
export DOTNET_SYSTEM_NET_DISABLEIPV6=1

PORT="${PORT:-5000}"
URLS="${URLS:-http://0.0.0.0:${PORT}}"

if [ -f "$SCRIPT_DIR/publish/BoardGame.Web.dll" ]; then
    cd "$SCRIPT_DIR/publish"
    exec "$DOTNET_BIN" "$SCRIPT_DIR/publish/BoardGame.Web.dll" --urls "$URLS" "$@"
else
    cd "$SCRIPT_DIR/BoardGame/BoardGame.Web"
    exec "$DOTNET_BIN" run --project "$SCRIPT_DIR/BoardGame/BoardGame.Web/BoardGame.Web.csproj" --no-restore --urls "$URLS" "$@"
fi
