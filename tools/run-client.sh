#!/usr/bin/env bash
#
# Oyun client-ini PENCERE ile isle salir - vizual gorunus burdadir.
#
#   tools/run-client.sh              # ad avtomatik
#   tools/run-client.sh Tahmaz       # oz adinla
#   tools/run-client.sh Rasim 27020  # basqa port
#
# Idareetme:
#   Pencereye klikle  -> oyuna gir (kursor tutulur)
#   WASD              -> herekat
#   Mouse             -> baxis
#   Sol klik          -> atesh
#   Space / Ctrl / Shift -> tullanma / comelme / addimlama
#   Esc               -> kursoru burax

set -euo pipefail

REPO_ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$REPO_ROOT"
source tools/godot-env.sh

NAME="${1:-Player$RANDOM}"
PORT="${2:-27015}"

echo "==> Client build edilir..."
dotnet build client/NaxcivanCS.Client.csproj --nologo -v q

echo "==> Client acilir: $NAME -> 127.0.0.1:$PORT"
echo "    Pencereye klikleyin ki kursor tutulsun. Cixmaq: Esc, sonra pencereni baglayin."
exec "$GODOT" --path client -- --server 127.0.0.1 --port "$PORT" --name "$NAME"
