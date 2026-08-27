#!/usr/bin/env bash

set -euo pipefail

SCRIPT_DIR="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd -- "$SCRIPT_DIR/../.." && pwd)"
ENV_FILE="$REPO_ROOT/.env"

require_command() {
    local name="$1"

    if ! command -v "$name" >/dev/null 2>&1; then
        echo "Required command '$name' was not found." >&2
        exit 1
    fi
}

dotenv_value() {
    local key="$1"
    local default_value="$2"

    if [[ ! -f "$ENV_FILE" ]]; then
        printf '%s' "$default_value"
        return
    fi

    local line
    line="$(
        grep -E "^[[:space:]]*${key}=" "$ENV_FILE" |
        tail -n 1 ||
        true
    )"

    if [[ -z "$line" ]]; then
        printf '%s' "$default_value"
        return
    fi

    local value="${line#*=}"
    value="${value%$'\r'}"

    if [[ "$value" == \"*\" && "$value" == *\" ]]; then
        value="${value:1:${#value}-2}"
    elif [[ "$value" == \'*\' && "$value" == *\' ]]; then
        value="${value:1:${#value}-2}"
    fi

    printf '%s' "$value"
}

wait_sql_container() {
    local timeout_seconds="${1:-90}"
    local elapsed=0

    echo "Waiting for SQL Server container..."

    while (( elapsed < timeout_seconds )); do
        if docker logs messengerslayer-sql 2>&1 |
            grep -q "SQL Server is now ready for client connections"; then
            echo "SQL Server is ready."
            return 0
        fi

        sleep 2
        elapsed=$((elapsed + 2))
    done

    echo
    echo "SQL Server did not report readiness within ${timeout_seconds}s."
    docker compose -f "$REPO_ROOT/docker-compose.yml" ps || true
    return 1
}
