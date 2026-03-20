#!/usr/bin/env bash
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd "$SCRIPT_DIR/.." && pwd)"
GIT_DIR="$REPO_ROOT/git"

echo "Setting up git and SSH for GitHub..."

# --- SSH key setup ---
mkdir -p "$HOME/.ssh"
chmod 700 "$HOME/.ssh"

# Copy public keys
cp "$GIT_DIR/yubikey-touch.pub" "$HOME/.ssh/yubikey-touch.pub"
cp "$GIT_DIR/yubikey-nfc.pub" "$HOME/.ssh/yubikey-nfc.pub"
chmod 644 "$HOME/.ssh/yubikey-touch.pub" "$HOME/.ssh/yubikey-nfc.pub"

# Install SSH config (back up existing if not a symlink)
if [[ -e "$HOME/.ssh/config" && ! -L "$HOME/.ssh/config" ]]; then
  BACKUP="$HOME/.ssh/config.backup.$(date +%Y%m%d_%H%M%S)"
  echo "Backing up existing SSH config: $HOME/.ssh/config -> $BACKUP"
  cp "$HOME/.ssh/config" "$BACKUP"
fi
cp "$GIT_DIR/ssh-config" "$HOME/.ssh/config"
chmod 600 "$HOME/.ssh/config"

# --- Extract private key stubs from YubiKey ---
echo ""
echo "Extracting resident SSH keys from YubiKey..."
echo "You will be prompted for your FIDO2 PIN and may need to touch the key."

TEMP_DIR=$(mktemp -d)
if ssh-keygen -K -f "$TEMP_DIR/id_sk" 2>/dev/null; then
  # Match extracted keys to our known public keys by fingerprint
  TOUCH_FP=$(ssh-keygen -lf "$HOME/.ssh/yubikey-touch.pub" | awk '{print $2}')
  NFC_FP=$(ssh-keygen -lf "$HOME/.ssh/yubikey-nfc.pub" | awk '{print $2}')

  for key in "$TEMP_DIR"/id_sk_rk_*; do
    [[ "$key" == *.pub ]] && continue
    [[ ! -f "$key" ]] && continue
    KEY_FP=$(ssh-keygen -lf "$key" | awk '{print $2}')
    if [[ "$KEY_FP" == "$TOUCH_FP" ]]; then
      cp "$key" "$HOME/.ssh/yubikey-touch"
      chmod 600 "$HOME/.ssh/yubikey-touch"
      echo "Installed yubikey-touch private key stub"
    elif [[ "$KEY_FP" == "$NFC_FP" ]]; then
      cp "$key" "$HOME/.ssh/yubikey-nfc"
      chmod 600 "$HOME/.ssh/yubikey-nfc"
      echo "Installed yubikey-nfc private key stub"
    fi
  done
  rm -rf "$TEMP_DIR"
else
  rm -rf "$TEMP_DIR"
  echo "Could not extract keys from YubiKey (wrong PIN or no key present)."
  echo "You can manually place the private key stubs at:"
  echo "  ~/.ssh/yubikey-touch"
  echo "  ~/.ssh/yubikey-nfc"
fi

# --- Git commit signing ---
git config --global gpg.format ssh
git config --global user.signingkey "$HOME/.ssh/yubikey-touch.pub"
git config --global commit.gpgsign true
git config --global tag.gpgsign true

# --- Add keys to ssh-agent ---
echo ""
echo "Adding keys to ssh-agent..."
eval "$(ssh-agent -s)" 2>/dev/null || true
[[ -f "$HOME/.ssh/yubikey-touch" ]] && ssh-add "$HOME/.ssh/yubikey-touch" 2>/dev/null && echo "Added yubikey-touch" || true
[[ -f "$HOME/.ssh/yubikey-nfc" ]] && ssh-add "$HOME/.ssh/yubikey-nfc" 2>/dev/null && echo "Added yubikey-nfc" || true

echo ""
echo "Git and SSH setup complete!"
echo "Test with: ssh -T git@github.com"
