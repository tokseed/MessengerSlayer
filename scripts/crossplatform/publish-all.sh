#!/usr/bin/env bash

set -euo pipefail

source "$(dirname "$0")/common.sh"

require_command dotnet

rids=(
    "win-x64"
    "win-arm64"
    "linux-x64"
    "linux-arm64"
    "osx-x64"
    "osx-arm64"
)

publish_root="$REPO_ROOT/publish"

rm -rf "$publish_root"
mkdir -p "$publish_root"

cd "$REPO_ROOT"

dotnet restore "./src/MessengerSlayer.slnx"

for rid in "${rids[@]}"; do
    echo
    echo "Publishing $rid..."

    dotnet publish "./src/Messenger.Client/Messenger.Client.csproj" \
        -c Release \
        -r "$rid" \
        --self-contained true \
        -o "$publish_root/$rid/client"

    dotnet publish "./src/Messenger.Server/Messenger.Server.csproj" \
        -c Release \
        -r "$rid" \
        --self-contained true \
        -o "$publish_root/$rid/server"
done

echo
echo "Published to:"
echo "  $publish_root"
