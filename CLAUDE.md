# Working agreement

- `acceptance/` holds executable specifications owned by the human.
  Never edit, delete, skip or rename anything in it. If a spec looks
  wrong, stop and explain why instead of working around it.
- The drivers in `acceptance/Drivers` define the contract: API routes
  and JSON shape, and the `data-testid` attributes the UI must expose.
- Test-drive internal code with unit tests: xUnit for the API (add a
  `Todo.Api.Tests` project), the Angular project's own test setup for web.
- A task is done when all of these pass:
  1. `pwsh -File scripts/acceptance.ps1` (rebuilds, then runs both channels)
  2. all unit tests