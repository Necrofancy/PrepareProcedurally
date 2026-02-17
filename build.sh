#!/usr/bin/env bash
set -euo pipefail

PROJECT_ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
SLN_PATH="${PROJECT_ROOT}/Necrofancy.PrepareProcedurally.sln"
CONFIG="Release"
VERSIONS=( "1.4" "1.5" "1.6" )

for v in "${VERSIONS[@]}"; do
  echo "=== Building PrepareProcedurally for Rimworld ${v} (${CONFIG}) ==="
  dotnet build "$SLN_PATH" -c "$CONFIG" /p:RimworldVersion="$v"
done

echo "=== Cleaning PDB files for mod release ==="
find "${PROJECT_ROOT}/Release" -name "*.pdb" -type f -delete