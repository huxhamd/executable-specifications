# Working agreement

## Roles
- Every session has one role: spec-agent, app-agent, harness-agent or
  free-agent. The human sets it per worktree, with a role command such
  as `/spec-agent` or with `scripts/set-role.ps1`, and it is fixed for
  the whole session.
- Your role and its instructions are given at the start of the session.
  State your role in your first reply.
- `.claude/roles.json` says which files each role may edit, and hooks
  enforce it. Edit only files your role owns. If a hook blocks you, don't
  look for another way: stop and tell the human which role should make
  the change.
- Never change your role, the role settings or the hooks.
- Without a role you may read, explain and discuss, but not change files.

## Always
- Never commit or push. The human commits after review.
- The drivers in `acceptance/Drivers` define the contract: API routes,
  JSON shape and the UI's `data-testid` attributes.
- Specs describe behaviour in domain language. Every spec must pass on
  every channel unless it is marked `[Fact, Channels(...)]`.
