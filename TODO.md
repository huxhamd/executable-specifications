# TODO

* Confirming that something is absent, e.g. `ConfirmNotShown`, with deleting a todo as the example spec. The UI driver must confirm the list has loaded before waiting for the todo to be gone, or the check passes before anything has happened. (Gap analysis point 1)

* Example spec for an error message that echoes a value, e.g. completing a todo that's already complete, with `ConfirmErrorIsShown` removing the alias. (Gap analysis point 3)

* Example of waiting for an eventually consistent outcome. (Gap analysis point 4, reopened; see WAITING.md)
 - Bulk import feature returning 202 Accepted, with a spec that imported todos are shown
 - Shared polling helper in `acceptance.Drivers/Core`, used by `ApiTodoDriver` queries
 - One channel-neutral timeout, replacing `ACCEPTANCE_UI_TIMEOUT_SECONDS`, passed to every channel
 - Decide whether the UI driver reloads or the page updates by itself, and record why
 - Walkthrough section that uses the example

* More than one actor: who is acting, and specs with two users. (Gap analysis point 6)
 - Also the example of a DSL default: most specs act as a default user, aliased like everything else, and name a user only when it matters, e.g. `as: "alice"`
 - Declare the default user once as a constant that both `Sut.` and `Api.` use, so a spec can't arrange as one user and check as another

* Controlling the system's boundaries: the clock and stubs for external systems. (Gap analysis point 7)

* Custom annotation for specs that are written but not implemented yet. (Gap analysis point 8)

* Decide whether to rename DSL methods whose names repeat their argument, e.g. `AttemptToAddWithTitleOfLength(length: 256)` to `AttemptToAdd(titleLength: 256)`.

* ✅ In other sessions the agents have referred to Farley using some kind of named parameters in the spec. (See GAPANALYSIS.md)

* ✅ Specs pass every DSL argument by name, and DSL defaults live on the interfaces, both enforced by an analyzer. (See GAPANALYSIS.md)

* ✅ A validation helper for domain-language values, `Vocabulary`, and unit tests for the DSL's own code. (See GAPANALYSIS.md)

* ✅ Example of adding a DSL parameter without breaking existing specs: a todo's priority, walkthrough step 9. A due date would also need control of the clock. (Gap analysis point 7)

* ✅ Bring WALKTHROUGH.md back in step with CLAUDE.md, `.claude/roles.json` and `.claude/roles/spec-agent.md`, named arguments and the analyzer.

* ✅ What are Farley's stubs for interacting with external apps / systems? (Merged into gap analysis point 7)

* ✅ Alias override technique. (Gap analysis point 2: not a gap)

* ✅ Duplicate identical entries. (Gap analysis point 5: not a gap)

* ✅ Open source library?

* ✅ When there are no specs (like in `template`), all the tests pass with a warning - probably wrong.

* Update template branch
 - ✅ Spec wording (guide only)
 - ✅ Dsl (Domain Specific Language)
 - ✅ ApiTodoDriver
 - ✅ Skill file

* ✅ stop-hook triggers check-guardrails.ps1 -> fails -> prompts agent -> stop-hook triggers check-guardrails.ps1
  Repeat ad-nausium

* ✅ Can't start if last changes weren't to a spec.

* ✅ A second agent that can only help with the specs and drivers, but never implement?

* ✅ Split the unit and integration test projects

* ✅ Scale and parallism

* ✅ Separate "Spec" project to isolate driver layer

* ✅ Commands for scripts?

* ✅ api.Tests folder when switching branch

* ✅ UI / API channel only tests

* ✅ Old data between channel executions - Is this a problem? No it's not. However, we don't want the data growing uncontrollably, so reset container between runs.

* ✅ Better script names

* ✅ Dedicated "setup" channel

* ✅ Clean-up channel setup implementation

* ✅ A spec on `practice` that uses the setup channel

* ✅ Teaching material on the "pinned channel" for the group.

* ✅ Allow confirmations via the pinned channel?

* ✅ Remove comments in the codebase and prevent the agent from creating new meaningless comments.
