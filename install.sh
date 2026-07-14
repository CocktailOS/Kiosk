#!/usr/bin/env bash
set -Eeuo pipefail

REPO="CocktailOS/Kiosk"
RUNTIME="linux-arm64"
SERVICE_NAME="cocktailos-kiosk"
SERVICE_USER="cocktailos"
INSTALL_ROOT="/opt/cocktailos-kiosk"
DATA_ROOT="/var/lib/cocktailos-kiosk"
PORT="5149"
KEEP_RELEASES="3"
TAG=""
INCLUDE_PRERELEASE="true"
MODE=""
LOW_PERFORMANCE=""
ORIGINAL_ARGS=("$@")

usage() {
  cat <<'USAGE'
Installiert oder aktualisiert CocktailOS Kiosk auf einem Raspberry Pi.

Aufruf:
  curl -fsSL https://raw.githubusercontent.com/CocktailOS/Kiosk/main/install.sh | sudo bash
  curl -fsSL https://raw.githubusercontent.com/CocktailOS/Kiosk/main/install.sh | sudo bash -s -- [Optionen]

Optionen:
  --headless          Nur die API im Netzwerk bereitstellen.
  --display           Cage-Kiosk auf dem angeschlossenen Display starten.
  --both              Cage-Kiosk starten und die API zusätzlich im Netzwerk bereitstellen.
  --low-performance   Reduziertes Leistungsprofil für --display oder --both.
  --tag TAG           Eine bestimmte Release-Version installieren.
  --stable            Vorabversionen beim automatischen Update ignorieren.
  -h, --help          Diese Hilfe anzeigen.

Ohne Modus-Option wird eine vorhandene Moduswahl beibehalten, ansonsten --display verwendet.
USAGE
}

log() { printf '[cocktailos-kiosk] %s\n' "$*"; }
fail() { printf '[cocktailos-kiosk] FEHLER: %s\n' "$*" >&2; exit 1; }

while [[ $# -gt 0 ]]; do
  case "$1" in
    --headless|--display|--both)
      [[ -z "$MODE" ]] || fail "Nur ein Betriebsmodus ist erlaubt."
      MODE="${1#--}"
      shift
      ;;
    --low-performance)
      LOW_PERFORMANCE="true"
      shift
      ;;
    --tag)
      TAG="${2:?Für --tag fehlt die Version.}"
      shift 2
      ;;
    --stable)
      INCLUDE_PRERELEASE="false"
      shift
      ;;
    -h|--help)
      usage
      exit 0
      ;;
    *) fail "Unbekannte Option: $1" ;;
  esac
done

if [[ "${EUID}" -ne 0 ]]; then
  if [[ "$0" != "bash" && "$0" != "sh" ]] && command -v sudo >/dev/null 2>&1; then
    exec sudo --preserve-env=GITHUB_TOKEN bash "$0" "${ORIGINAL_ARGS[@]}"
  fi
  fail "Bitte als root ausführen, zum Beispiel: curl -fsSL https://raw.githubusercontent.com/CocktailOS/Kiosk/main/install.sh | sudo bash"
fi

install_base_packages() {
  local missing=()
  for command_name in curl python3 tar sha256sum systemctl; do
    command -v "$command_name" >/dev/null 2>&1 || missing+=("$command_name")
  done

  [[ "${#missing[@]}" -eq 0 ]] && return
  command -v apt-get >/dev/null 2>&1 || fail "Fehlende Befehle: ${missing[*]}"
  log "Installiere Grundpakete: ${missing[*]}"
  apt-get update
  apt-get install -y ca-certificates curl python3 coreutils tar systemd
}

install_display_packages() {
  if command -v cage >/dev/null 2>&1 && command -v cog >/dev/null 2>&1; then
    return
  fi

  command -v apt-get >/dev/null 2>&1 || fail "Der Displaymodus benötigt Cage und Cog. Installiere beide manuell oder verwende --headless."
  log "Installiere Cage und Cog für den Wayland-Kiosk"
  apt-get update
  apt-get install -y cage cog
}

find_boot_config() {
  local candidate
  for candidate in /boot/firmware/config.txt /boot/config.txt; do
    if [[ -f "$candidate" ]]; then
      printf '%s\n' "$candidate"
      return
    fi
  done
  fail "Keine Raspberry-Pi-Boot-Konfiguration gefunden. Verwende auf anderen Systemen --headless."
}

