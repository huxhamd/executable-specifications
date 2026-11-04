---
description: "Run the definition of done: protected files, unit and integration tests, and acceptance specs on every channel"
disable-model-invocation: true
---
The human typed /check-done to check whether the work in this worktree is done.

From the repository root, run:

    pwsh -File scripts/check-done.ps1

It runs the acceptance specs on every channel after the unit and integration
tests, so allow it up to 10 minutes.

Then report the summary: each check with PASS, FAIL or SKIP, and for each failure,
what failed and why. Don't change any files or try to fix anything: the
human decides what happens next.
