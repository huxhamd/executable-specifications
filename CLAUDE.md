# Working agreement

## Ownership
- `acceptance/` and every path denied in `.claude/settings.json` are
  owned by the human. Never modify, delete, skip or rename them. If a
  spec looks wrong or a guardrail blocks you, stop and explain why.
- Never commit. The human commits after review.
- The drivers in `acceptance/Drivers` define the contract: API routes,
  JSON shape and the UI's `data-testid` attributes.

## Testing
- Test-drive all logic: write each unit test before its code, and cover
  every branch, boundary, invalid input and error path. Every test must
  assert something meaningful.
- API tests live in `api.Tests/` (`Todo.Api.Tests`); web tests use the
  Angular project's own setup.
- Add narrow integration tests (one component, one real dependency) for
  persistence against a real database, framework configuration and
  external adapters. Never re-test what acceptance specs cover.

## Done
- Implement acceptance specs with the `implement-spec` skill.
- A task is done when `pwsh -File scripts/verify.ps1` passes.