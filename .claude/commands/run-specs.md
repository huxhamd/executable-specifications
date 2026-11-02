---
description: "Deploy fresh containers and run the acceptance specs, on one channel or every channel"
argument-hint: "[channel]"
disable-model-invocation: true
---
The human typed /run-specs to run the acceptance specs.

From the repository root, run:

    pwsh -File scripts/run-acceptance-tests.ps1 -Channel "$ARGUMENTS"

An empty channel runs every channel. The script deploys fresh containers
first, so allow it up to 10 minutes.

Then report briefly: which channels passed, and for each failing spec, its
channel and why it failed. If the script rejected the channel, pass on its
message. Don't change any files or try to fix anything: the human decides
what happens next.
