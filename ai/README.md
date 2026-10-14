# General AI workspace

A sherlock workspace for AI work that is not tied to any project — research,
one-off questions, notes worth keeping. Opened from the Control Center's **AI**
tile, or by hand:

```sh
cd ~/.local/share/ai && sherlock copilot
```

The real directory is `ai/` in this repo; `~/.local/share/ai` is a symlink to it
(see [linux/init-symlinks.sh](../linux/init-symlinks.sh)). Memory files are
tracked here and reach other machines when `shell-dev` is pushed.

## Layout

```text
ai/
├── sherlock.toml              Sherlock config; sets this directory as project root
├── AGENTS.md                  the instruction file, read by every agent
├── opencode.json              opencode-only config
├── knowledge/                 OKF memory, project scope
├── okf-user/                  OKF memory, user scope  →  ~/.okf
└── .github/
    ├── instructions/          *.instructions.md   — scoped add-ons to AGENTS.md
    ├── agents/                *.agent.md          — subagents for /agent
    └── skills/                <name>/SKILL.md     — procedures for /skills
```

## Project root and workspace config

Sherlock walks up from the working directory to the first `sherlock.toml` and
uses its directory as the project root. This workspace has its own
[configuration](sherlock.toml), separate from the
[repository-root configuration](../sherlock.toml). Browser automation is
enabled only here, and memory uses this workspace's bundle.

The bundle path is fixed at `<project root>/knowledge`.

## The two memory bundles

| Bundle      | Scope   | Visible from                  | Holds                                   |
| ----------- | ------- | ----------------------------- | --------------------------------------- |
| `knowledge/`| project | only this workspace           | notes about the workspace and work done in it |
| `okf-user/` | user    | **every** sherlock project    | durable facts and preferences about the user |

`okf-user/` is mounted at `~/.okf`, which is where OKF looks for the user scope.
Because OKF searches project, vendor, user and system scopes together, anything
recorded there is readable from any project's sherlock session — including
project-charlie and this repo — while still being a tracked file here.

The symlink is deliberate: `OKF_USER_DIR` would do the same job, but only for
processes that happened to inherit the variable. A symlink needs no environment
at all.

Neither bundle may contain credentials or secrets. Both are world-readable in
git history once pushed.

## Instructions

`AGENTS.md` is the single source. Copilot CLI and opencode both read it
natively, which is why there is no `.github/copilot-instructions.md` — Copilot
CLI reads *both* that file and `AGENTS.md`, so a symlink between them would feed
the same text to the model twice.

This directory sits inside the shell-dev git repo, so an `AGENTS.md` added at
the repo root would also be loaded here, on top of this one.

`opencode.json` exists only to hand opencode the `.github/instructions/` glob,
which it would otherwise ignore. Its own `instructions` key is not yet honoured
by opencode v2 — if a rule must apply there today, it has to be in `AGENTS.md`.

## Services

[`sherlock.toml`](sherlock.toml) enables OKF memory, SearXNG search,
Paperless document access, geography/weather and Firefox automation.
The experimental direct Proton Mail client reuses a cached session or opens the
configured browser at `https://account.proton.me/authorize`. Credentials and
verification are entered only on Proton's website; there is no local or
terminal login form. It uses Proton's internal session-fork protocol, not
documented public third-party OAuth. A real-account test through the loaded MCP
verified browser authorization, message metadata, labels and decrypted message
bodies.
Geography/weather requests location on demand: device
location when available, then approximate IP location (the network exit, not a
verified physical position); if neither works, it asks for a place. A
same-named local service replaces the global entry, so include all required
fields. See the full set in
[Project Charlie's configuration](../../charlie/project-charlie/sherlock.toml).

`[services.memory]` is deliberately kept out of the global config. With no local
`sherlock.toml` the project root falls back to the launch directory, so enabling
memory globally would drop a `knowledge/` folder into whatever directory
sherlock happened to start in.

## Browser automation

[Mozilla's Firefox MCP](https://github.com/mozilla/firefox-devtools-mcp) runs
headless over stdio, using the installed Firefox and a temporary profile.
It does not attach to normal browsing or retain logins between sessions.
The package version is pinned in [the config](sherlock.toml); `npx` installs
it on first use. Node.js >=20.19 and Firefox are required; Selenium manages
geckodriver.

The `slim` preset exposes page navigation, snapshots, input and screenshots, but
not script evaluation or privileged developer tools. Page content is untrusted;
browser actions can submit forms or change remote state. The MCP is not a
sandbox. Screenshots, saved output and runtime logs can remain on disk even
though the browser profile is temporary.
