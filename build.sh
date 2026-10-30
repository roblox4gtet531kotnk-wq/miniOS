#!/usr/bin/env bash
# Сборка PPKit на Linux/macOS/CI.
# Нужен .NET SDK 6+ и папка с reference-сборками (из вашей установленной игры
# или собранная вручную): UnityEngine*.dll, BepInEx.dll, 0Harmony.dll.
#
#   PP_GAME_DIR="/path/to/People Playground" ./build.sh
#   PP_REF_DIR="/path/to/refs" ./build.sh
set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
GAME_DIR="${PP_GAME_DIR:-}"
REF_DIR="${PP_REF_DIR:-}"

if ! command -v dotnet >/dev/null 2>&1; then
  echo "dotnet не найден. Установите .NET SDK 6+: https://dotnet.microsoft.com/download" >&2
  exit 1
fi

if [[ -n "$GAME_DIR" ]]; then
  MANAGED="$GAME_DIR/People Playground_Data/Managed"
  if [[ ! -f "$MANAGED/UnityEngine.dll" ]]; then
    echo "В $MANAGED нет UnityEngine.dll — проверьте путь к игре." >&2
    exit 1
  fi
  if [[ ! -f "$GAME_DIR/BepInEx/core/BepInEx.dll" ]]; then
    echo "Предупреждение: BepInEx не установлен в игру — собираемся, но плагин не запустится." >&2
  fi
  echo "Игра: $GAME_DIR"
  export PP_GAME_DIR="$GAME_DIR"
elif [[ -n "$REF_DIR" ]]; then
  echo "Reference-папка: $REF_DIR"
  export PP_REF_DIR="$REF_DIR"
else
  echo "Задайте PP_GAME_DIR или PP_REF_DIR." >&2
  exit 1
fi

echo "Сборка…"
dotnet build "$ROOT/src/PPKit/PPKit.csproj" -c Release --nologo

DLL="$ROOT/src/PPKit/bin/Release/PPKit.dll"
[[ -f "$DLL" ]] || { echo "Не найден $DLL" >&2; exit 1; }

DIST="$ROOT/dist"
PLUGIN_DIR="$DIST/BepInEx/plugins/PPKit"
mkdir -p "$PLUGIN_DIR"
cp -f "$DLL" "$PLUGIN_DIR/"
cp -f "$ROOT/src/PPKit/Loc/ru.json" "$DIST/" 2>/dev/null || true
cp -f "$ROOT/README.md" "$DIST/" 2>/dev/null || true
cp -f "$ROOT/packaging/manifest.json" "$PLUGIN_DIR/" 2>/dev/null || true

rm -f "$DIST/ppkit-thunderstore.zip"
( cd "$DIST" && zip -qr ppkit-thunderstore.zip BepInEx manifest.json 2>/dev/null \
  || zip -qr ppkit-thunderstore.zip BepInEx )

echo "Готово: $PLUGIN_DIR"
echo "Архив: $DIST/ppkit-thunderstore.zip"
