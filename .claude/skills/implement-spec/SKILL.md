---
name: implement-spec
description: Make a failing acceptance spec pass. Use when asked to implement an acceptance spec or feature.
---

# Implement a failing acceptance spec

## When to stop
At any point, stop and report instead of continuing if:
- the same failure persists after three genuinely different approaches;
- passing would require changing a spec, a driver contract or a guardrail;
- the spec looks contradictory, ambiguous or impossible to satisfy;
- you need a decision the spec doesn't cover.
Report what you tried, what failed and why, and what you need from the
human.

When you stop, leave your changes uncommitted for the human to review.
Before reporting, check that the API and web projects still build and
that `pwsh -File scripts/check-protected-files.ps1` passes. If your latest
change broke the build, undo that change only.

## Steps
1. Identify the target spec: the spec method named in the request. If none
   is named, stop and ask which one. Run
   `pwsh -File scripts/check-ready-to-implement.ps1 -Spec <SpecName>`. If it fails, stop and
   report its output.
2. Read the target spec and the contract comments in the driver files.
3. Run `pwsh -File scripts/run-acceptance-tests.ps1`. Confirm the target spec fails
   because the behaviour is missing, not because of broken plumbing.
   If it fails for any other reason, or already passes, stop and report.
4. Implement in small steps, following the testing rules in CLAUDE.md,
   until the definition of done in CLAUDE.md is met.
5. Report what changed, any design decisions you made, and anything the
   spec didn't cover that a human should decide.