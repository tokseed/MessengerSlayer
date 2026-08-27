#!/usr/bin/env bash

set -euo pipefail

source "$(dirname "$0")/common.sh"

require_command dotnet

password="$(dotenv_value MSSQL_SA_PASSWORD Your_password123)"
database_port="$(dotenv_value MSSQL_HOST_PORT 1433)"
database="$(dotenv_value MESSENGER_DATABASE MessengerSlayer)"
server_port="$(dotenv_value MESSENGER_SERVER_PORT 5000)"

export ConnectionStrings__DefaultConnection="Server=localhost,${database_port};Database=${database};User Id=sa;Password=${password};TrustServerCertificate=True;"
export TcpServer__Port="$server_port"

cd "$REPO_ROOT/src/Messenger.Server"

echo "Starting colleague Messenger.Server"
echo "Database: localhost:${database_port} / ${database}"
echo "TCP server port: ${server_port}"
echo

exec dotnet run
