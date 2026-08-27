#!/usr/bin/env bash

set -euo pipefail

source "$(dirname "$0")/common.sh"

require_command dotnet

cd "$REPO_ROOT"

exec dotnet run \
    --project "./src/Messenger.Client/Messenger.Client.csproj"
