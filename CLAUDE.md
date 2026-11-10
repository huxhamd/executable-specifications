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
- The drivers in `acceptance.Drivers/<Domain>` define the contract: API routes,
  JSON shape and the UI's `data-testid` attributes.
- Specs describe behaviour in domain language. Every spec must pass on
  every channel unless it is marked `[Fact, Channels(...)]`.
- Specs reach the system through two roots, named for their channel.
  `Api.` (`Api.Todos.Add(...)`) is pinned to the API on every run, and
  `Sut.` (`Sut.Todos.Complete(...)`) is the channel under test. Specs
  arrange what they need through `Api.` and act and check through `Sut.`.
  Check through a pinned channel only an outcome that users of the channel
  under test don't see. A failed action on a pinned channel is reported as
  an arrangement failure.
- Specs run in parallel against one shared, running system. A spec checks
  only what it created itself, through the DSL's aliased names, never the
  state of the whole system, such as how many todos there are in total.
- Don't add code comments unless the human asks for one. If a change makes an
  existing comment wrong, correct it, keeping it no longer than it was.
  Do not embellish existing comments. You may remove comments that aren't needed.