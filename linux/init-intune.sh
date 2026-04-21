#!/usr/bin/env bash
set -euo pipefail

# Microsoft Intune enrollment setup for CachyOS/Arch + Hyprland
# Spoofs os-release so Intune thinks we're Ubuntu 24.04

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"

echo "=== Microsoft Intune Setup ==="
echo

# 1. Install AUR packages
AUR_HELPER=""
if command -v paru &>/dev/null; then
  AUR_HELPER="paru"
elif command -v yay &>/dev/null; then
  AUR_HELPER="yay"
else
  echo "ERROR: No AUR helper found (paru/yay)." >&2
  exit 1
fi

echo "[1/8] Installing Intune packages from AUR..."
"$AUR_HELPER" -S --needed --noconfirm intune-portal-bin microsoft-edge-stable-bin opensc

# 2. Spoof os-release
INTUNE_OS_RELEASE="/opt/microsoft/intune/share/os-release"
if [[ ! -f "$INTUNE_OS_RELEASE" ]]; then
  echo "ERROR: $INTUNE_OS_RELEASE not found. Is intune-portal-bin installed?" >&2
  exit 1
fi

echo
echo "[2/8] Spoofing /etc/os-release for Intune..."
if [[ -f /etc/os-release ]] && ! grep -q "^ID=ubuntu" /etc/os-release; then
  sudo cp /etc/os-release /etc/os-release.cachyos.bak
  echo "  Backed up original to /etc/os-release.cachyos.bak"
fi
sudo cp "$INTUNE_OS_RELEASE" /etc/os-release
echo "  /etc/os-release now identifies as Ubuntu 24.04"

# 3. Enable systemd services
echo
echo "[3/8] Enabling systemd services and pcscd..."
sudo systemctl enable --now intune-daemon.service
systemctl --user enable --now intune-agent.service intune-agent.timer

# gnome-keyring is needed for secret storage (already in deps.txt)
if systemctl --user list-unit-files gnome-keyring-daemon.socket &>/dev/null; then
  systemctl --user enable --now gnome-keyring-daemon.socket gnome-keyring-daemon.service
fi

# pcscd is needed for YubiKey PIV smart card auth
sudo systemctl enable --now pcscd.socket

# 4. Fix WebKitGTK rendering on NVIDIA + Wayland (GBM buffer error)
# The identity broker is D-Bus activated, so it needs the env var globally via environment.d
# This affects all WebKitGTK apps (intune-portal, identity-broker) — not Firefox/Edge/etc.
echo
echo "[4/8] Applying WebKit DMA-BUF fix for NVIDIA..."
ENV_DIR="$HOME/.config/environment.d"
ENV_FILE="$ENV_DIR/webkit-fix.conf"
mkdir -p "$ENV_DIR"
if [[ ! -f "$ENV_FILE" ]] || ! grep -q "WEBKIT_DISABLE_DMABUF_RENDERER" "$ENV_FILE"; then
  echo "WEBKIT_DISABLE_DMABUF_RENDERER=1" > "$ENV_FILE"
  echo "  Created $ENV_FILE"
else
  echo "  Already configured"
fi

# 5. Set up NSS database with OpenSC for YubiKey PIV smart card auth
echo
echo "[5/8] Setting up NSS database for smart card auth..."
if [[ ! -d "$HOME/.pki/nssdb" ]] || ! modutil -dbdir sql:"$HOME/.pki/nssdb" -list 2>/dev/null | grep -q 'SC Module'; then
  mkdir -p "$HOME/.pki/nssdb"
  chmod 700 "$HOME/.pki" "$HOME/.pki/nssdb"
  modutil -force -create -dbdir sql:"$HOME/.pki/nssdb" 2>/dev/null
  modutil -force -dbdir sql:"$HOME/.pki/nssdb" -add 'SC Module' -libfile /usr/lib/pkcs11/opensc-pkcs11.so 2>/dev/null
  echo "  NSS database configured with OpenSC module"
else
  echo "  Already configured"
fi

# 6. Configure password compliance for Intune (common-password + pwquality)
echo
echo "[6/8] Configuring password compliance..."
if [[ ! -f /etc/pam.d/common-password ]] || ! grep -q pam_pwquality /etc/pam.d/common-password; then
  echo "password    required    pam_pwquality.so    retry=3 minlen=12 dcredit=-1 ucredit=-1 lcredit=-1 ocredit=-1" | sudo tee /etc/pam.d/common-password > /dev/null
  echo "password    required    pam_unix.so    try_first_pass nullok shadow" | sudo tee -a /etc/pam.d/common-password > /dev/null
  echo "  Created /etc/pam.d/common-password"
else
  echo "  Already configured"
fi
if ! grep -q "^minlen" /etc/security/pwquality.conf 2>/dev/null; then
  echo "# Intune compliance requirements" | sudo tee -a /etc/security/pwquality.conf > /dev/null
  echo "minlen = 12" | sudo tee -a /etc/security/pwquality.conf > /dev/null
  echo "dcredit = -1" | sudo tee -a /etc/security/pwquality.conf > /dev/null
  echo "ucredit = -1" | sudo tee -a /etc/security/pwquality.conf > /dev/null
  echo "lcredit = -1" | sudo tee -a /etc/security/pwquality.conf > /dev/null
  echo "ocredit = -1" | sudo tee -a /etc/security/pwquality.conf > /dev/null
  echo "  Updated /etc/security/pwquality.conf"
fi

# 7. Create registration symlink
echo
echo "[7/8] Creating registration symlink..."
mkdir -p ~/.local/state/intune
mkdir -p ~/.config/intune
if [[ ! -L ~/.local/state/intune/registration.toml ]]; then
  ln -sf ~/.config/intune/registration.toml ~/.local/state/intune/registration.toml
  echo "  Symlinked registration.toml"
else
  echo "  Symlink already exists"
fi

# 8. Done
echo
echo "[8/8] Setup complete!"
echo
echo "Next steps:"
echo "  1. Reboot"
echo "  2. Open the Intune Portal app and sign in with your work account"
echo "  3. Complete enrollment and fix any compliance issues"
echo "  4. Sign into Microsoft Edge with your work account"
echo
echo "To restore original os-release later:"
echo "  sudo cp /etc/os-release.cachyos.bak /etc/os-release"
