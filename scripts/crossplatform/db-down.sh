#!/usr/bin/env bash

set -euo pipefail

source "$(dirname "$0")/common.sh"

require_command docker

cd "$REPO_ROOT"

docker compose down
