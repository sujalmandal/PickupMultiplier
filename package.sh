#!/usr/bin/env bash
set -euo pipefail

ROOT="$(cd "$(dirname "$0")" && pwd)"
PROJECT="$ROOT/src/PickupMultiplier/PickupMultiplier.csproj"
VERSION="$(grep -oE '<Version>[^<]+' "$PROJECT" | sed 's/<Version>//')"
STAGE="$ROOT/dist/stage"
ARCHIVE="$ROOT/dist/PickupMultiplier-$VERSION.zip"

"$ROOT/build.sh" --deploy "$@"

rm -rf "$STAGE"
mkdir -p "$STAGE/mods/PickupMultiplier"
cp "$ROOT/src/PickupMultiplier/bin/Release/PickupMultiplier.dll" "$STAGE/mods/"
cp "$ROOT/src/PickupMultiplier/manifest.json" "$STAGE/mods/PickupMultiplier/"
cp "$ROOT/README.md" "$STAGE/"

rm -f "$ARCHIVE"
(cd "$STAGE" && zip -r -X -q "$ARCHIVE" .)

echo "packaged $ARCHIVE"
unzip -l "$ARCHIVE"
