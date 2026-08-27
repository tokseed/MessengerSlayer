#!/usr/bin/env bash
set -euo pipefail

ROOT="$(cd "$(dirname "$0")/src" && pwd)"
SOLUTION="$ROOT/MessengerSlayer.slnx"

echo "Restoring NuGet packages..."
dotnet restore "$SOLUTION"

echo "Building MessengerSlayer..."
dotnet build "$SOLUTION" --no-restore -m:1 -p:UsedAvaloniaProducts=

case "${1:-full}" in
  server)
    exec dotnet run --project "$ROOT/Messenger.Server/Messenger.Server.csproj" --no-build
    ;;
  client)
    exec dotnet run --project "$ROOT/Messenger.Client/Messenger.Client.csproj" --no-build
    ;;
  full)
    dotnet run --project "$ROOT/Messenger.Server/Messenger.Server.csproj" --no-build &
    SERVER_PID=$!
    trap 'kill "$SERVER_PID" 2>/dev/null || true' EXIT INT TERM
    sleep 3
    dotnet run --project "$ROOT/Messenger.Client/Messenger.Client.csproj" --no-build
    ;;
  *)
    echo "Usage: $0 [full|server|client]"
    exit 2
    ;;
esac