set_boot_config_value() {
  local file="$1" key="$2" value="$3"
  local escaped_key="${key//./\\.}"
  sed -i -E "/^[[:space:]]*${escaped_key}[[:space:]]*=/d" "$file"
  printf '%s=%s\n' "$key" "$value" >> "$file"
}

configure_display() {
  local boot_config
  boot_config="$(find_boot_config)"
  log "Konfiguriere 1024x600-HDMI-Display in ${boot_config}"
  set_boot_config_value "$boot_config" "hdmi_force_hotplug" "1"
  set_boot_config_value "$boot_config" "max_usb_current" "1"
  set_boot_config_value "$boot_config" "hdmi_group" "2"
  set_boot_config_value "$boot_config" "hdmi_mode" "87"
  set_boot_config_value "$boot_config" "hdmi_cvt" "1024 600 60 6 0 0 0"
  set_boot_config_value "$boot_config" "hdmi_drive" "1"
  sed -i -E '/^[[:space:]]*dtoverlay[[:space:]]*=[[:space:]]*vc4-(fkms|kms)-v3d([,[:space:]]|$)/d' "$boot_config"
  printf '%s\n' 'dtoverlay=vc4-kms-v3d' >> "$boot_config"
}

github_api() {
  local url="$1"
  local headers=(-H "Accept: application/vnd.github+json" -H "X-GitHub-Api-Version: 2022-11-28")
  [[ -n "${GITHUB_TOKEN:-}" ]] && headers+=(-H "Authorization: Bearer ${GITHUB_TOKEN}")
  curl --fail --silent --show-error --location "${headers[@]}" "$url"
}

select_release() {
  local releases_json="$1"
  JSON_INPUT="$releases_json" python3 - "$TAG" "$INCLUDE_PRERELEASE" <<'PY'
import json
import os
import sys

requested_tag = sys.argv[1]
include_prerelease = sys.argv[2].lower() == "true"
releases = json.loads(os.environ["JSON_INPUT"])
if isinstance(releases, dict):
    releases = [releases]

for release in releases:
    if release.get("draft"):
        continue
    if requested_tag and release.get("tag_name") != requested_tag:
        continue
    if not include_prerelease and release.get("prerelease"):
        continue
    print(json.dumps(release))
    sys.exit(0)
sys.exit(1)
PY
}

select_asset_url() {
  local release_json="$1" suffix="$2"
  JSON_INPUT="$release_json" python3 - "$suffix" <<'PY'
import json
import os
import sys

suffix = sys.argv[1]
for asset in json.loads(os.environ["JSON_INPUT"]).get("assets", []):
    if asset.get("name", "").endswith(suffix):
        print(asset["browser_download_url"])
        sys.exit(0)
sys.exit(1)
PY
}

json_value() {
  JSON_INPUT="$1" python3 - "$2" <<'PY'
import json
import os
import sys
print(json.loads(os.environ["JSON_INPUT"]).get(sys.argv[1], ""))
PY
}

download_file() {
  local headers=()
  [[ -n "${GITHUB_TOKEN:-}" ]] && headers+=(-H "Authorization: Bearer ${GITHUB_TOKEN}")
  curl --fail --silent --show-error --location "${headers[@]}" --output "$2" "$1"
}

install_base_packages

ENV_FILE="/etc/default/${SERVICE_NAME}"
if [[ -z "$MODE" && -f "$ENV_FILE" ]]; then
  MODE="$(sed -n -E 's/^COCKTAILOS_MODE=(headless|display|both)$/\1/p' "$ENV_FILE" | tail -n 1)"
fi
MODE="${MODE:-display}"

if [[ "$MODE" == "headless" && "$LOW_PERFORMANCE" == "true" ]]; then
  fail "--low-performance ist nur mit --display oder --both zulässig."
fi
if [[ -z "$LOW_PERFORMANCE" && -f "$ENV_FILE" ]] && grep -qx 'COCKTAILOS_LOW_PERFORMANCE=true' "$ENV_FILE"; then
  LOW_PERFORMANCE="true"
fi
LOW_PERFORMANCE="${LOW_PERFORMANCE:-false}"
if [[ "$MODE" == "headless" ]]; then
  LOW_PERFORMANCE="false"
fi

if [[ "$MODE" == "display" || "$MODE" == "both" ]]; then
  install_display_packages
  configure_display
fi

TMP_DIR="$(mktemp -d)"
trap 'rm -rf "$TMP_DIR"' EXIT
API_ROOT="https://api.github.com/repos/${REPO}"

