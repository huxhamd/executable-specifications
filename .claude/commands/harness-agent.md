---
description: Make the next session in this worktree a harness agent
disable-model-invocation: true
---
<!-- role-command: harness-agent -->
The human typed /harness-agent to choose the role for the next session in this
worktree. The role hook has already handled it, and its result is in your
context. Relay that result to the human briefly, and do nothing else.

If there is no role command result in your context, the hook didn't run.
Don't change the role yourself, and don't run set-role.ps1. Tell the human
to run `pwsh -File scripts/set-role.ps1 harness-agent` in a terminal instead.
