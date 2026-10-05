# General AI workspace

A sherlock workspace for AI work that is not tied to any project — research,
one-off questions, notes worth keeping. Opened from the Control Center's **AI**
tile, or by hand:

```sh
cd ~/.local/share/ai && sherlock copilot
```

The real directory is `ai/` in this repo; `~/.local/share/ai` is a symlink to it
(see `linux/init-symlinks.sh`). That is the whole trick: the agent writes
memories into tracked files, so committing and pushing shell-dev propagates them
to every other machine.

## Layout

```text
ai/
├── sherlock.toml              symlink to ../sherlock.toml — marks the project root
├── AGENTS.md                  the instruction file, read by every agent
├── opencode.json              opencode-only config
├── knowledge/                 OKF memory, project scope
├── okf-user/                  OKF memory, user scope  →  ~/.okf
└── .github/
    ├── instructions/          *.instructions.md   — scoped add-ons to AGENTS.md
    ├── agents/                *.agent.md          — subagents for /agent
    └── skills/                <name>/SKILL.md     — procedures for /skills
```

## Why `sherlock.toml` has to be here

Sherlock walks up from the working directory to the first `sherlock.toml` and
treats the directory holding it as the project root. Without a file here the
walk would continue to this repo's root and the agent would write into
shell-dev's own `knowledge/` bundle instead of this one.

The file is a symlink to the repo root's, so there is only one config to
maintain. That does not merge the two roots: sherlock derives the root with
`filepath.Abs`, which does not resolve symlinks, so each directory keeps its own
bundle. Replace the symlink with a real file if the two ever need to differ.

The bundle path is not configurable — it is always `<project root>/knowledge`.

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

`sherlock.toml` enables the OKF memory MCP and searxng web search, for this
workspace and for the repo root alike. Adding a service means spelling it out in
full: sherlock replaces a same-named entry from the global
`~/.config/sherlock/config.toml` wholesale, not field by field. See
`/mnt/seagate/Dev/charlie/project-charlie/sherlock.toml` for the full set.

`[services.memory]` is deliberately kept out of the global config. With no local
`sherlock.toml` the project root falls back to the launch directory, so enabling
memory globally would drop a `knowledge/` folder into whatever directory
sherlock happened to start in.
