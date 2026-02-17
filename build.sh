#!/usr/bin/env bash
set -euo pipefail

SLN_PATH="path/to/your.sln"
CONFIG="Release"
VERSIONS=( "1.4" "1.5" "1.6" )

for v in "${VERSIONS[@]}"; do
  echo "=== Building PrepareProcedurally for Rimworld ${v} (${CONFIG}) ==="
  dotnet build "$SLN_PATH" -c "$CONFIG" /p:RimWorldVersion="$v"
done