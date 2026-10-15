---
type: Preference
title: Git commit signing with a USB YubiKey
description: "Commit signing needs a YubiKey touch: alert the user, or skip signing when working hands-off."
tags: [git, signing, yubikey]
generated: { by: agent/mcp, at: "2026-10-15T10:27:13Z" }
status: stable
---

Git commits are signed with a USB YubiKey SSH key, and each signature needs a physical touch.

- When a commit needs signing, tell the user to touch the YubiKey before or while running the command.
- When working hands-off, with the user not active in the chat, skip signing (`git commit --no-gpg-sign`) instead of waiting for a touch.
