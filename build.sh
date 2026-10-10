#!/usr/bin/env bash
set -euo pipefail

ROOT="$(cd "$(dirname "$0")" && pwd)"
DOTNET="${DOTNET:-$HOME/.dotnet/dotnet}"
GAME="${GAME:-$HOME/Library/Application Support/CrossOver/Bottles/Steam/drive_c/Program Files (x86)/Steam/steamapps/common/Sons Of The Forest}"
PROJECT="$ROOT/src/PickupMultiplier/PickupMultiplier.csproj"
OUTPUT="$ROOT/src/PickupMultiplier/bin/Release"
MOD_NAME="PickupMultiplier"

DEPLOY=false
BUILD_ARGS=()
for arg in "$@"; do
    if [[ "$arg" == "--deploy" ]]; then
        DEPLOY=true
    else
        BUILD_ARGS+=("$arg")
    fi
done

export DOTNET_CLI_TELEMETRY_OPTOUT=1 DOTNET_NOLOGO=1
"$DOTNET" build "$PROJECT" -c Release -p:GameDir="$GAME" "${BUILD_ARGS[@]+"${BUILD_ARGS[@]}"}"

if [[ "$DEPLOY" != true ]]; then
    exit 0
fi

if pgrep -f 'SonsOfTheForest\.exe' >/dev/null 2>&1; then
    echo "refusing to deploy: SonsOfTheForest.exe is running" >&2
    exit 1
fi

mkdir -p "$GAME/mods/$MOD_NAME"
cp "$OUTPUT/$MOD_NAME.dll" "$GAME/mods/"
cp "$ROOT/src/$MOD_NAME/manifest.json" "$GAME/mods/$MOD_NAME/"
chmod 644 "$GAME/mods/$MOD_NAME.dll" "$GAME/mods/$MOD_NAME/manifest.json"
echo "deployed to $GAME/mods"
ls -la "$GAME/mods/$MOD_NAME.dll" "$GAME/mods/$MOD_NAME/manifest.json"
