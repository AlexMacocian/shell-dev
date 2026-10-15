---
type: Preference
title: "Ship Sherlock changes as releases, not local installs"
description: "After changing Sherlock, bump VERSION, commit and push; the user runs `sherlock update`. Never go install temporary binaries."
tags: [sherlock, release, workflow]
generated: { by: agent/mcp, at: "2026-10-15T14:59:02Z" }
status: stable
---

After changing the Sherlock repository (`/mnt/seagate/Dev/sherlock`):

- Bump `VERSION`, commit as `Release X.Y.Z: <summary>`, and push to `main`. CI runs lint and tests, then tags the release from `VERSION`.
- The user then runs `sherlock update` to install it.
- MUST NOT run `go install` or otherwise put temporary or dev binaries in `~/go/bin`.

Source: user request, 2026-10-15 ("bump version, commit, push and I'll do sherlock update").
