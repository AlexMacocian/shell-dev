# Custom agents

Subagents available to `/agent`, one `*.agent.md` per agent. Copilot CLI loads
this directory automatically.

A file is frontmatter plus the agent's system prompt:

```markdown
---
name: librarian
description: Searches the OKF bundles and reports what is already known.
tools: ["okf_search", "okf_show"]
---

You are... (the agent's instructions)
```

Reach for one when a job needs its own context window — a long search, a review
— rather than to give the main agent a different personality.
