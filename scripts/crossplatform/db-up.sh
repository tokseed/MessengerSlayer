#!/usr/bin/env bash

set -euo pipefail

source "$(dirname "$0")/common.sh"

require_command docker

cd "$REPO_ROOT"

docker compose up -d sqlserver
wait_sql_container 90
