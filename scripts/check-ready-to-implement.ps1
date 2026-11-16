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

$found = git grep -n -E "Task\s+$Spec\s*\(" HEAD -- ":(glob)acceptance.Specs/**"
if (-not $found) {
    [Console]::Error.WriteLine("No committed spec named '$Spec' in acceptance.Specs/<Domain>.")
    exit 1
}

$specPattern = '(?m)^(?<attributes>(?:[ \t]*\[[^\r\n]*\][ \t]*\r?\n)+)[^\r\n]*\bTask\s+(?<name>\w+)\s*\('
$pending = $found | ForEach-Object { ($_ -split ':', 3)[1] } | Select-Object -Unique | Where-Object {
    [regex]::Matches((git show "HEAD:$_" | Out-String), $specPattern) |
        Where-Object { $_.Groups['name'].Value -eq $Spec -and $_.Groups['attributes'].Value -match '\bPending\b' }
}
if ($pending) {
    [Console]::Error.WriteLine("'$Spec' is still marked [Pending]. To start it, remove [Pending] from the spec and commit.")
    exit 1
}

Write-Host "Ready to implement. Target spec:`n$found"
