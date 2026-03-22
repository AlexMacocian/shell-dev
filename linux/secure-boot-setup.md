# Secure Boot Setup — CachyOS on HP EliteBook 8 G1a

## System Info

- **Model**: HP EliteBook 8 G1a 16 inch Notebook Next Gen AI PC
- **BIOS**: X84 Ver. 01.03.03
- **OS**: CachyOS x86_64
- **Kernel**: linux-cachyos, linux-cachyos-lts
- **Bootloader**: GRUB 2:2.14
- **Shim**: shim-signed 15.8+ubuntu+1.59 (from AUR)

## Architecture

The boot chain is:

```
UEFI Firmware
  → shimx64.efi (BOOTx64.EFI) — signed by Microsoft UEFI CA 2011
    → grubx64.efi (standalone) — signed with MOK key
      → vmlinuz-linux-cachyos — signed with MOK key
```

## Key Files

| File | Purpose |
|---|---|
| `/etc/mok/MOK.key` | Private key (4096-bit RSA) — **keep secret** |
| `/etc/mok/MOK.crt` | Public certificate (PEM format, for sbsign) |
| `/etc/mok/MOK.cer` | Public certificate (DER format, for MokManager) |
| `/boot/efi/MOK.cer` | Copy on ESP for MokManager enrollment from disk |

The MOK certificate has subject `CN=CachyOS MOK`, valid 2026–2036.

## ESP Layout (`/boot/efi/EFI/cachyos/`)

| File | What it is |
|---|---|
| `BOOTx64.EFI` | Ubuntu shim, signed by Microsoft |
| `grubx64.efi` | **Standalone** GRUB with all modules embedded + SBAT, signed with MOK |
| `mmx64.efi` | MokManager (for key enrollment) |
| `grubx64.efi.bak` | Old unsigned GRUB (can be deleted) |
| `grubx64.efi.old` | Previous GRUB version (can be deleted) |

## EFI Boot Entry

```
Boot0000* cachyos  .../\EFI\cachyos\BOOTx64.EFI
```

The entry points to **shim** (`BOOTx64.EFI`), which chain-loads `grubx64.efi`.

## Automatic Kernel Signing

A pacman hook signs kernels automatically on install/upgrade:

**Hook** — `/etc/pacman.d/hooks/99-secureboot-sign.hook`:
```ini
[Trigger]
Operation = Install
Operation = Upgrade
Type = Path
Target = boot/vmlinuz-*

[Action]
Description = Signing kernel for Secure Boot...
When = PostTransaction
Exec = /usr/local/bin/sbsign-kernel
Depends = sbsigntools
```

**Script** — `/usr/local/bin/sbsign-kernel`:
```bash
#!/usr/bin/env bash
KEY="/etc/mok/MOK.key"
CERT="/etc/mok/MOK.crt"

for kernel in /boot/vmlinuz-*; do
    [ -f "$kernel" ] || continue
    if ! sbverify --cert "$CERT" "$kernel" &>/dev/null; then
        echo "Signing $kernel..."
        sbsign --key "$KEY" --cert "$CERT" --output "$kernel" "$kernel"
    else
        echo "$kernel is already signed."
    fi
done
```

## HP-Specific Gotchas

### 1. Missing Microsoft 3rd Party UEFI CA (Secured-core PC)

HP EliteBooks ship as "Secured-core PCs" without the Microsoft Corporation UEFI CA 2011 in the firmware db. Shim is signed with this key so it won't load without it.

**Fix**: BIOS (F10) → Security → Secure Boot Configuration → **Enable "Allow Microsoft 3rd Party UEFI CA"**

### 2. HP Sure Start prevents clearing Secure Boot keys

Sure Start re-enables itself on every reboot, preventing entry into Setup Mode. This blocks `sbctl enroll-keys`, which would otherwise be the simpler approach.

**Consequence**: You cannot use `sbctl` to manage firmware keys. Must use shim + MOK instead.

### 3. GRUB must be standalone (shim_lock_verifier)

When shim launches GRUB, shim's `shim_lock_verifier` blocks GRUB from loading unsigned modules from disk. A regular `grub-install` produces a small GRUB that loads modules from `/boot/efi/EFI/cachyos/`, which all fail verification.

**Fix**: Build GRUB as a standalone binary with all needed modules embedded:

```bash
sudo grub-mkstandalone \
  --sbat /usr/share/grub/sbat.csv \
  -O x86_64-efi \
  -o /boot/efi/EFI/cachyos/grubx64.efi \
  --modules="part_gpt part_msdos fat btrfs ext2 normal boot linux \
    chain configfile echo search search_fs_uuid search_fs_file search_label \
    ls cat help true test keystatus regexp reboot halt gfxterm gfxterm_background \
    gfxmenu font png jpeg all_video video video_bochs video_cirrus \
    videoinfo gettext tpm cryptodisk luks luks2 gcry_rijndael gcry_sha256 \
    gcry_sha512 password_pbkdf2 sleep loadenv" \
  "boot/grub/grub.cfg=/boot/grub/grub.cfg"
```

> **Note**: `linuxefi` and `gfxterm_menu` modules do not exist on CachyOS — the `linux` module handles EFI loading natively.

