# TODO

* In other sessions the agents have referred to Farley using some kind of named parameters in the spec.

* What are Farley's stubs for interacting with external apps / systems?

* Approach for "ensureEntryDoesNotExist", to compliment the existing "ensureEntryExists" scenario. (Gap analysis point 1)

* Alias override technique. (Gap analysis point 2)

* Error messages that echo a value. (Gap analysis point 3)

* Waiting on the API side. (Gap analysis point 4)

* Duplicate identical entries. (Gap analysis point 5)

* How to handle user types? (Gap analysis point 6)

* Time machine for anything date based. (Gap analysis point 7)

* Custom annotation for specs that are written but not implemented yet. (Gap analysis point 8)

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