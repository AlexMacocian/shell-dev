---
name: okf-memory
description: "Use at the start of an AI workspace conversation and when the user asks to remember, store, save, or learn from information, including extracted reference data. Search OKF, identify the intended content, choose a bundle, write or update it, and verify persistence."
---

# OKF memory workflow

## Resolve and search

1. Discover the memory MCP tool definitions before calling them.
2. Resolve the workspace's `knowledge/` directory and `~/.okf` to absolute
   real paths, following symlinks. Shell commands expand `~`; MCP arguments
   must contain the resolved path, not a literal tilde.
3. Search each bundle explicitly with `okf_search`, passing its real path as
   `bundle` and concise terms for the current topic as `query`. Read relevant
   results with `okf_show`, using the same bundle.
4. A failed lookup is not an empty result. Report unavailable memory rather
   than silently substituting built-in memory. If default or scoped searches
   return nothing, check the explicit real bundle paths before concluding
   that no relevant memory exists.

## Identify what to save

1. Identify the information the user means, not just the memory workflow.
   After extracting collection dates, "store this info in OKF" refers to the
   dates, not a preference to use OKF. If the reference is ambiguous, ask.
2. Read the workspace's `AGENTS.md` memory directives to determine eligibility
   and scope. Exclude sensitive and personal information. Public reference
   data may be recorded without adding personal associations.
3. Use the resolved user bundle for eligible cross-project preferences and
   facts; use the resolved workspace bundle for workspace-specific information.
   Ask when the intended scope is unclear.
4. Saving an ordinary file does not by itself record OKF memory. For requests
   to save extracted reference data in this workspace, record the information
   in OKF; also produce a CSV or other artifact when requested.

## Write and verify

1. Search the destination bundle for duplicates using terms for the actual
   content, then read candidate concepts. Update an equivalent concept with
   `okf_update`; otherwise create one with `okf_create`. Always pass the
   explicit resolved `bundle`.
2. Include the requested information in the body. For extracted data, retain
   the source URL, applicable year, categories, and complete date list. Clearly
   mark unresolved entries rather than guessing.
3. Use MCP writes so parent indexes and the bundle log are maintained. Do not
   manually edit bundle files as a fallback for failed MCP writes.
4. Read the concept back with `okf_show` using the same bundle and concept ID.
   Check the requested content, not merely whether a file exists; for a date
   list, check the full list, categories, and count against the extraction.
5. Report the saved concept and scope only after verification. If persistence
   failed, report the failure. Do not claim that an artifact in session storage
   is OKF memory or that local writes have been committed or pushed.
