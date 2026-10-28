# Checks the starting point before the agent implements a spec:
# the working tree is clean, and the named spec is committed.
param([string]$Spec)

# Not a mandatory parameter: PowerShell would prompt for it, which an agent can't answer.
if (-not $Spec) {
    [Console]::Error.WriteLine("Usage: pwsh -File scripts/check-ready-to-implement.ps1 -Spec <SpecMethodName>")
    exit 1
}

if ($Spec -notmatch '^[A-Za-z_][A-Za-z0-9_]*$') {
    [Console]::Error.WriteLine("'$Spec' is not a spec method name.")
    exit 1
}

$dirty = git status --porcelain
if ($dirty) {
    [Console]::Error.WriteLine("Working tree is not clean:`n$dirty")
    exit 1
}

$found = git grep -n -E "Task\s+$Spec\s*\(" HEAD -- acceptance/Specs
if (-not $found) {
    [Console]::Error.WriteLine("No committed spec named '$Spec' in acceptance/Specs.")
    exit 1
}

Write-Host "Ready to implement. Target spec:`n$found"
