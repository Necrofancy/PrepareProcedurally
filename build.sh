#!/usr/bin/env bash
set -euo pipefail

PROJECT_ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
SLN_PATH="${PROJECT_ROOT}/Necrofancy.PrepareProcedurally.sln"
CONFIG="Release"
VERSIONS=( "1.4" "1.5" "1.6" "1.7" )

for v in "${VERSIONS[@]}"; do
  echo "=== Building PrepareProcedurally for Rimworld ${v} (${CONFIG}) ==="
  dotnet build "$SLN_PATH" -c "$CONFIG" /p:RimworldVersion="$v"
done

echo "=== Cleaning PDB files for mod release ==="
find "${PROJECT_ROOT}/Release" -name "*.pdb" -type f -delete

echo "=== Pushing Version to Release Folder ==="

MANIFEST="${PROJECT_ROOT}/Release/About/Manifest.xml"
MOD_VERSION="$(dotnet msbuild "${PROJECT_ROOT}/src/Necrofancy.PrepareProcedurally/Necrofancy.PrepareProcedurally.csproj" \
  -getProperty:Version -p:Configuration="$CONFIG")"
if [[ ! "$MOD_VERSION" =~ ^[0-9]+(\.[0-9]+){1,3}$ ]]; then
  echo "Could not read <Version> from MSBuild (got: '${MOD_VERSION}')" >&2
  exit 1
fi

echo "=== Stamping Manifest.xml with version ${MOD_VERSION} ==="
sed -i.bak -E "s|<version>[^<]*</version>|<version>${MOD_VERSION}</version>|" "$MANIFEST" && rm "${MANIFEST}.bak"
grep -q "<version>${MOD_VERSION}</version>" "$MANIFEST" || { echo "Failed to update ${MANIFEST}" >&2; exit 1; }