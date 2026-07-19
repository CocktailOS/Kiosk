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
NETWORK_PIN=""
EXISTING_NETWORK_ACCESS_PIN_HASH=""
ORIGINAL_ARGS=("$@")

usage() {
  cat <<'USAGE'
Installiert oder aktualisiert CocktailOS Kiosk auf einem Raspberry Pi.

Aufruf:
  curl -fsSL https://raw.githubusercontent.com/CocktailOS/Kiosk/main/install.sh | sudo bash
  curl -fsSL https://raw.githubusercontent.com/CocktailOS/Kiosk/main/install.sh | sudo bash -s -- [Optionen]

Optionen:
  --headless          Ohne angeschlossenes Display installieren und den Netzwerkzugriff aktivieren.
  --network-pin PIN   Vierstelligen PIN für den Netzwerkzugriff setzen (erforderlich mit --headless).
  --tag TAG           Eine bestimmte Release-Version installieren.
  --stable            Vorabversionen beim automatischen Update ignorieren.
  -h, --help          Diese Hilfe anzeigen.

Ohne Option startet CocktailOS auf dem angeschlossenen Display. Netzwerkzugriff wird später direkt in den Systemeinstellungen aktiviert.
USAGE
}

log() { printf '[cocktailos-kiosk] %s\n' "$*"; }
fail() { printf '[cocktailos-kiosk] FEHLER: %s\n' "$*" >&2; exit 1; }

while [[ $# -gt 0 ]]; do
  case "$1" in
    --headless)
      [[ -z "$MODE" ]] || fail "Nur ein Betriebsmodus ist erlaubt."
      MODE="${1#--}"
      shift
      ;;
    --network-pin)
      NETWORK_PIN="${2:?Für --network-pin fehlt der PIN.}"
      shift 2
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
  for command_name in curl python3 tar sha256sum systemctl sudo; do
    command -v "$command_name" >/dev/null 2>&1 || missing+=("$command_name")
  done

  [[ "${#missing[@]}" -eq 0 ]] && return
  command -v apt-get >/dev/null 2>&1 || fail "Fehlende Befehle: ${missing[*]}"
  log "Installiere Grundpakete: ${missing[*]}"
  apt-get update
  apt-get install -y ca-certificates curl python3 coreutils tar systemd sudo
}

install_display_packages() {
  if { command -v chromium >/dev/null 2>&1 || command -v chromium-browser >/dev/null 2>&1; } \
    && command -v cage >/dev/null 2>&1; then
    return
  fi

  command -v apt-get >/dev/null 2>&1 || fail "Der Displaymodus benötigt Cage und Chromium. Installiere diese manuell oder verwende --headless."
  log "Installiere Cage und Chromium für den Wayland-Kiosk"
  apt-get update
  if apt-cache show chromium >/dev/null 2>&1; then
    apt-get install -y cage chromium
  else
    apt-get install -y cage chromium-browser
  fi
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
  printf '%s\n' 'dtoverlay=vc4-fkms-v3d' >> "$boot_config"
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
if [[ -f "$ENV_FILE" ]]; then
  if [[ -z "$MODE" ]]; then
    MODE="$(sed -n -E 's/^COCKTAILOS_MODE=headless$/headless/p' "$ENV_FILE" | tail -n 1)"
  fi
  EXISTING_NETWORK_ACCESS_PIN_HASH="$(sed -n -E 's/^COCKTAILOS_NETWORK_ACCESS_PIN_HASH=(.+)$/\1/p' "$ENV_FILE" | tail -n 1)"
fi
MODE="${MODE:-display}"

if [[ "$MODE" == "headless" ]]; then
  if [[ -n "$NETWORK_PIN" ]]; then
    [[ "$NETWORK_PIN" =~ ^[0-9]{4}$ ]] || fail "Der Netzwerk-PIN muss aus genau vier Ziffern bestehen."
  elif [[ -z "$EXISTING_NETWORK_ACCESS_PIN_HASH" ]]; then
    fail "Für --headless ist ein vierstelliger PIN über --network-pin erforderlich."
  fi
elif [[ -n "$NETWORK_PIN" ]]; then
  fail "--network-pin kann nur zusammen mit --headless verwendet werden."
fi

if [[ "$MODE" == "display" ]]; then
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

APP_URL="http://0.0.0.0:${PORT}"
NETWORK_ACCESS_DEFAULT="false"
NETWORK_ACCESS_PIN_HASH="$EXISTING_NETWORK_ACCESS_PIN_HASH"
if [[ "$MODE" == "headless" ]]; then
  NETWORK_ACCESS_DEFAULT="true"
  if [[ -n "$NETWORK_PIN" ]]; then
    NETWORK_ACCESS_PIN_HASH="$(NETWORK_PIN="$NETWORK_PIN" python3 - <<'PY'
import base64
import hashlib
import os

pin = os.environ["NETWORK_PIN"].encode("ascii")
salt = os.urandom(16)
digest = hashlib.pbkdf2_hmac("sha256", pin, salt, 120_000, 32)
print(f"v1.120000.{base64.b64encode(salt).decode()}.{base64.b64encode(digest).decode()}")
PY
)"
  fi
