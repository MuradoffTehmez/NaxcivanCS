#!/usr/bin/env bash
#
# Dedicated serveri headless isle salir (PRD 87).
#
#   tools/run-server.sh              # default port 27015, NC_Qala
#   tools/run-server.sh 27020        # basqa port
#
# Dayandirmaq: Ctrl+C

set -euo pipefail

REPO_ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$REPO_ROOT"
source tools/godot-env.sh

PORT="${1:-27015}"

echo "==> Server build edilir..."
dotnet build server/NaxcivanCS.Server.csproj --nologo -v q

echo "==> Dedicated server qalxir (port $PORT). Dayandirmaq ucun Ctrl+C."
exec "$GODOT" --headless --path server -- --port "$PORT" --map NC_Qala
