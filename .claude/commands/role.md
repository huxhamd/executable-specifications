---
description: Show this worktree's roles, or set the next session's role (/role <name> or /role none)
argument-hint: "[role | none]"
disable-model-invocation: true
---
<!-- role-command: role $ARGUMENTS -->
The human typed /role to see or set this worktree's roles. The role hook has
already handled it, and its result is in your context. Relay that result to
the human briefly, and do nothing else.

If there is no role command result in your context, the hook didn't run.
Don't change the role yourself, and don't run set-role.ps1. Tell the human
to run `pwsh -File scripts/set-role.ps1` in a terminal instead (with the role
name, if they gave one).