fi

cat > "$ENV_FILE" <<ENVIRONMENT
# Managed by CocktailOS Kiosk install.sh.
COCKTAILOS_MODE=${MODE}
COCKTAILOS_NETWORK_ACCESS_DEFAULT=${NETWORK_ACCESS_DEFAULT}
COCKTAILOS_NETWORK_ACCESS_PIN_HASH=${NETWORK_ACCESS_PIN_HASH}
ENVIRONMENT
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

UPDATE_SERVICE_NAME="${SERVICE_NAME}-update"
UPDATE_SCRIPT="/usr/local/lib/cocktailos-kiosk/update"
UPDATE_SUDOERS_FILE="/etc/sudoers.d/${SERVICE_NAME}-update"
install -d -m 0755 /usr/local/lib/cocktailos-kiosk
cat > "$UPDATE_SCRIPT" <<UPDATE_SCRIPT
#!/usr/bin/env bash
set -Eeuo pipefail
/usr/bin/curl --fail --silent --show-error --location "https://raw.githubusercontent.com/${REPO}/main/install.sh" | /usr/bin/bash
UPDATE_SCRIPT
chmod 0755 "$UPDATE_SCRIPT"
chown root:root "$UPDATE_SCRIPT"
cat > "/etc/systemd/system/${UPDATE_SERVICE_NAME}.service" <<UPDATE_SERVICE
[Unit]
Description=CocktailOS Kiosk update
After=network-online.target
Wants=network-online.target

[Service]
Type=oneshot
ExecStart=${UPDATE_SCRIPT}
UPDATE_SERVICE
cat > "$UPDATE_SUDOERS_FILE" <<SUDOERS
${SERVICE_USER} ALL=(root) NOPASSWD: /usr/bin/systemctl start --no-block ${UPDATE_SERVICE_NAME}.service
SUDOERS
chmod 0440 "$UPDATE_SUDOERS_FILE"
chown root:root "$UPDATE_SUDOERS_FILE"

PIN_CHANGE_SCRIPT="/usr/local/sbin/cocktailos-change-pin"
cat > "$PIN_CHANGE_SCRIPT" <<PIN_CHANGE_SCRIPT
#!/usr/bin/env bash
set -Eeuo pipefail

SERVICE_NAME="${SERVICE_NAME}"
KIOSK_SERVICE_NAME="${SERVICE_NAME}-cage"
DATABASE="${SHARED_DATA_DIR}/cocktailos.db"
ENV_FILE="${ENV_FILE}"

if [[ "\${EUID}" -ne 0 ]]; then
  echo "Bitte mit sudo ausführen: sudo cocktailos-change-pin" >&2
  exit 1
fi

[[ -f "\${DATABASE}" ]] || { echo "CocktailOS-Datenbank nicht gefunden: \${DATABASE}" >&2; exit 1; }

read -r -s -p "Neuen vierstelligen App-PIN eingeben: " pin
echo
read -r -s -p "App-PIN wiederholen: " confirmation
echo

