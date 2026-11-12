#!/usr/bin/env bash
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
SERVICE_NAME="boardgame-web.service"
SOURCE_UNIT="$SCRIPT_DIR/services/$SERVICE_NAME"
TARGET_DIR="$HOME/.config/systemd/user"
TARGET_UNIT="$TARGET_DIR/$SERVICE_NAME"

ACTION="install"
AUTO_START=0
DO_PUBLISH=1

while [[ $# -gt 0 ]]; do
    case "$1" in
        --uninstall|-u)
            ACTION="uninstall"
            shift
            ;;
        --start|-s)
            AUTO_START=1
            shift
            ;;
        --no-publish)
            DO_PUBLISH=0
            shift
            ;;
        --publish|-p)
            DO_PUBLISH=1
            shift
            ;;
        --status)
            ACTION="status"
            shift
            ;;
        *)
            echo "Unknown option: $1" >&2
            echo "Usage: ./install-service.sh [--start] [--no-publish] [--uninstall] [--status]" >&2
            exit 1
            ;;
    esac
done

if [ "$ACTION" = "status" ]; then
    systemctl --user status "$SERVICE_NAME" || true
    exit 0
fi

if [ "$ACTION" = "uninstall" ]; then
    echo ">> Disabling and stopping $SERVICE_NAME..."
    systemctl --user stop "$SERVICE_NAME" 2>/dev/null || true
    systemctl --user disable "$SERVICE_NAME" 2>/dev/null || true
    
    if [ -L "$TARGET_UNIT" ] || [ -f "$TARGET_UNIT" ]; then
        echo ">> Removing symlink: $TARGET_UNIT"
        rm -f "$TARGET_UNIT"
    fi
    systemctl --user daemon-reload
    echo ">> Service uninstalled successfully."
    exit 0
fi

# Performance & reliability flags on Linux
export DOTNET_CLI_TELEMETRY_OPTOUT=1
export DOTNET_SKIP_FIRST_TIME_EXPERIENCE=1
export DOTNET_NOLOGO=1
export DOTNET_CLI_WORKLOAD_UPDATE_NOTIFY_DISABLE=1
export DOTNET_SYSTEM_NET_DISABLEIPV6=1

# 1. Optionally compile/publish Release binary for optimal performance
if [ "$DO_PUBLISH" -eq 1 ]; then
    # Locate dotnet binary
    DOTNET_BIN="dotnet"
    if [ -x "$HOME/.dotnet/dotnet" ]; then
        DOTNET_BIN="$HOME/.dotnet/dotnet"
    elif [ -x "/var/home/david/.dotnet/dotnet" ]; then
        DOTNET_BIN="/var/home/david/.dotnet/dotnet"
    elif [ -x "/home/linuxbrew/.linuxbrew/bin/dotnet" ]; then
        DOTNET_BIN="/home/linuxbrew/.linuxbrew/bin/dotnet"
    elif [ -x "/var/home/linuxbrew/.linuxbrew/bin/dotnet" ]; then
        DOTNET_BIN="/var/home/linuxbrew/.linuxbrew/bin/dotnet"
    fi

    echo ">> Building BoardGame.Web (Release)..."
    "$DOTNET_BIN" build "$SCRIPT_DIR/BoardGame/BoardGame.Web/BoardGame.Web.csproj" \
        -c Release \
        --no-restore

    echo ">> Publishing BoardGame.Web to $SCRIPT_DIR/publish..."
    "$DOTNET_BIN" publish "$SCRIPT_DIR/BoardGame/BoardGame.Web/BoardGame.Web.csproj" \
        -c Release \
        -o "$SCRIPT_DIR/publish" \
        --no-build
fi

# 2. Ensure target user directory exists
mkdir -p "$TARGET_DIR"

# 3. Create or update symlink
echo ">> Symlinking:"
echo "   Source: $SOURCE_UNIT"
echo "   Target: $TARGET_UNIT"
ln -sfn "$SOURCE_UNIT" "$TARGET_UNIT"

# 4. Reload systemd user daemon
echo ">> Reloading systemd user daemon..."
systemctl --user daemon-reload

echo "=========================================================="
echo " ✅ Successfully installed $SERVICE_NAME"
echo "=========================================================="
echo " Symlink: $TARGET_UNIT -> $SOURCE_UNIT"
echo ""

if [ "$AUTO_START" -eq 1 ]; then
    echo ">> Enabling and starting $SERVICE_NAME..."
    systemctl --user enable --now "$SERVICE_NAME"
    echo ""
    systemctl --user status "$SERVICE_NAME" --no-pager || true
else
    echo " Next steps to start and manage the service:"
    echo "   Start now & enable on boot:"
    echo "     systemctl --user enable --now $SERVICE_NAME"
    echo ""
    echo "   Check service status:"
    echo "     systemctl --user status $SERVICE_NAME"
    echo ""
    echo "   View live logs:"
    echo "     journalctl --user -u $SERVICE_NAME -f"
    echo ""
    echo "   Keep running after logout (optional):"
    echo "     loginctl enable-linger $USER"
    echo ""
    echo "   To stop or uninstall:"
    echo "     ./install-service.sh --uninstall"
fi
echo "=========================================================="