if [[ -n "$TAG" ]]; then
  RELEASES_JSON="$(github_api "${API_ROOT}/releases/tags/${TAG}")"
else
  RELEASES_JSON="$(github_api "${API_ROOT}/releases?per_page=50")"
fi
RELEASE_JSON="$(select_release "$RELEASES_JSON")" || fail "Keine passende GitHub-Release gefunden."
RELEASE_TAG="$(json_value "$RELEASE_JSON" tag_name)"
ASSET_SUFFIX="-${RUNTIME}.tar.gz"
ARCHIVE_URL="$(select_asset_url "$RELEASE_JSON" "$ASSET_SUFFIX")" || fail "Für ${RELEASE_TAG} fehlt das ${RUNTIME}-Archiv."
CHECKSUM_URL="$(select_asset_url "$RELEASE_JSON" "${ASSET_SUFFIX}.sha256")" || fail "Für ${RELEASE_TAG} fehlt die Prüfsumme."
ARCHIVE_PATH="${TMP_DIR}/cocktailos-kiosk.tar.gz"
CHECKSUM_PATH="${TMP_DIR}/cocktailos-kiosk.tar.gz.sha256"

log "Lade ${RELEASE_TAG} für ${RUNTIME}"
download_file "$ARCHIVE_URL" "$ARCHIVE_PATH"
download_file "$CHECKSUM_URL" "$CHECKSUM_PATH"
EXPECTED_HASH="$(awk '{print $1}' "$CHECKSUM_PATH")"
ACTUAL_HASH="$(sha256sum "$ARCHIVE_PATH" | awk '{print $1}')"
[[ "$EXPECTED_HASH" == "$ACTUAL_HASH" ]] || fail "Prüfsummenprüfung fehlgeschlagen."

RELEASE_DIR="${INSTALL_ROOT}/releases/${RELEASE_TAG}"
CURRENT_LINK="${INSTALL_ROOT}/current"
SHARED_DATA_DIR="${DATA_ROOT}/data"
SHARED_UPLOADS_DIR="${DATA_ROOT}/uploads"
install -d -m 0755 "$INSTALL_ROOT" "${INSTALL_ROOT}/releases" "$DATA_ROOT"
install -d -m 0750 "$SHARED_DATA_DIR" "$SHARED_UPLOADS_DIR"
rm -rf "$RELEASE_DIR"
install -d -m 0755 "$RELEASE_DIR"
tar -xzf "$ARCHIVE_PATH" -C "$RELEASE_DIR"
chmod +x "${RELEASE_DIR}/CocktailOS.Kiosk"
rm -rf "${RELEASE_DIR}/data" "${RELEASE_DIR}/wwwroot/uploads"
ln -s "$SHARED_DATA_DIR" "${RELEASE_DIR}/data"
install -d -m 0755 "${RELEASE_DIR}/wwwroot"
ln -s "$SHARED_UPLOADS_DIR" "${RELEASE_DIR}/wwwroot/uploads"

if ! id "$SERVICE_USER" >/dev/null 2>&1; then
  useradd --system --home-dir "$DATA_ROOT" --shell /usr/sbin/nologin "$SERVICE_USER"
fi
getent group gpio >/dev/null 2>&1 && usermod -aG gpio "$SERVICE_USER"
SERVICE_GROUP="$(id -gn "$SERVICE_USER")"
chown -R root:root "$INSTALL_ROOT"
chown -R "${SERVICE_USER}:${SERVICE_GROUP}" "$DATA_ROOT"
ln -sfn "$RELEASE_DIR" "$CURRENT_LINK"

if [[ "$MODE" == "headless" || "$MODE" == "both" ]]; then
  APP_URL="http://0.0.0.0:${PORT}"
else
  APP_URL="http://127.0.0.1:${PORT}"
fi

cat > "$ENV_FILE" <<ENVIRONMENT
# Managed by CocktailOS Kiosk install.sh.
COCKTAILOS_MODE=${MODE}
COCKTAILOS_LOW_PERFORMANCE=${LOW_PERFORMANCE}
ENVIRONMENT
if [[ "$LOW_PERFORMANCE" == "true" ]]; then
  cat >> "$ENV_FILE" <<'ENVIRONMENT'
DOTNET_GCServer=0
DOTNET_GCConserveMemory=9
ENVIRONMENT
fi
chmod 0644 "$ENV_FILE"

cat > "/etc/systemd/system/${SERVICE_NAME}.service" <<SERVICE
[Unit]
Description=CocktailOS Kiosk API
After=network-online.target
Wants=network-online.target

