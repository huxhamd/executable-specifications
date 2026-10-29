# Sets this worktree's role for Claude Code sessions started from now on.
#
# Sessions already running keep the role they started with. So does the
# protected-files check in check-done.ps1, which judges the work in the tree by
# the role of the last session started here.
#
#   pwsh -File scripts/set-role.ps1             shows the roles and the choices
#   pwsh -File scripts/set-role.ps1 spec-agent  sets the role for the next session
#   pwsh -File scripts/set-role.ps1 none        clears it: new agent sessions can read
#                                               and discuss, but not edit files
param([string]$Role)

. "$PSScriptRoot/../.claude/hooks/roles.ps1"

$names = Get-RoleNames

# Each path may belong to one role only.
$seen = @{}
foreach ($name in $names) {
    foreach ($entry in @((Get-Role $name).owns)) {
        if (-not $entry) { continue }
        if ($seen.ContainsKey($entry)) {
            [Console]::Error.WriteLine(".claude/roles.json gives '$entry' to both $($seen[$entry]) and $name.")
            exit 1
        }
        $seen[$entry] = $name
    }
}

if (-not $Role) {
    $active = Get-ActiveRole
    $stored = if (Test-Path -LiteralPath (Get-RoleFilePath)) { (Get-Content -LiteralPath (Get-RoleFilePath) -Raw).Trim() }
    if ($stored -and $stored -ne (Get-WorktreeRole)) {
        Write-Host "This worktree is set to '$stored', which is no longer a role, so it counts as none."
    }
    Write-Host "Next session:                 $(Get-WorktreeRole)"
    Write-Host "Last session started here:    $(if ($active) { $active } else { '(none yet)' })`n"
    foreach ($name in $names) { Write-Host ("  {0,-14} {1}" -f $name, (Get-Role $name).summary) }
    Write-Host "`nSet one with: pwsh -File scripts/set-role.ps1 <role>"
    exit 0
}

$path = Get-RoleFilePath
if ($Role -eq 'none') {
    Remove-Item -LiteralPath $path -ErrorAction SilentlyContinue
    Write-Host "Role cleared. New agent sessions in this worktree can read and discuss, but not edit files."
    exit 0
}

if ($Role -cnotin $names) {
    [Console]::Error.WriteLine("Unknown role '$Role'. Choose one of: $($names -join ', '), or none.")
    exit 1
}

Set-Content -LiteralPath $path -Value $Role
Write-Host "The next session in this worktree will be $Role. Start a new Claude Code session for it"
Write-Host "to take effect; sessions already open keep the role they started with."
