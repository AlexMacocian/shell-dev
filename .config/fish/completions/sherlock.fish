# fish completion for sherlock.
# Installed by install.sh into $__fish_config_dir/completions/sherlock.fish
# when a fish config directory is present. Hand-maintained: keep in sync
# with cmd/sherlock/main.go (subcommands) and internal/agent (agents).

# ── Subcommands (only when no subcommand has been given yet) ──────────
complete -c sherlock -n '__fish_use_subcommand' -f -a 'status'  -d 'Show wallet contents (one entry per service)'
complete -c sherlock -n '__fish_use_subcommand' -f -a 'logout'  -d "Forget all stored tokens, or just one service's"
complete -c sherlock -n '__fish_use_subcommand' -f -a 'run'     -d 'Spawn an agent (run <agent> [args...])'
complete -c sherlock -n '__fish_use_subcommand' -f -a 'version' -d 'Print the sherlock version and exit'
complete -c sherlock -n '__fish_use_subcommand' -f -a 'help'    -d 'Show usage'

# Agent aliases are also top-level subcommands (`sherlock copilot` ==
# `sherlock run copilot`).
complete -c sherlock -n '__fish_use_subcommand' -f -a 'copilot' -d 'GitHub Copilot CLI (alias for run copilot)'
complete -c sherlock -n '__fish_use_subcommand' -f -a 'claude'  -d 'Anthropic Claude Code CLI (alias for run claude)'

# ── `sherlock run <agent>` ───────────────────────────────────────────
complete -c sherlock -n '__fish_seen_subcommand_from run' -f -a 'copilot' -d 'GitHub Copilot CLI'
complete -c sherlock -n '__fish_seen_subcommand_from run' -f -a 'claude'  -d 'Anthropic Claude Code CLI'

# ── `sherlock logout [<service>]` ────────────────────────────────────
# Offer the services sherlock ships MCPs for. logout also accepts no
# argument (forget everything), so these are hints, not requirements.
complete -c sherlock -n '__fish_seen_subcommand_from logout' -f -a 'gitea'   -d 'Forget the gitea session'
complete -c sherlock -n '__fish_seen_subcommand_from logout' -f -a 'grafana' -d 'Forget the grafana session'
complete -c sherlock -n '__fish_seen_subcommand_from logout' -f -a 'gssh'    -d 'Forget the gssh session'

# ── Top-level flags ──────────────────────────────────────────────────
complete -c sherlock -n '__fish_use_subcommand' -l version -s v -f -d 'Print the sherlock version and exit'
complete -c sherlock -n '__fish_use_subcommand' -l help    -s h -f -d 'Show usage'
