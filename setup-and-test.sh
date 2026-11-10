#!/usr/bin/env bash
set -euo pipefail

# Configuration with sensible defaults (overridable via environment variables)
REPO_URL="${REPO_URL:-https://github.com/fridobansho/BoardGame.git}"
TARGET_DIR="${TARGET_DIR:-BoardGame-workspace}"
BRANCH="${BRANCH:-}"
DOTNET_CHANNEL="${DOTNET_CHANNEL:-8.0}"
DOTNET_DIR="${DOTNET_DIR:-$HOME/.dotnet}"

# Speed up dotnet CLI on Linux: disable telemetry, first-time greeting, and workload update checks
export DOTNET_CLI_TELEMETRY_OPTOUT=1
export DOTNET_SKIP_FIRST_TIME_EXPERIENCE=1
export DOTNET_NOLOGO=1
export DOTNET_CLI_WORKLOAD_UPDATE_NOTIFY_DISABLE=1

echo "=========================================================="
echo " BoardGame CI/Test Script"
echo " Target dir: $TARGET_DIR"
echo " Repo URL:   $REPO_URL"
echo " Branch:     ${BRANCH:-'(default)'}"
echo "=========================================================="

# 1. Ensure .NET SDK is available (check PATH, DOTNET_DIR, or install via dotnet-install.sh)
if command -v dotnet >/dev/null 2>&1; then
    DOTNET_BIN="$(command -v dotnet)"
elif [ -x "$DOTNET_DIR/dotnet" ]; then
    DOTNET_BIN="$DOTNET_DIR/dotnet"
    export PATH="$DOTNET_DIR:$PATH"
else
    echo ">> .NET SDK not detected. Installing .NET $DOTNET_CHANNEL into $DOTNET_DIR..."
    mkdir -p "$DOTNET_DIR"
    curl -sSL https://dot.net/v1/dotnet-install.sh | bash /dev/stdin \
        --channel "$DOTNET_CHANNEL" \
        --install-dir "$DOTNET_DIR"
    DOTNET_BIN="$DOTNET_DIR/dotnet"
    export PATH="$DOTNET_DIR:$PATH"
fi

echo ">> Using .NET SDK: $("$DOTNET_BIN" --version)"

# 2. Clone or update repository (rerunnable)
if [ ! -d "$TARGET_DIR/.git" ]; then
    echo ">> Cloning $REPO_URL into $TARGET_DIR..."
    if [ -n "$BRANCH" ]; then
        git clone --branch "$BRANCH" "$REPO_URL" "$TARGET_DIR"
    else
        git clone "$REPO_URL" "$TARGET_DIR"
    fi
else
    echo ">> Existing repository found at $TARGET_DIR. Updating..."
    git -C "$TARGET_DIR" fetch --prune origin
    if [ -n "$BRANCH" ]; then
        git -C "$TARGET_DIR" checkout "$BRANCH"
        git -C "$TARGET_DIR" pull --ff-only origin "$BRANCH"
    else
        CURRENT_BRANCH="$(git -C "$TARGET_DIR" rev-parse --abbrev-ref HEAD)"
        if [ "$CURRENT_BRANCH" != "HEAD" ]; then
            git -C "$TARGET_DIR" pull --ff-only origin "$CURRENT_BRANCH" || true
        fi
    fi
fi

# Locate solution file (handles root or nested subdirectory)
if [ -f "$TARGET_DIR/BoardGame/BoardGame.sln" ]; then
    SLN_PATH="$TARGET_DIR/BoardGame/BoardGame.sln"
elif [ -f "$TARGET_DIR/BoardGame.sln" ]; then
    SLN_PATH="$TARGET_DIR/BoardGame.sln"
else
    echo "Error: Could not locate BoardGame.sln in $TARGET_DIR" >&2
    exit 1
fi

echo ">> Found solution at: $SLN_PATH"

# 3. Build solution
echo ">> Building solution..."
"$DOTNET_BIN" build "$SLN_PATH" --configuration Release

# 4. Run test suite
echo ">> Running unit test suite..."
"$DOTNET_BIN" test "$SLN_PATH" --configuration Release --no-build --verbosity normal

echo "=========================================================="
echo " All tests passed successfully!"
echo "=========================================================="
