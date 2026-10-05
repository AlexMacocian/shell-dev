# Skills

Procedures the agent loads on demand, one directory per skill holding a
`SKILL.md`. Copilot CLI discovers them here automatically and lists them under
`/skills`.

```text
.github/skills/
└── transcribe-video/
    ├── SKILL.md         # frontmatter: name, description — plus the procedure
    └── (any scripts or templates the procedure refers to)
```

The `description` is the only part always in context; it is what the agent
matches against to decide the skill is relevant, so write it as a trigger
("Use when the user asks to ..."), not as a title.

Skills are for multi-step procedures with supporting files. A single rule
belongs in `AGENTS.md`.

| Skill | Use |
| ----- | --- |
| [okf-memory](./okf-memory/SKILL.md) | At conversation startup and for requests to remember or save information: find the relevant OKF bundle, write or update a concept, and verify it. |
