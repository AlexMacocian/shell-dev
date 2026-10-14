---
type: Plan
title: Assistant capabilities and backlog
description: "Current Sherlock assistant integrations and remaining plans, including AI-workspace-only Firefox automation."
tags: [assistant, todo, planning, sherlock]
generated: { by: agent/mcp, at: "2026-10-14T22:38:04Z" }
status: draft
---

## Current integrations

Prefer Go-based MCP binaries that fit Sherlock's setup; browser automation uses Mozilla's maintained Node.js server.

| Capability | Integration |
| --- | --- |
| Web search | SearXNG search and engine discovery through Sherlock. |
| Documents | [paperless-mcp-go](https://github.com/cbinckly/paperless-mcp-go) through Sherlock's public OIDC device flow and authenticated API wrapper. The deployed Paperless stack uses a small mounted multi-client OIDC compatibility extension. |
| Geography and weather | `geo-weather-mcp`: Photon/OpenStreetMap place search and reverse lookup, Open-Meteo current conditions and forecasts. Location is requested on demand: device location when available, then approximate public-IP network-exit location, then ask for a place. Results retain source, timestamp and uncertainty; IP location is not a verified physical position. No location history or fallback home address is stored in OKF or on disk. |
| Browser automation | [Mozilla's Firefox MCP](https://github.com/mozilla/firefox-devtools-mcp), enabled only in the AI workspace. Headless installed Firefox, temporary profiles and the `slim` preset: page navigation, snapshots, input and screenshots, without script-evaluation or privileged developer modules. Package version pinned in the workspace config. Node.js >=20.19 and Firefox required; Selenium manages geckodriver. Runtime logs and explicitly saved outputs may remain on disk; the MCP is not a sandbox. |
| Email | Experimental Linux-only read-only `proton-mcp` using [go-proton-api](https://github.com/ProtonMail/go-proton-api), without Bridge or a plaintext mail index. Enabled in the [workspace config](../../sherlock.toml). Sherlock reuses a stored session or opens Proton's own website before agent launch; no CLI credentials or local password form. Proton's internal session-fork protocol is not public third-party OAuth. Tokens and derived mailbox-key material use the OS-keyring wallet; `sherlock status` and `sherlock logout proton` manage it. Three tools list subject-filtered metadata, decrypt message bodies and list labels/folders. Real-account browser authorization and all three tools are verified. Full regression gates, callback checks and WebCrypto compatibility pass. One active client; no sending, mailbox mutations or attachment downloads. No credentials, account identities or mail contents belong in OKF. |

The workspace has its own [Sherlock configuration](../../sherlock.toml), separate from the repository-root config. Existing memory, search, documents and geography/weather remain enabled.

Browser validation covered JavaScript-rendered content, form filling/clicking, screenshots and public HTTPS page extraction. Two separate sessions confirmed cookies/localStorage were not reused, browser subprocesses stopped on stdio shutdown and temporary profiles were removed. Normal Firefox profiles are not attached.

Native geography lookup requires Linux GeoClue and OS authorization; other platforms use IP fallback. The server does not install a location daemon or bypass permissions.

## Planned

| TODO | Priority | Requirements and candidates |
| --- | --- | --- |
| Add calendar access | Separate ongoing work | Google, Outlook and DAV reads; OAuth-based per-user access. Another agent is implementing this integration. |
| Add local-event discovery | Backlog | Evaluate a skill consuming event/newsletter RSS feeds. |
| Select and integrate a lists/task service | Backlog | Shopping lists, household tasks and shared lists; no service selected. |
| Evaluate voice workflows | Validation pending | Copilot CLI exposes `/voice` dictation through Foundry Local; local runtime setup is being tested. Spoken replies and continuous voice interaction are not established. |

Image input uses screenshots/crops pasted into Copilot chat; no separate image integration is needed for that workflow. Backlog items are not implemented integrations. Travel-document retrieval belongs in Paperless. Reuse existing monitoring integrations rather than adding another monitoring system.

Source: user requests and verified integration work in this workspace conversation, 2026-10-14.
