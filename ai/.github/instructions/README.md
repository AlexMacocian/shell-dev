# Scoped instructions

Markdown files here named `*.instructions.md` are loaded **in addition to**
`AGENTS.md`. Copilot CLI picks up `.github/instructions/**/*.instructions.md`
automatically; opencode does not, which is why `opencode.json` lists this glob
explicitly.

Use these for guidance that only applies to one kind of task, so `AGENTS.md`
stays short enough to be read every session. Put anything that is always true
in `AGENTS.md` instead — a rule split across two files is a rule that gets half
applied.

Copilot CLI supports an `applyTo` frontmatter key to scope a file by path glob:

```markdown
---
applyTo: "**/*.md"
---

Prose rules go here.
```
