#!/usr/bin/env bash
set -euo pipefail

# Configuration
PROJECT_ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
STEAM_USER=""
INSTALLS_DIR="$HOME/SteamCMD/Rimworld"
RW_APP_ID="294100"
HAR_MOD_ID="839005762" # Humanoid Alien Races

# Helper function
usage() {
  echo "Usage: $0 --steam-user <name> [--installs-dir <path>]"
  echo ""
  echo "Options:"
  echo "  -u, --steam-user    Your Steam username (required, will likely need Steam Guard authentication)"
  echo "  -d, --installs-dir  Directory for Rimworld installs (default: $HOME/SteamCMD/Rimworld)"
  echo "  -h, --help          Show this help message"
  exit 1
}

# Parse arguments
while [[ $# -gt 0 ]]; do
  case $1 in
    -u|--steam-user)
      STEAM_USER="$2"
      shift 2
      ;;
    -d|--installs-dir)
      # Expand ~ to $HOME
      INSTALLS_DIR="${2/#\~/$HOME}"
      shift 2
      ;;
    -h|--help)
      usage
      ;;
    *)
      echo "Unknown argument: $1"
      usage
      ;;
  esac
done

# Validation
if [[ -z "${STEAM_USER}" ]]; then
  echo "Error: --steam-user is required."
  usage
fi

# Each version supported by Prepare Procedurally.
VERSIONS=( "1.4" "1.5" "1.6" )
# BETA version labels of Rimworld to download.
BETAS=( "version-1.4.3901" "version-1.5" "public" )
# HumanoidAlienRace versions versioned by which assembly to look at for PrepareProcedurally builds.
ALIEN_RACE_COMPAT=( "1.4" "1.4" "1.6" )

echo "======Installing Humanoid Alien Races (HAR) for Prepare Procedurally compatibility build======"
MODS_INSTALL_DIRECTORY="${INSTALLS_DIR}/Mods"
mkdir -p "${MODS_INSTALL_DIRECTORY}"
steamcmd +force_install_dir "${MODS_INSTALL_DIRECTORY}" \
         +login "${STEAM_USER}" \
         +workshop_download_item "${RW_APP_ID}" "${HAR_MOD_ID}" validate \
         +quit

HAR_MOD_SOURCE="${MODS_INSTALL_DIRECTORY}/steamapps/workshop/content/${RW_APP_ID}/${HAR_MOD_ID}"

echo "======Removing Project's Steam Dependencies Directory======"
STEAM_DEP_DIR="${PROJECT_ROOT}/Steam"
if [ -d "${STEAM_DEP_DIR}" ]; then
  rm -rf "${STEAM_DEP_DIR}"
fi

for i in "${!VERSIONS[@]}"; do
  VERSION="${VERSIONS[$i]}"
  BETA="${BETAS[$i]}"
  ALIEN_VER="${ALIEN_RACE_COMPAT[$i]}"
  VERSION_DIR="${INSTALLS_DIR}/${VERSION}"
  
  echo "======Installing Rimworld v${VERSION} (BETA: ${BETA})======"
  steamcmd +force_install_dir "${VERSION_DIR}" \
           +login ${STEAM_USER} \
           +app_update "${RW_APP_ID}" -beta "${BETA}" validate \
           +quit
  
  echo "======Symlinking PrepareProcedurally's Release so the install can use it as a local mod======"
  LOCAL_MOD_DESTINATION="${VERSION_DIR}/Mods/PrepareProcedurally"
  ln -snf "${PROJECT_ROOT}/Release" "${LOCAL_MOD_DESTINATION}"
  
  echo "=====Adding a local launch script to test without messing with the main steam install on your host OS====="
  RUN_SCRIPT="${INSTALLS_DIR}/run_rimworld_${VERSION}.sh"
  cat << EOF > "$RUN_SCRIPT"
#!/bin/bash
# Automatically generated portable launcher for RimWorld $VERSION
SCRIPT_DIR=\$(dirname "\$(readlink -f "\$0")")
VERSIONED_DIR="\$SCRIPT_DIR/$VERSION/"
cd "\$VERSIONED_DIR"

PROFILE_DIR="\$VERSIONED_DIR/LocalSave"
mkdir -p "\$PROFILE_DIR"

# Ensure the specific version binary is executable
if [ ! -x "./RimWorldLinux" ]; then
    chmod +x "./RimWorldLinux"
fi

LOG="-logfile \$PROFILE_DIR/rimworld_log.txt"
# VERSION is written directly here (e.g. 1.4), others are escaped
LC_ALL=C ./RimWorldLinux -savedatafolder="\$PROFILE_DIR" \$LOG "\$@"
EOF

  # Make the newly created script executable
  chmod +x "$RUN_SCRIPT"
  echo "Launcher created at $RUN_SCRIPT"
  
  echo "======Copying Rimworld v${VERSION} assemblies for PrepareProcedurally v${VERSION} build======"
  DEPENDENCY_DIR="${PROJECT_ROOT}/Steam/${VERSION}"
  SOURCE_ASSEMBLIES="${VERSION_DIR}/RimWorldLinux_Data/Managed"
  DESTINATION="${DEPENDENCY_DIR}/Game"
  mkdir -p "${DESTINATION}"
  cp -r "${SOURCE_ASSEMBLIES}"/. "${DESTINATION}"
  
  echo "======Copying AlienRace v${ALIEN_VER} assemblies for PrepareProcedurally v${VERSION} build======"
  SOURCE_ASSEMBLIES="${HAR_MOD_SOURCE}/${ALIEN_VER}/Assemblies"
  DESTINATION="${DEPENDENCY_DIR}/AlienRace"
  mkdir -p "${DESTINATION}"
  cp -r "${SOURCE_ASSEMBLIES}"/. "${DESTINATION}"
done

echo ""
echo "Installation(s) complete."
echo "For every version installed, there is a run_rimworld_XX.sh at the install dir to keep local saves/options."
echo "If you use SELinux-enabled distro (Bazzite) and want to use the devcontainer, see .devcontainer/README.md"