Then sign it:
```bash
sudo sbsign --key /etc/mok/MOK.key --cert /etc/mok/MOK.crt \
  --output /boot/efi/EFI/cachyos/grubx64.efi \
  /boot/efi/EFI/cachyos/grubx64.efi
```

### 4. SBAT section required

Shim 15.3+ refuses to load any EFI binary without a `.sbat` section. The `--sbat /usr/share/grub/sbat.csv` flag embeds it. Without it you get a `0x1A security violation`.

## Manual Re-signing (if needed)

### Sign a kernel
```bash
sudo sbsign --key /etc/mok/MOK.key --cert /etc/mok/MOK.crt \
  --output /boot/vmlinuz-linux-cachyos /boot/vmlinuz-linux-cachyos
```

### Verify a binary is signed
```bash
sudo sbverify --cert /etc/mok/MOK.crt /boot/efi/EFI/cachyos/grubx64.efi
sudo sbverify --cert /etc/mok/MOK.crt /boot/vmlinuz-linux-cachyos
```

### Check SBAT section exists
```bash
sudo objdump -j .sbat -s /boot/efi/EFI/cachyos/grubx64.efi
```

### Check Secure Boot status
```bash
mokutil --sb-state
```

### List enrolled MOK keys
```bash
mokutil --list-enrolled
```

## Re-enrollment (if MOK key is lost from shim's database)

If after a firmware update the MOK key disappears:

1. Boot with Secure Boot disabled
2. `sudo mokutil --import /etc/mok/MOK.cer` (set a one-time password)
3. Reboot with Secure Boot enabled
4. MokManager will prompt — enroll the pending key using the password

Or from MokManager directly:
1. Select "Enroll key from disk"
2. Select the EFI partition
3. Select `MOK.cer`
4. Confirm enrollment

## After GRUB Package Updates

If `grub` is updated by pacman, you need to rebuild the standalone GRUB:

```bash
# Rebuild standalone GRUB with embedded modules
sudo grub-mkstandalone \
  --sbat /usr/share/grub/sbat.csv \
  -O x86_64-efi \
  -o /boot/efi/EFI/cachyos/grubx64.efi \
  --modules="part_gpt part_msdos fat btrfs ext2 normal boot linux \
    chain configfile echo search search_fs_uuid search_fs_file search_label \
    ls cat help true test keystatus regexp reboot halt gfxterm gfxterm_background \
    gfxmenu font png jpeg all_video video video_bochs video_cirrus \
    videoinfo gettext tpm cryptodisk luks luks2 gcry_rijndael gcry_sha256 \
    gcry_sha512 password_pbkdf2 sleep loadenv" \
  "boot/grub/grub.cfg=/boot/grub/grub.cfg"

# Sign it
sudo sbsign --key /etc/mok/MOK.key --cert /etc/mok/MOK.crt \
  --output /boot/efi/EFI/cachyos/grubx64.efi \
  /boot/efi/EFI/cachyos/grubx64.efi

# Also update grub.cfg
sudo grub-mkconfig -o /boot/grub/grub.cfg

# Fix boot entry if grub-install overwrote it
sudo efibootmgr -b 0000 -B
sudo efibootmgr --create --disk /dev/nvme0n1 --part 1 \
  --label "cachyos" --loader '\EFI\cachyos\BOOTx64.EFI'
```

**Do NOT run plain `grub-install`** — it will overwrite the standalone GRUB with a regular one that will fail under shim.

## LUKS Keyfile (Single Passphrase Prompt)

By default, GRUB prompts for the LUKS passphrase to load the kernel, then the initramfs prompts again to mount root. A keyfile embedded in the initramfs eliminates the second prompt.

### Key Files

| File | Purpose |
|---|---|
| `/etc/cryptsetup-keys.d/root.key` | 2KB random keyfile (root:root, mode 600) |
| `/etc/crypttab.initramfs` | Tells `sd-encrypt` to use the keyfile |

### How It Works

1. GRUB unlocks LUKS with your passphrase to read kernel + initramfs
2. initramfs uses the embedded keyfile to unlock LUKS for root — no second prompt

The keyfile lives on the encrypted partition, so it cannot be read without first unlocking LUKS.

### Setup

```bash
# Create keyfile
sudo mkdir -p /etc/cryptsetup-keys.d
sudo dd bs=512 count=4 if=/dev/urandom of=/etc/cryptsetup-keys.d/root.key iflag=fullblock
sudo chmod 600 /etc/cryptsetup-keys.d/root.key

# Add keyfile to LUKS partition
sudo cryptsetup luksAddKey /dev/nvme0n1p2 /etc/cryptsetup-keys.d/root.key
```

Add to `/etc/mkinitcpio.conf`:
```
FILES=(/etc/cryptsetup-keys.d/root.key)
```

Create `/etc/crypttab.initramfs`:
```
cryptroot  UUID=cacb6282-f437-48ff-be8b-d0050e04a10f  /etc/cryptsetup-keys.d/root.key  luks
```

Rebuild:
```bash
sudo mkinitcpio -P
sudo grub-mkconfig -o /boot/grub/grub.cfg
# Then rebuild standalone GRUB and sign (see "After GRUB Package Updates" above)
```