[Service]
Type=simple
User=${SERVICE_USER}
WorkingDirectory=${CURRENT_LINK}
ExecStart=${CURRENT_LINK}/CocktailOS.Kiosk
Restart=always
RestartSec=5
EnvironmentFile=-${ENV_FILE}
Environment=ASPNETCORE_ENVIRONMENT=Production
Environment=ASPNETCORE_URLS=${APP_URL}

[Install]
WantedBy=multi-user.target
SERVICE

KIOSK_SERVICE_NAME="${SERVICE_NAME}-cage"
KIOSK_SERVICE_FILE="/etc/systemd/system/${KIOSK_SERVICE_NAME}.service"
KIOSK_SCRIPT="/usr/local/lib/cocktailos-kiosk/cage-session"
if [[ "$MODE" == "display" || "$MODE" == "both" ]]; then
  install -d -m 0755 /usr/local/lib/cocktailos-kiosk
  install -d -m 0700 -o "$SERVICE_USER" -g "$SERVICE_GROUP" "${DATA_ROOT}/runtime" "${DATA_ROOT}/cog"
  cat > "$KIOSK_SCRIPT" <<KIOSK
#!/usr/bin/env bash
set -Eeuo pipefail
export HOME="${DATA_ROOT}/cog"
export XDG_CONFIG_HOME="\${HOME}/.config"
export XDG_CACHE_HOME="\${HOME}/.cache"
export XDG_RUNTIME_DIR="${DATA_ROOT}/runtime"
mkdir -p "\${XDG_CONFIG_HOME}" "\${XDG_CACHE_HOME}" "\${XDG_RUNTIME_DIR}"
chmod 0700 "\${XDG_RUNTIME_DIR}"

exec /usr/bin/cage -s -- /usr/bin/cog --fullscreen --platform=wayland --web-process-count=1 "http://127.0.0.1:${PORT}"
KIOSK
  chmod 0755 "$KIOSK_SCRIPT"
  chown root:root "$KIOSK_SCRIPT"
  cat > "$KIOSK_SERVICE_FILE" <<KIOSK_SERVICE
[Unit]
Description=CocktailOS Kiosk on Cage
After=${SERVICE_NAME}.service systemd-user-sessions.service
Wants=${SERVICE_NAME}.service
Conflicts=display-manager.service getty@tty1.service

[Service]
Type=simple
User=${SERVICE_USER}
WorkingDirectory=${DATA_ROOT}/cog
EnvironmentFile=-${ENV_FILE}
PAMName=login
SupplementaryGroups=video render input tty
TTYPath=/dev/tty1
StandardInput=tty
TTYReset=yes
TTYVHangup=yes
TTYVTDisallocate=yes
ExecStart=${KIOSK_SCRIPT}
Restart=on-failure
RestartSec=3

[Install]
WantedBy=multi-user.target
KIOSK_SERVICE
else
  rm -f "$KIOSK_SERVICE_FILE"
fi

log "Aktiviere Dienste (${MODE})"
systemctl daemon-reload
systemctl enable "$SERVICE_NAME"
systemctl restart "$SERVICE_NAME"
if [[ "$MODE" == "display" || "$MODE" == "both" ]]; then
  systemctl disable --now display-manager.service 2>/dev/null || true
  systemctl enable "$KIOSK_SERVICE_NAME"
  systemctl restart "$KIOSK_SERVICE_NAME"
else
  systemctl disable --now "$KIOSK_SERVICE_NAME" 2>/dev/null || true
fi

find "${INSTALL_ROOT}/releases" -mindepth 1 -maxdepth 1 -type d -printf '%T@ %p\n' \
  | sort -rn | tail -n +"$((KEEP_RELEASES + 1))" | cut -d' ' -f2- \
  | while IFS= read -r old_release; do
      [[ -z "$old_release" || "$old_release" == "$RELEASE_DIR" ]] || rm -rf "$old_release"
    done

log "${RELEASE_TAG} ist installiert."
if [[ "$MODE" == "headless" || "$MODE" == "both" ]]; then
  log "Netzwerk-URL: http://$(hostname -I | awk '{print $1}'):${PORT}"
fi
if [[ "$MODE" == "display" || "$MODE" == "both" ]]; then
  log "Cage startet die Oberfläche lokal auf dem HDMI-Display. Die HDMI-Änderung wird nach einem Neustart vollständig übernommen."
fi
