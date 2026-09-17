#!/usr/bin/env bash
set -euo pipefail

ROOT="$(cd "$(dirname "$0")/../.." && pwd)"
WIN="$ROOT/windows"
PUBLISH="$WIN/dist/publish"
DIST="$WIN/dist"
CONFIGURATION="${CONFIGURATION:-Release}"

mkdir -p "$PUBLISH" "$DIST"
export DOTNET_CLI_TELEMETRY_OPTOUT=1

dotnet test "$WIN/tests/Tinycast.Windows.Tests/Tinycast.Windows.Tests.csproj" -c "$CONFIGURATION"
dotnet publish "$WIN/src/Tinycast.Windows/Tinycast.Windows.csproj" \
  -c "$CONFIGURATION" \
  -r win-x64 \
  --self-contained true \
  -p:PublishSingleFile=true \
  -p:IncludeNativeLibrariesForSelfExtract=true \
  -o "$PUBLISH"

cp -f "$WIN/src/Tinycast.Windows/Assets/tinycast.ico" "$PUBLISH/tinycast.ico"

if command -v makensis >/dev/null 2>&1; then
  makensis -DSOURCE_DIR="$PUBLISH" -DOUT_DIR="$DIST" "$WIN/installer/tinycast.nsi"
  (cd "$DIST" && sha256sum TinycastSetup.exe > TinycastSetup.exe.sha256)
  echo "Installer: $DIST/TinycastSetup.exe"
else
  echo "makensis not found — published files are in $PUBLISH"
  echo "Install NSIS to wrap them: apt-get install nsis / choco install nsis"
fi
