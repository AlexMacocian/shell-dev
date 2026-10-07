---
type: Fact
title: Hyprland uses Lua configuration
description: This repository configures Hyprland with Lua; use Lua dispatcher syntax for hyprctl commands instead of legacy Hyprland config syntax.
tags: [hyprland, lua, configuration]
generated: { by: agent/mcp, at: "2026-10-07T13:15:58Z" }
status: stable
---

The desktop's Hyprland configuration is `.config/hypr/hyprland.lua`, split into Lua modules with `require()`. It starts `omni-shell` with `hl.exec_cmd()` on `hyprland.start`. For `hyprctl dispatch`, use Lua dispatcher expressions such as `hyprctl dispatch 'hl.dsp.exec_cmd("omni-shell")'`; the legacy `hyprctl dispatch exec omni-shell` form fails with a Lua parse error.

Evidence: `.config/hypr/hyprland.lua:1-8,49-56,176`.
