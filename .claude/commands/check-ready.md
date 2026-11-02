---
description: "Check a spec is ready to hand to the app agent: clean tree, spec committed"
argument-hint: "<SpecName>"
disable-model-invocation: true
---
The human typed /check-ready to check that a spec is ready to implement.

If no spec name was given after the command, ask which spec, and do nothing
else. Otherwise, from the repository root, run:

    pwsh -File scripts/check-ready-to-implement.ps1 -Spec "$ARGUMENTS"

Then say whether the spec is ready. If it isn't, pass on what the script
said. Don't change any files.