[[ "\${pin}" =~ ^[0-9]{4}$ ]] || { echo "Der PIN muss aus genau vier Ziffern bestehen." >&2; exit 1; }
[[ "\${pin}" == "\${confirmation}" ]] || { echo "Die PINs stimmen nicht überein." >&2; exit 1; }

kiosk_was_active=false
if systemctl is-active --quiet "\${KIOSK_SERVICE_NAME}"; then
  kiosk_was_active=true
  systemctl stop "\${KIOSK_SERVICE_NAME}"
fi
was_active=false
if systemctl is-active --quiet "\${SERVICE_NAME}"; then
  was_active=true
  systemctl stop "\${SERVICE_NAME}"
fi

restart_service() {
  if [[ "\${was_active}" == true ]]; then
    systemctl start "\${SERVICE_NAME}"
  fi
  if [[ "\${kiosk_was_active}" == true ]]; then
    systemctl start "\${KIOSK_SERVICE_NAME}"
  fi
}
trap restart_service EXIT

PIN="\${pin}" DATABASE="\${DATABASE}" ENV_FILE="\${ENV_FILE}" python3 - <<'PY'
import base64
import hashlib
import os
import sqlite3

pin = os.environ["PIN"].encode("ascii")
salt = os.urandom(16)
digest = hashlib.pbkdf2_hmac("sha256", pin, salt, 120_000, 32)
pin_hash = f"v1.120000.{base64.b64encode(salt).decode()}.{base64.b64encode(digest).decode()}"

database = os.environ["DATABASE"]
with sqlite3.connect(database) as connection:
    cursor = connection.execute(
        "UPDATE MachineConfigurations SET NetworkAccessPinHash = ? WHERE Id = 1", (pin_hash,))
    if cursor.rowcount != 1:
        raise SystemExit("Die CocktailOS-Konfiguration konnte nicht gefunden werden.")

env_file = os.environ["ENV_FILE"]
lines = []
if os.path.exists(env_file):
    with open(env_file, encoding="utf-8") as handle:
        lines = [line.rstrip("\\n") for line in handle]

key = "COCKTAILOS_NETWORK_ACCESS_PIN_HASH="
updated = False
for index, line in enumerate(lines):
    if line.startswith(key):
        lines[index] = key + pin_hash
        updated = True
        break
if not updated:
    lines.append(key + pin_hash)

temporary = env_file + ".tmp"
with open(temporary, "w", encoding="utf-8") as handle:
    handle.write("\\n".join(lines) + "\\n")
os.chmod(temporary, 0o644)
os.replace(temporary, env_file)
PY

echo "App-PIN wurde geändert. Der neue PIN gilt auch für den Netzwerkzugriff."
PIN_CHANGE_SCRIPT
chmod 0755 "$PIN_CHANGE_SCRIPT"
chown root:root "$PIN_CHANGE_SCRIPT"

PIN_RESET_SCRIPT="/usr/local/sbin/cocktailos-reset-pin"
cat > "$PIN_RESET_SCRIPT" <<PIN_RESET_SCRIPT
#!/usr/bin/env bash
set -Eeuo pipefail

SERVICE_NAME="${SERVICE_NAME}"
KIOSK_SERVICE_NAME="${SERVICE_NAME}-cage"
DATABASE="${SHARED_DATA_DIR}/cocktailos.db"
ENV_FILE="${ENV_FILE}"

if [[ "\${EUID}" -ne 0 ]]; then
  echo "Bitte mit sudo ausführen: sudo cocktailos-reset-pin" >&2
  exit 1
fi

[[ -f "\${DATABASE}" ]] || { echo "CocktailOS-Datenbank nicht gefunden: \${DATABASE}" >&2; exit 1; }
read -r -p "PIN wirklich löschen? Zum Bestätigen LOESCHEN eingeben: " confirmation
[[ "\${confirmation}" == "LOESCHEN" ]] || { echo "Abgebrochen."; exit 0; }

kiosk_was_active=false
if systemctl is-active --quiet "\${KIOSK_SERVICE_NAME}"; then
  kiosk_was_active=true
  systemctl stop "\${KIOSK_SERVICE_NAME}"
