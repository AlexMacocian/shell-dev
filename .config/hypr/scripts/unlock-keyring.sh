#!/usr/bin/env bash
# Auto-unlocks the GNOME keyring on Hyprland session start.
#
# Why: SDDM autologin doesn't run pam_gnome_keyring with a password (no PAM
# auth_tok available), so the login.keyring stays locked. Microsoft
# identity broker / intune-portal both REQUIRE a password-protected keyring
# to store credentials — see
# https://gitlab.gnome.org/GNOME/gnome-keyring/-/issues/103
#
# This script reads the keyring password from a 0600 file and pipes it to
# gnome-keyring-daemon, which both starts the daemon and unlocks the
# default keyring (`login`) in one shot.
#
# IMPORTANT — sync safety:
# This script is part of the shell-dev dotfiles repo and is symlinked
# across machines. The password file is intentionally placed OUTSIDE the
# synced ~/.config/hypr/ tree (which is the symlinked dir) so it never
# gets committed. On a machine without ~/.config/.keyring-pwd this script
# just starts the daemon normally — same behaviour as before.
#
# Setup on a new machine:
#   ( umask 077; read -s -p 'Linux password: ' PWD \
#       && printf '%s' "$PWD" > ~/.config/.keyring-pwd \
#       && unset PWD && echo OK )

set -euo pipefail

PWD_FILE="$HOME/.config/.keyring-pwd"

if [ ! -r "$PWD_FILE" ]; then
    echo "[unlock-keyring] $PWD_FILE missing; falling back to bare daemon start" >&2
    eval "$(gnome-keyring-daemon --start --components=secrets)"
    exit 0
fi

# Refuse to use a world/group-readable password file
PERMS=$(stat -c '%a' "$PWD_FILE")
if [ "$PERMS" != "600" ] && [ "$PERMS" != "400" ]; then
    echo "[unlock-keyring] $PWD_FILE has perms $PERMS, refusing (chmod 600)" >&2
    eval "$(gnome-keyring-daemon --start --components=secrets)"
    exit 0
fi

# --replace replaces any daemon socket-activated by systemd with no password.
# --unlock reads the password from stdin and unlocks the default keyring.
# --daemonize ensures the call returns after the daemon backgrounds itself.
# tr -d '\r\n' strips any accidental trailing newline from the password
# file — gnome-keyring treats stdin verbatim, so a 'foo\n' file would be
# interpreted as a 4-char password 'foo\n' and silently fail.
eval "$(tr -d '\r\n' < "$PWD_FILE" | gnome-keyring-daemon --replace --unlock --components=pkcs11,secrets --daemonize)"
