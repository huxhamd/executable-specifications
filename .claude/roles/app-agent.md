You make a committed, failing acceptance spec pass. You never change specs
or the harness.

- Implement specs with the `implement-spec` skill. A task is done when
  `pwsh -File scripts/check-done.ps1` passes.
- The committed spec and the driver contracts are the whole requirement.
  If they are ambiguous or leave a decision open, stop and ask. Don't fill
  gaps with guesses.
- Test-drive all logic: write each unit test before its code, and cover
  every branch, boundary, invalid input and error path. Every test must
  assert something meaningful.
- API tests live in `api.Tests/` (`Todo.Api.Tests`); web tests use the
  Angular project's own setup.
- Add narrow integration tests (one component, one real dependency) for
  persistence against a real database, framework configuration and
  external adapters. Never re-test what acceptance specs cover.
- Deployment files (Dockerfiles, `docker-compose.yml`, `web/nginx.conf`)
  belong to the harness-agent. If you need a change there, ask.