fi
was_active=false
if systemctl is-active --quiet "\${SERVICE_NAME}"; then
  was_active=true
  systemctl stop "\${SERVICE_NAME}"
fi

restart_service() {
  if [[ "\${was_active}" == true ]]; then
    systemctl start "\${SERVICE_NAME}"
  fi
  if [[ "\${kiosk_was_active}" == true ]]; then
    systemctl start "\${KIOSK_SERVICE_NAME}"
  fi
}
trap restart_service EXIT

DATABASE="\${DATABASE}" ENV_FILE="\${ENV_FILE}" python3 - <<'PY'
import os
import sqlite3

database = os.environ["DATABASE"]
with sqlite3.connect(database) as connection:
    cursor = connection.execute(
        "UPDATE MachineConfigurations SET NetworkAccessPinHash = NULL, NetworkAccessEnabled = 0, IntroTourCompleted = 1 WHERE Id = 1")
    if cursor.rowcount != 1:
        raise SystemExit("Die CocktailOS-Konfiguration konnte nicht gefunden werden.")

env_file = os.environ["ENV_FILE"]
lines = []
if os.path.exists(env_file):
    with open(env_file, encoding="utf-8") as handle:
        lines = [line.rstrip("\\n") for line in handle]

updates = {
    "COCKTAILOS_NETWORK_ACCESS_PIN_HASH=": "COCKTAILOS_NETWORK_ACCESS_PIN_HASH=",
    "COCKTAILOS_NETWORK_ACCESS_DEFAULT=": "COCKTAILOS_NETWORK_ACCESS_DEFAULT=false",
}
for key, value in updates.items():
    for index, line in enumerate(lines):
        if line.startswith(key):
            lines[index] = value
            break
    else:
        lines.append(value)

temporary = env_file + ".tmp"
with open(temporary, "w", encoding="utf-8") as handle:
    handle.write("\\n".join(lines) + "\\n")
os.chmod(temporary, 0o644)
os.replace(temporary, env_file)
PY

echo "App-PIN wurde gelöscht. Vergib ihn jetzt direkt in der CocktailOS-Oberfläche neu."
echo "Der Netzwerkzugriff wurde zur Sicherheit deaktiviert und kann danach in den Einstellungen wieder aktiviert werden."
PIN_RESET_SCRIPT
chmod 0755 "$PIN_RESET_SCRIPT"
chown root:root "$PIN_RESET_SCRIPT"

KIOSK_SERVICE_NAME="${SERVICE_NAME}-cage"
KIOSK_SERVICE_FILE="/etc/systemd/system/${KIOSK_SERVICE_NAME}.service"
KIOSK_SCRIPT="/usr/local/lib/cocktailos-kiosk/cage-session"
LEGACY_DISPLAY_SERVICE_NAME="${SERVICE_NAME}-display"
LEGACY_DISPLAY_SERVICE_FILE="/etc/systemd/system/${LEGACY_DISPLAY_SERVICE_NAME}.service"
if [[ "$MODE" == "display" ]]; then
  install -d -m 0755 /usr/local/lib/cocktailos-kiosk
  install -d -m 0700 -o "$SERVICE_USER" -g "$SERVICE_GROUP" "${DATA_ROOT}/runtime" "${DATA_ROOT}/browser"
  install -d -m 0755 /etc/chromium/policies/managed
  cat > /etc/chromium/policies/managed/cocktailos-kiosk.json <<'CHROMIUM_POLICY'
{
  "TranslateEnabled": false
}
CHROMIUM_POLICY
  chmod 0644 /etc/chromium/policies/managed/cocktailos-kiosk.json
  chown root:root /etc/chromium/policies/managed/cocktailos-kiosk.json
  if [[ -f /etc/X11/Xwrapper.config ]] && grep -q 'Managed by CocktailOS Kiosk install.sh.' /etc/X11/Xwrapper.config; then
    rm -f /etc/X11/Xwrapper.config
  fi
  cat > "$KIOSK_SCRIPT" <<KIOSK
