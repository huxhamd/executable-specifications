# Working agreement

- `acceptance/` holds executable specifications owned by the human.
  Never edit, delete, skip or rename anything in it. If a spec looks
  wrong, stop and explain why instead of working around it.
- The drivers in `acceptance/Drivers` define the contract: API routes
  and JSON shape, and the `data-testid` attributes the UI must expose.
- Test-drive internal code with unit tests: xUnit for the API (add a
  `Todo.Api.Tests` project), the Angular project's own test setup for web.
- A task is done when all of these pass:
  1. `docker compose up -d --build`
  2. `ACCEPTANCE_CHANNEL=api dotnet test acceptance`
  3. `ACCEPTANCE_CHANNEL=ui dotnet test acceptance`
  4. all unit tests

@docs/agent-notes.md


# Working agreement

- `acceptance/` holds executable specifications owned by the human.
  Never edit, delete, skip or rename anything in it. If a spec looks
  wrong, stop and explain why instead of working around it.
- `.claude/settings.json` and `.claude/hooks/` are guardrails owned by
  the human. Never modify them. If a guardrail blocks you, stop and
  explain what you were trying to do.
- The drivers in `acceptance/Drivers` define the contract: API routes
  and JSON shape, and the `data-testid` attributes the UI must expose.
- Test-drive internal code with unit tests: xUnit for the API (add a
  `Todo.Api.Tests` project), the Angular project's own test setup for web.
- A task is done when all of these pass:
  1. `docker compose up -d --build`
  2. `ACCEPTANCE_CHANNEL=api dotnet test acceptance`
  3. `ACCEPTANCE_CHANNEL=ui dotnet test acceptance`
  4. `dotnet test` for the API unit test project
  5. `npx ng test --watch=false` in `web/`
