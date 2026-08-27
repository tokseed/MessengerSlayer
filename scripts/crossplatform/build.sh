#!/usr/bin/env bash

set -euo pipefail

source "$(dirname "$0")/common.sh"

require_command dotnet

cd "$REPO_ROOT"

echo "[1/3] Restore"
dotnet restore "./src/MessengerSlayer.slnx"

echo
echo "[2/3] Debug"
dotnet build "./src/MessengerSlayer.slnx" \
    -c Debug \
    --no-restore

echo
echo "[3/3] Release"
dotnet build "./src/MessengerSlayer.slnx" \
    -c Release \
    --no-restore
