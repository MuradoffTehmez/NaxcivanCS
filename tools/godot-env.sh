#!/usr/bin/env bash
#
# Godot 4 Mono binarini tapir ve GODOT deyisenine yazir.
# Diger skriptler bunu `source` edir.
#
# Axtaris sirasi:
#   1. GODOT_BIN muhit deyiseni
#   2. PATH-deki `godot`
#   3. winget paket qovlugu (Windows)
#   4. Adi Linux/macOS yollari

set -uo pipefail

find_godot() {
    if [ -n "${GODOT_BIN:-}" ] && [ -x "$GODOT_BIN" ]; then
        echo "$GODOT_BIN"
        return 0
    fi

    if command -v godot > /dev/null 2>&1; then
        command -v godot
        return 0
    fi

    # Windows - winget ile qurasdirilmis Godot Mono.
    if [ -n "${LOCALAPPDATA:-}" ]; then
        local winget_dir="$LOCALAPPDATA/Microsoft/WinGet/Packages"
        local found
        found="$(find "$winget_dir" -maxdepth 3 -name "Godot_v*_mono_win64_console.exe" 2>/dev/null | sort -r | head -1)"
        if [ -n "$found" ]; then
            echo "$found"
            return 0
        fi
    fi

    # Linux / macOS - adi yerler.
    for candidate in \
        "$HOME/godot/Godot_v"*_mono_linux.x86_64 \
        /usr/local/bin/godot4 \
        /Applications/Godot_mono.app/Contents/MacOS/Godot; do
        if [ -x "$candidate" ]; then
            echo "$candidate"
            return 0
        fi
    done

    return 1
}

GODOT="$(find_godot)" || {
    echo "XETA: Godot 4 Mono tapilmadi." >&2
    echo >&2
    echo "Qurasdirmaq ucun (Windows):" >&2
    echo "  winget install --id GodotEngine.GodotEngine.Mono --exact" >&2
    echo >&2
    echo "Ve ya GODOT_BIN deyisenini elle verin:" >&2
    echo "  GODOT_BIN=/path/to/godot tools/run-server.sh" >&2
    exit 1
}

export GODOT
