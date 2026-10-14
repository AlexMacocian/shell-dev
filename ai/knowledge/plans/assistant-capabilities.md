---
type: Plan
title: Assistant capabilities and backlog
description: "Implemented Sherlock assistant integrations and remaining plans, preferring Go-based MCP binaries."
tags: [assistant, todo, planning, sherlock]
generated: { by: agent/mcp, at: "2026-10-14T20:46:00Z" }
status: draft
---

## Current integrations

Prefer Go-based MCP binaries that fit Sherlock's setup.

| Capability | Integration |
| --- | --- |
| Web search | SearXNG search and engine discovery through Sherlock. |
| Documents | [paperless-mcp-go](https://github.com/cbinckly/paperless-mcp-go) through Sherlock's public OIDC device flow and authenticated API wrapper. The deployed Paperless stack uses a small mounted multi-client OIDC compatibility extension. |
| Geography and weather | `geo-weather-mcp`: Photon/OpenStreetMap place search and reverse lookup, Open-Meteo current conditions and forecasts. Location is requested on demand: device location when available, then approximate public-IP network-exit location, then ask for a place. Results retain source, timestamp and uncertainty; IP location is not a verified physical position. No location history or fallback home address is stored in OKF or on disk. |

Geography/weather is enabled in the workspace's [Sherlock configuration](../../../sherlock.toml). Native device lookup requires Linux GeoClue and OS authorization; other platforms use IP fallback. The server does not install a location daemon or bypass permissions.

## Planned

| TODO | Priority | Requirements and candidates |
| --- | --- | --- |
| Add calendar access | Separate ongoing work | Google, Outlook and DAV reads; OAuth-based per-user access. Another agent is implementing this integration. |
| Add email access | Backlog | Evaluate [proton-mail-bridge-client](https://github.com/googlarz/proton-mail-bridge-client) or a Go alternative. Hosted Bridge authentication and deployment suitability remain unresolved. |
| Add browser automation | Backlog | Headless Firefox through a small MCP, using a separate automation browser/profile. |
| Add local-event discovery | Backlog | Evaluate a skill consuming event/newsletter RSS feeds. |
| Select and integrate a lists/task service | Backlog | Shopping lists, household tasks and shared lists; no service selected. |
| Evaluate voice and image workflows | Much later | Speech input/output and image-based assistance. |

Backlog items are not implemented integrations. Travel-document retrieval belongs in Paperless. Reuse existing monitoring integrations rather than adding another monitoring system.

Source: user requests and verified integration work in this workspace conversation, 2026-10-14.
