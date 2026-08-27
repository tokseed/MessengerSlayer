#!/bin/bash

set -euo pipefail

SCRIPT_DIR="$(cd -- "$(dirname -- "$0")" && pwd)"
REPO_ROOT="$(cd -- "$SCRIPT_DIR/.." && pwd)"
SCRIPTS="$REPO_ROOT/scripts/crossplatform"
LOG_DIR="$REPO_ROOT/.runtime"

mkdir -p "$LOG_DIR"

echo
echo "========================================"
echo "MessengerSlayer - macOS"
echo "========================================"
echo

if ! command -v docker >/dev/null 2>&1; then
    echo "ERROR: Docker was not found."
    echo "Install and start Docker Desktop first."
    read -r -p "Press Enter to close..."
    exit 1
fi

if ! command -v dotnet >/dev/null 2>&1; then
    echo "ERROR: .NET SDK was not found."
    read -r -p "Press Enter to close..."
    exit 1
fi

chmod +x "$SCRIPTS"/*.sh

echo "[1/4] Starting SQL Server container..."
"$SCRIPTS/db-up.sh"

echo
echo "[2/4] Building project..."
"$SCRIPTS/build.sh"

echo
echo "[3/4] Starting server..."
nohup "$SCRIPTS/run-server.sh" \
    >"$LOG_DIR/server.log" \
    2>&1 &

SERVER_PID=$!
echo "$SERVER_PID" > "$LOG_DIR/server.pid"

sleep 5

if ! kill -0 "$SERVER_PID" 2>/dev/null; then
    echo
    echo "ERROR: Server stopped during startup."
    echo "Last server log:"
    tail -n 80 "$LOG_DIR/server.log" || true
    read -r -p "Press Enter to close..."
    exit 1
fi

echo "Server PID: $SERVER_PID"
echo "Server log: $LOG_DIR/server.log"

echo
echo "[4/4] Starting client..."
"$SCRIPTS/run-client.sh"

STATUS=$?

echo
read -r -p "Client closed. Press Enter to close this launcher..."
exit "$STATUS"
