# Checks that nothing the agent must not change has changed.
#
#   (no switch)    Fails if any protected path differs from the last commit.
#                  Used by verify.ps1: the human's definition of done.
#   -Record        UserPromptSubmit hook. Records the protected paths' current
#                  state for this session, so the Stop hook can tell what the
#                  agent changed during the turn. Prints nothing: a
#                  UserPromptSubmit hook's output becomes context for Claude.
#   -SinceRecord   Stop hook. Fails only if protected paths changed since the
#                  prompt was submitted, so the human's own uncommitted edits
#                  never trap the agent.
param(
    [switch]$Record,
    [switch]$SinceRecord
)

$protected = @(
    "acceptance/",
    ".claude/settings.json",
    ".claude/skills/implement-spec/",
    "scripts/acceptance.ps1",
    "scripts/check-guardrails.ps1",
    "scripts/preflight.ps1",
    "scripts/verify.ps1"
)

Set-Location (git rev-parse --show-toplevel)

# Each changed protected file, with a hash of its content or "deleted".
function Get-ProtectedState {
    $paths = @(git diff --name-only HEAD -- $protected) +
             @(git ls-files --others --exclude-standard -- $protected)
    $state = [ordered]@{}
    foreach ($path in $paths | Where-Object { $_ } | Sort-Object -Unique) {
        $state[$path] = if (Test-Path -LiteralPath $path -PathType Leaf) {
            git hash-object -- $path
        } else { "deleted" }
    }
    $state
}

function Get-RecordPath([string]$sessionId) {
    $folder = git rev-parse --git-path claude-guardrails
    New-Item -ItemType Directory -Force -Path $folder | Out-Null
    $safeId = $sessionId -replace '[^A-Za-z0-9_-]', '_'
    Join-Path $folder "$safeId.json"
}

if ($Record -or $SinceRecord) {
    $hookInput = [Console]::In.ReadToEnd() | ConvertFrom-Json
    $recordPath = Get-RecordPath $hookInput.session_id
}

if ($Record) {
    Get-ProtectedState | ConvertTo-Json -Compress | Set-Content -LiteralPath $recordPath
    exit 0
}

$current = Get-ProtectedState

if (-not $SinceRecord) {
    if ($current.Count) {
        [Console]::Error.WriteLine(
            "Protected files have uncommitted changes:`n$($current.Keys -join "`n")")
        exit 1
    }
    Write-Host "Guardrails intact: no protected files have changed."
    exit 0
}

# Without a record, fall back to comparing with the last commit.
$recorded = @{}
if (Test-Path -LiteralPath $recordPath) {
    $json = Get-Content -LiteralPath $recordPath -Raw | ConvertFrom-Json
    if ($json) { $json.PSObject.Properties | ForEach-Object { $recorded[$_.Name] = $_.Value } }
}

$changed = @($current.Keys) + @($recorded.Keys) | Sort-Object -Unique |
    Where-Object { $current[$_] -ne $recorded[$_] }
if (-not $changed) { exit 0 }

$list = $changed -join "`n"
if ($hookInput.stop_hook_active) {
    # Already asked once. Let the turn end rather than loop, and warn the human.
    @{ systemMessage = "Guardrails: protected files changed during this turn and were not restored:`n$list" } |
        ConvertTo-Json -Compress
    exit 0
}

# Files the human had already changed must not be restored with git, or their work is lost.
$clean = @($changed | Where-Object { -not $recorded.Contains($_) })
$dirty = @($changed | Where-Object { $recorded.Contains($_) })
$message = "You changed protected files during this turn. You must not change these."
if ($clean) {
    $message += "`n`nUndo these with git restore (or delete them if you created them):`n" +
        ($clean -join "`n")
}
if ($dirty) {
    $message += "`n`nThese already had the human's uncommitted changes before this turn. " +
        "Undo only your own edits and do not use git restore, which would discard " +
        "the human's work:`n" + ($dirty -join "`n")
}
$message += "`n`nThen stop and explain why you needed to change them."
[Console]::Error.WriteLine($message)
exit 2
