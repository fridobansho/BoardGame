#!/usr/bin/env bash
set -euo pipefail

# Ensure .NET is available on PATH
if ! command -v dotnet >/dev/null 2>&1; then
    if [ -x "/home/linuxbrew/.linuxbrew/bin/dotnet" ]; then
        export PATH="/home/linuxbrew/.linuxbrew/bin:$PATH"
    elif [ -x "$HOME/.dotnet/dotnet" ]; then
        export PATH="$HOME/.dotnet:$PATH"
    fi
fi

# Speed up dotnet CLI on Linux
export DOTNET_CLI_TELEMETRY_OPTOUT=1
export DOTNET_SKIP_FIRST_TIME_EXPERIENCE=1
export DOTNET_NOLOGO=1
export DOTNET_CLI_WORKLOAD_UPDATE_NOTIFY_DISABLE=1
export DOTNET_SYSTEM_NET_DISABLEIPV6=1

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT_PATH="$SCRIPT_DIR/BoardGame/BoardGame/BoardGame.csproj"

echo "=========================================================="
echo " BoardGame Automated TUI Demo"
echo " Playing in-place (no scrolling) with turn-by-turn animation"
echo " Usage: ./play-demo.sh [connect4|tictactoe|chomp|all] [--delay <ms>] [--alt|--no-alt]"
echo "=========================================================="

dotnet run --project "$PROJECT_PATH" --configuration Release -- --in-place "$@"
