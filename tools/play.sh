#!/usr/bin/env bash
#
# "Sadece oyuna baxmaq isteyirem" rejimi.
#
# Serveri arxa fonda qaldirir, sonra client pencereni acir.
# Client baglananda server de dayandirilir.
#
#   tools/play.sh          # 1 client
#   tools/play.sh 2        # 2 client (bir-birinizi gorursunuz)

set -euo pipefail

REPO_ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$REPO_ROOT"
source tools/godot-env.sh

CLIENTS="${1:-1}"
PORT=27015
SERVER_LOG="$(mktemp)"

echo "==> Build..."
dotnet build server/NaxcivanCS.Server.csproj --nologo -v q
dotnet build client/NaxcivanCS.Client.csproj --nologo -v q

echo "==> Dedicated server qalxir (port $PORT)"
"$GODOT" --headless --path server -- --port "$PORT" --map NC_Qala > "$SERVER_LOG" 2>&1 &
SERVER_PID=$!

cleanup() {
    echo
    echo "==> Server dayandirilir"
    kill "$SERVER_PID" 2> /dev/null || true
    wait "$SERVER_PID" 2> /dev/null || true
    echo "--- server logu ---"
    grep -vE '^\s+\[|^\s+at:|C# backtrace' "$SERVER_LOG" || true
    rm -f "$SERVER_LOG"
}
trap cleanup EXIT

sleep 3

if ! kill -0 "$SERVER_PID" 2> /dev/null; then
    echo "XETA: server qalxmadi:" >&2
    cat "$SERVER_LOG" >&2
    exit 1
fi

PIDS=()
for i in $(seq 1 "$CLIENTS"); do
    echo "==> Client $i acilir"
    "$GODOT" --path client -- --server 127.0.0.1 --port "$PORT" --name "Player$i" &
    PIDS+=($!)
    sleep 1
done

echo
echo "Pencereye klikleyin ki kursor tutulsun. WASD + mouse, sol klik = atesh, Esc = kursoru burax."
echo "Bitirmek ucun pencereleri baglayin."
echo

for pid in "${PIDS[@]}"; do
    wait "$pid" 2> /dev/null || true
done
