# TODO

* Confirming that something is absent, e.g. `ConfirmNotShown`, with an example spec such as deleting a todo. (Gap analysis point 1)

* Example spec for an error message that echoes a value, e.g. completing a todo that's already complete, with `ConfirmErrorIsShown` removing the alias. (Gap analysis point 3)

* Example of waiting for an eventually consistent outcome. (Gap analysis point 4, reopened; see WAITING.md)
 - Bulk import feature returning 202 Accepted, with a spec that imported todos are shown
 - Shared polling helper in `acceptance.Drivers/Core`, used by `ApiTodoDriver` queries
 - One channel-neutral timeout, replacing `ACCEPTANCE_UI_TIMEOUT_SECONDS`, passed to every channel
 - Decide whether the UI driver reloads or the page updates by itself, and record why
 - Walkthrough section that uses the example

* More than one actor: who is acting, and specs with two users. (Gap analysis point 6)

* Controlling the system's boundaries: the clock and stubs for external systems. (Gap analysis point 7)

* Custom annotation for specs that are written but not implemented yet. (Gap analysis point 8)

* Example of adding a DSL parameter without breaking existing specs, e.g. a due date set by a new spec while the existing specs stay untouched. (Leads into gap analysis point 7)

* Decide whether to rename DSL methods whose names repeat their argument, e.g. `AttemptToAddWithTitleOfLength(length: 256)` to `AttemptToAdd(titleLength: 256)`.

* Bring WALKTHROUGH.md back in step: its copies of CLAUDE.md, `.claude/roles.json` and `.claude/roles/spec-agent.md` were already out of date before the analyzer, its spec examples pass DSL arguments positionally, and it has no section on the analyzer.

* ✅ In other sessions the agents have referred to Farley using some kind of named parameters in the spec. (See GAPANALYSIS.md)

* ✅ Specs pass every DSL argument by name, and DSL defaults live on the interfaces, both enforced by an analyzer. (See GAPANALYSIS.md)

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
