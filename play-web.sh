#!/usr/bin/env bash
set -euo pipefail

# Ensure .NET is available on PATH
if ! command -v dotnet >/dev/null 2>&1; then
    if [ -x "$HOME/.dotnet/dotnet" ]; then
        export PATH="$HOME/.dotnet:$PATH"
    elif [ -x "/var/home/david/.dotnet/dotnet" ]; then
        export PATH="/var/home/david/.dotnet:$PATH"
    elif [ -x "/home/linuxbrew/.linuxbrew/bin/dotnet" ]; then
        export PATH="/home/linuxbrew/.linuxbrew/bin:$PATH"
    fi
fi

# Performance & reliability flags on Linux
export DOTNET_CLI_TELEMETRY_OPTOUT=1
export DOTNET_SKIP_FIRST_TIME_EXPERIENCE=1
export DOTNET_NOLOGO=1
export DOTNET_CLI_WORKLOAD_UPDATE_NOTIFY_DISABLE=1
export DOTNET_SYSTEM_NET_DISABLEIPV6=1

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT_PATH="$SCRIPT_DIR/BoardGame/BoardGame.Web/BoardGame.Web.csproj"
PORT="${PORT:-5000}"

WATCH_MODE=1

# Parse command line flags
while [[ $# -gt 0 ]]; do
    case "$1" in
        --no-watch)
            WATCH_MODE=0
            shift
            ;;
        --watch|-w)
            WATCH_MODE=1
            shift
            ;;
        --port|-p)
            PORT="$2"
            shift 2
            ;;
        *)
            break
            ;;
    esac
done

# Detect primary LAN IP address
LAN_IP=""
if command -v ip >/dev/null 2>&1; then
    LAN_IP="$(ip route get 1.1.1.1 2>/dev/null | awk '{print $7; exit}' || true)"
fi
if [ -z "$LAN_IP" ] && command -v hostname >/dev/null 2>&1; then
    LAN_IP="$(hostname -I 2>/dev/null | awk '{print $1}' || true)"
fi
LAN_IP="${LAN_IP:-127.0.0.1}"

echo "=========================================================="
echo " 🎲 BoardGame Mobile-Friendly Local Web Server"
echo "=========================================================="
echo ""
echo "  💻 Local:   http://localhost:${PORT}"
if [ "$LAN_IP" != "127.0.0.1" ]; then
    echo "  📱 Mobile:  http://${LAN_IP}:${PORT}"
fi
echo ""
echo "  Games available:"
echo "    • 🔴 Connect Four (Pass & Play, vs AI, or Wi-Fi Room)"
echo "    • ❌ Tic-Tac-Toe (Pass & Play, vs Unbeatable Minimax, or Wi-Fi Room)"
echo "    • 🍫 Chomp (Pass & Play, vs Game-Theory Solver, or Wi-Fi Room)"
echo ""
echo "  To play on your phone:"
echo "    1. Connect your phone to the same local Wi-Fi."
echo "    2. Open your mobile browser and go to:"
echo "       👉 http://${LAN_IP}:${PORT}"
echo "    3. Tap 'Share' in the web app to display a QR code for other devices!"
echo ""
echo "  Note: If your phone cannot connect, verify your firewall permits port ${PORT}:"
echo "        sudo firewall-cmd --add-port=${PORT}/tcp --temporary"
if [ "$WATCH_MODE" -eq 1 ]; then
    echo "  ⚡ AUTO-RECOMPILE ENABLED: changes to C# and web files reload on save!"
    echo "     (Pass --no-watch to run standard single-build server)"
    echo "=========================================================="
    echo ""
    exec dotnet watch --project "$PROJECT_PATH" run --no-restore --urls "http://0.0.0.0:${PORT}" "$@"
else
    echo "  Standard mode active (no watch)."
    echo "=========================================================="
    echo ""
    exec dotnet run --project "$PROJECT_PATH" --no-restore --urls "http://0.0.0.0:${PORT}" "$@"
fi
