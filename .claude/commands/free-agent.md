---
description: Make the next session in this worktree a free agent
disable-model-invocation: true
---
<!-- role-command: free-agent -->
The human typed /free-agent to choose the role for the next session in this
worktree. The role hook has already handled it, and its result is in your
context. Relay that result to the human briefly, and do nothing else.

If there is no role command result in your context, the hook didn't run.
Don't change the role yourself, and don't run set-role.ps1. Tell the human
to run `pwsh -File scripts/set-role.ps1 free-agent` in a terminal instead.
