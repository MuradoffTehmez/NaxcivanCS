#!/usr/bin/env bash
#
# PRD 137 - MVP acceptance kriteriyalarinin avtomatlasdirilmis hissesi.
#
# Dedicated serveri headless qaldirir, iki client qosur ve hər ikisinin
# handshake + snapshot axinini aldigini yoxlayir. Hər hansi proses sifirdan
# fərqli kodla cixsa və ya log-da ERROR olsa, skript ugursuz olur.
#
# Istifade:
#   tools/e2e-smoke-test.sh [godot-binary]
#
# GODOT_BIN muhit deyiseni ile de verile biler.

set -euo pipefail

REPO_ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$REPO_ROOT"

GODOT="${1:-${GODOT_BIN:-godot}}"
PORT="${PORT:-27015}"
SERVER_SECONDS="${SERVER_SECONDS:-14}"
CLIENT_SECONDS="${CLIENT_SECONDS:-6}"

LOG_DIR="$(mktemp -d)"
trap 'rm -rf "$LOG_DIR"' EXIT

echo "==> Godot: $GODOT"
"$GODOT" --version

echo "==> Asset import"
"$GODOT" --headless --path server --import > "$LOG_DIR/import-server.log" 2>&1
"$GODOT" --headless --path client --import > "$LOG_DIR/import-client.log" 2>&1

echo "==> Dedicated server qaldirilir (port $PORT)"
"$GODOT" --headless --path server -- \
    --port "$PORT" --smoke-test-seconds "$SERVER_SECONDS" > "$LOG_DIR/server.log" 2>&1 &
SERVER_PID=$!
sleep 3

echo "==> Client 1 qosulur"
"$GODOT" --headless --path client -- \
    --server 127.0.0.1 --port "$PORT" --name TestAlpha \
    --smoke-test-seconds "$CLIENT_SECONDS" > "$LOG_DIR/client1.log" 2>&1 &
CLIENT1_PID=$!
sleep 1

echo "==> Client 2 qosulur"
"$GODOT" --headless --path client -- \
    --server 127.0.0.1 --port "$PORT" --name TestBravo \
    --smoke-test-seconds "$CLIENT_SECONDS" > "$LOG_DIR/client2.log" 2>&1
CLIENT2_STATUS=$?

wait "$CLIENT1_PID" && CLIENT1_STATUS=0 || CLIENT1_STATUS=$?
wait "$SERVER_PID"  && SERVER_STATUS=0  || SERVER_STATUS=$?

echo
echo "===================== LOGLAR ====================="
for name in server client1 client2; do
    echo "----- $name -----"
    grep -vE '^\s+\[|^\s+at:|C# backtrace' "$LOG_DIR/$name.log" || true
    echo
done

FAILED=0

check_exit() {
    if [ "$2" -ne 0 ]; then
        echo "FAIL: $1 sifirdan ferqli kodla cixdi ($2)"
        FAILED=1
    fi
}

check_exit "server"  "$SERVER_STATUS"
check_exit "client1" "$CLIENT1_STATUS"
check_exit "client2" "$CLIENT2_STATUS"

for name in server client1 client2; do
    if grep -q "ERROR:" "$LOG_DIR/$name.log"; then
        echo "FAIL: $name log-unda ERROR var"
        grep "ERROR:" "$LOG_DIR/$name.log" | head -5
        FAILED=1
    fi
done

# Hər iki client snapshot almalidir (PRD 44).
for name in client1 client2; do
    if ! grep -qE 'snapshot=[1-9][0-9]*' "$LOG_DIR/$name.log"; then
        echo "FAIL: $name snapshot almadi"
        FAILED=1
    fi
done

# Server hər iki oyuncunu qebul etmeli ve komandalara bolmelidir (PRD 7).
if [ "$(grep -c 'Oyunçu əlavə edildi' "$LOG_DIR/server.log")" -ne 2 ]; then
    echo "FAIL: server iki oyuncu qebul etmedi"
    FAILED=1
fi

if [ "$FAILED" -eq 0 ]; then
    echo "OK: end-to-end smoke test kecdi (2 oyuncu, handshake, snapshot axini)"
fi

exit "$FAILED"
