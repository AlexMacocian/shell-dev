# AI Workspace — Instructions for AI Agents

## Normative Language

- KEYWORDS: Uppercase `MUST`, `MUST NOT`, `REQUIRED`, `SHALL`, `SHALL NOT`,
  `SHOULD`, `SHOULD NOT`, `RECOMMENDED`, `MAY`, and `OPTIONAL` have the meanings
  defined by [RFC 2119](https://www.rfc-editor.org/rfc/rfc2119), clarified by
  [RFC 8174](https://www.rfc-editor.org/rfc/rfc8174). Only uppercase forms carry
  these special meanings.

## Workspace Codex

- DOMAIN: general-purpose AI workspace == research, reasoning, durable notes
- ROOT: workspace == `~/.local/share/ai`, a symlink to `ai/` in the shell-dev
  dotfiles repository
- SCOPE: work that belongs to no specific project. Agents MUST NOT grow a
  codebase here; work belonging to a real project MUST be redirected to that
  project's repository.
- GOAL: Agents MUST preserve correctness, clarity, and reliability, and MUST
  prefer being useful over being agreeable.

## Workspace Context

- Agents MUST read [README.md](./README.md) before changing the workspace's own
  layout, configuration, or memory bundles.
- VCS: every file here is tracked in the shell-dev repository. Writing a file is
  a change to that repository; it reaches other machines only once shell-dev is
  committed and pushed.
- AGENT: `sherlock copilot`, started from this directory. `sherlock.toml` is
  what makes this directory the project root and places the memory MCP in front
  of the agent.

## Documentation Invariants

- Documentation MUST be brief and context-only.
- Documentation MUST NOT include filler or restatements of self-evident content.
- References to files MUST link to the actual file.
- Related items SHOULD be listed in tables.
- Diagrams MUST use Mermaid syntax; ASCII art MUST NOT be used.
- Documentation MUST NOT describe unimplemented or proposed behavior as current
  behavior. Planned behavior MUST be labeled as planned.
- Documentation MUST describe the current state only, and MUST NOT include
  migration history or comparisons with removed implementations.

## Instruction Scope

- General AI directives MUST be placed in this file.
- Path- or topic-scoped directives MUST be placed in
  `.github/instructions/*.instructions.md`.
- Task-specific workflows MUST be placed in `.github/skills/*/SKILL.md`.
- Agents MUST NOT split a single rule across files.

## Memory Directives

- Agents MUST load [okf-memory](./.github/skills/okf-memory/SKILL.md) at the
  start of each conversation and before handling requests to remember, store, or
  save information in OKF.
- MUST use OKF memory, not copilot in-built memory.
- On each conversation, agents MUST look up existing OKF memory, filtering for
  information related to the current topic.
- OKF bundles are tracked in the public `shell-dev` repository. Agents MUST store
  only information suitable for public, repository-level memory.
- When useful, agents MAY reference a private Paperless document in OKF using
  only a neutral label, its document ID, and its authenticated Paperless URL.
  The reference grants no access; Paperless read permissions remain required.
  URLs MUST NOT contain credentials, tokens, or sensitive query parameters.
- Agents MUST NOT copy private document contents, excerpts, summaries, or
  personal/private details into OKF. Keep those details in Paperless.
- Credentials, tokens, and secrets MUST NOT be stored in OKF or Paperless; use
  an approved secret manager.
- Before recording eligible information, agents MUST search the appropriate OKF
  scope for duplicates and update an existing concept rather than create a
  duplicate.
- `knowledge/` is project-scoped and visible only in this workspace;
  `okf-user/` is visible across Sherlock projects. Both scopes have the same
  public-information restriction.

## Language Review

- After each implementation or documentation change, agents MUST spawn a GPT-6
  Luna subagent to simplify related docs and remove Claude-isms like heavy
  metaphors or "load-bearing"/"footgun" jargon.

## Tooling

- Use calendar mcp tools for personal centralized events, items, reminders
  related
- Use searxng tools for web search
- Use geo-weather tools for location and weather services
- Use paperless mcp tools for personal document storage and retrieval
- Use proton tools for a centralized personal email access
