---
type: Reference
title: Bloodborne on Linux via bbhost
description: "Bloodborne GOTY runs on Linux through bbhost at /mnt/seagate/Games/Bloodborne, including the PKG extraction and eboot decryption steps."
tags: [bloodborne, bbhost, linux, gaming, ps4, vulkan]
generated: { by: agent/mcp, at: "2026-10-14T11:58:16Z" }
status: stable
---

Bloodborne (PS4) runs natively on Linux via [bbhost](https://github.com/droogie/bbhost),
installed at `/mnt/seagate/Games/Bloodborne/`. Launch with `./play.sh` or the
Bloodborne desktop entry.

## Why bbhost

bbhost is a translation layer, not an emulator: the game's own 1.09 x86-64
executable runs on the host CPU, with PS4 system calls reimplemented and GCN
shaders recompiled to Vulkan. It builds and ships native Linux binaries.

`Awakenchan/BloodBorneButPc` was rejected: it is a Windows-only C#/WPF launcher
with no Linux build, no Linux CI, and no Wine/Proton guidance.
`deadinside28/bloodborne_pc` ("bbport") is the Linux-native alternative in the
same family if bbhost ever breaks.

## Layout

| Path | Contents |
|---|---|
| `games/CUSA03173` | Base GOTY dump (EU), the folder holding `dvdroot_ps4` |
| `games/CUSA03173-UPDATE` | Update 1.09; bbhost reads its files in place of the base's |
| `games/CUSA03173-UPDATE/eboot.elf` | Decrypted executable bbhost loads |
| `bbhost/bbhost-linux-v0.2.18/` | Release build plus `bbhost.toml` |
| `tools/` | `ps4-pkg-tools` and `ps4_unfself` |
| `data/` | Saves, shader caches, generated assets |
| `data/mods/` | Mod overlay |
| `icon.png` | Copy of the dump's `sce_sys/icon0.png` |

bbhost imports `bbhost.toml` into `~/.config/bbhost/bbhost.toml`, which every
copy of bbhost reads.

The desktop entry is `~/.local/share/applications/bloodborne.desktop`, a plain
local file. Most entries in that directory are local; only ones symlinked into
`shell-dev/.local/share/applications/` are tracked by the dotfiles repository.
Its `StartupWMClass=bbhost` matches the window class bbhost reports.

## Setup steps

The base and update `.pkg` files are not kept, so a fresh dump is needed to
repeat extraction. `tools/` contains source for both tools; `ps4-pkg-tools` has
no built CLI, so run `make cli` before extracting.

1. Extract each `.pkg` with
   [ps4-pkg-tools](https://github.com/xXJSONDeruloXx/ps4-pkg-tools). It names
   output folders by title ID, so extract base and update to separate parents.
2. Convert the update's `eboot.bin` to an ELF with
   [ps4_unfself](https://github.com/SocraticBliss/ps4_unfself). bbhost pins the
   result to SHA-256 `941f887a562aae054fac35af8cc8f27cf075f3d4cc2e029fb5ae2a663aaa5ae7`
   and refuses any other build. `SelfUtil-Patched` produces different bytes and
   is rejected.
3. Point `paths.app0` at the base folder and `paths.eboot` at `eboot.elf`.

Validate without opening a window using `./bbhost --config bbhost.toml --headless`.

## Mods

bbhost never edits the game files. A file at `data/mods/dvdroot_ps4/<path>`
replaces the dump's `dvdroot_ps4/<path>`. The startup log confirms the overlay
with `FS overlay=/mnt/seagate/Games/Bloodborne/data/mods (shadows /app0)`, and
it takes priority over bbhost's own generated assets.

Controller button glyphs live in `dvdroot_ps4/menu/common.tpf.dcx`. Xbox and
Switch replacements come from
[Xbox (and switch) UI for Bloodborne](https://www.nexusmods.com/bloodborne/mods/30)
by Dommo, which ships Elden Ring and Dark Souls III glyph styles. Downloading
needs a Nexus account, and Cloudflare blocks scripted fetches.

That mod's "Elden Ring Style - Modern Xbox prompts" file is installed, so the
game shows Xbox glyphs. Deleting the overlay copy restores the PlayStation
glyphs.

bbhost rewrites prompts to the bound keys while keyboard and mouse are in use,
so such a texture mod only shows with a controller.

## Notes

- The GOTY base already contains The Old Hunters (281 `m21*` files), so bbhost
  logs `patch: old-hunters: 1 of 1 sites written`. The separate DLC `.pkg` is
  US-region (`UP9000-CUSA00900`), entitlement-only metadata with no game data,
  and is unused.
- The dump's SELF files are fake-signed, so segments are stored unencrypted and
  need no console keys.
- AMD on Linux needs `/dev/udmabuf` readable by the user. A `uaccess` ACL
  already grants this; otherwise add a udev rule with `KERNEL=="udmabuf",
  TAG+="uaccess"`.
- Online play uses the private server `thehuntersdream.com` on UDP 9307.
- The i5-9400F (6 cores, no SMT) limits frame rate before the RX 9060 XT does.