#!/usr/bin/env bash
set -Eeuo pipefail
export HOME="${DATA_ROOT}/browser"
export XDG_CONFIG_HOME="\${HOME}/.config"
export XDG_CACHE_HOME="\${HOME}/.cache"
export XDG_RUNTIME_DIR="${DATA_ROOT}/runtime"
mkdir -p "\${XDG_CONFIG_HOME}" "\${XDG_CACHE_HOME}" "\${XDG_RUNTIME_DIR}"
chmod 0700 "\${XDG_RUNTIME_DIR}"
mkdir -p "\${HOME}/kiosk-profile/Default"
cat > "\${HOME}/kiosk-profile/Default/Preferences" <<'PREFERENCES'
{
  "translate": {
    "enabled": false
  },
  "intl": {
    "accept_languages": "de-DE,de"
  }
}
PREFERENCES

if command -v chromium >/dev/null 2>&1; then
  chromium_bin="\$(command -v chromium)"
elif command -v chromium-browser >/dev/null 2>&1; then
  chromium_bin="\$(command -v chromium-browser)"
else
  echo "Chromium wurde nicht gefunden." >&2
  exit 1
fi

args=(
  --kiosk
  --no-first-run
  --no-default-browser-check
  --disable-session-crashed-bubble
  --disable-infobars
  --user-data-dir="\${HOME}/kiosk-profile"
  --password-store=basic
  --ozone-platform=wayland
  --enable-features=UseOzonePlatform
  --use-gl=egl
  --enable-gpu-rasterization
  --ignore-gpu-blocklist
  --disable-translate
  --disable-features=Translate,TranslateUI,MediaRouter,OptimizationHints,OverscrollHistoryNavigation
  --disable-pinch
  --overscroll-history-navigation=0
)

for ((attempt = 1; attempt <= 60; attempt++)); do
  if /usr/bin/curl --fail --silent --show-error --connect-timeout 1 --max-time 2 --output /dev/null "http://127.0.0.1:${PORT}/"; then
    break
  fi

  if (( attempt == 60 )); then
    echo "CocktailOS API ist nach 60 Sekunden noch nicht erreichbar." >&2
    exit 1
  fi

  sleep 1
done

exec /usr/bin/cage -- "\${chromium_bin}" "\${args[@]}" "http://127.0.0.1:${PORT}"
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
WorkingDirectory=${DATA_ROOT}/browser
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
rm -f "$LEGACY_DISPLAY_SERVICE_FILE"

log "Aktiviere Dienste (${MODE})"
systemctl daemon-reload
systemctl enable "$SERVICE_NAME"
systemctl restart "$SERVICE_NAME"
if [[ "$MODE" == "display" ]]; then
  systemctl disable --now display-manager.service 2>/dev/null || true
  systemctl stop "$LEGACY_DISPLAY_SERVICE_NAME" 2>/dev/null || true
  systemctl disable --now "$LEGACY_DISPLAY_SERVICE_NAME" 2>/dev/null || true
  systemctl enable "$KIOSK_SERVICE_NAME"
  systemctl restart "$KIOSK_SERVICE_NAME"
else
  systemctl disable --now "$KIOSK_SERVICE_NAME" 2>/dev/null || true
  systemctl stop "$LEGACY_DISPLAY_SERVICE_NAME" 2>/dev/null || true
  systemctl disable --now "$LEGACY_DISPLAY_SERVICE_NAME" 2>/dev/null || true
fi

find "${INSTALL_ROOT}/releases" -mindepth 1 -maxdepth 1 -type d -printf '%T@ %p\n' \
  | sort -rn | tail -n +"$((KEEP_RELEASES + 1))" | cut -d' ' -f2- \
  | while IFS= read -r old_release; do
      [[ -z "$old_release" || "$old_release" == "$RELEASE_DIR" ]] || rm -rf "$old_release"
    done

log "${RELEASE_TAG} ist installiert."
if [[ "$MODE" == "headless" ]]; then
  log "Netzwerk-URL: http://$(hostname -I | awk '{print $1}'):${PORT}"
fi
if [[ "$MODE" == "display" ]]; then
  log "Cage startet die Oberfläche lokal auf dem HDMI-Display. Die HDMI-Änderung wird nach einem Neustart vollständig übernommen."
fi
