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
#
# In Claude Code, /spec-agent (and the other role names) and /role do the same.
param([string]$Role)

. "$PSScriptRoot/../.claude/hooks/roles.ps1"

$problem = Test-RolesTable
if ($problem) {
    [Console]::Error.WriteLine($problem)
    exit 1
}

if (-not $Role) {
    Get-RoleStatus | ForEach-Object { Write-Host $_ }
    Write-Host ""
    foreach ($name in Get-RoleNames) { Write-Host ("  {0,-14} {1}" -f $name, (Get-Role $name).summary) }
    Write-Host "`nSet one with: pwsh -File scripts/set-role.ps1 <role>, or /<role> in Claude Code"
    exit 0
}

$problem = Set-WorktreeRole $Role
if ($problem) {
    [Console]::Error.WriteLine($problem)
    exit 1
}
if ($Role -eq 'none') {
    Write-Host "Role cleared. New agent sessions in this worktree can read and discuss, but not edit files."
    exit 0
}
Write-Host "The next session in this worktree will be $Role. Start a new Claude Code session for it"
Write-Host "to take effect; sessions already open keep the role they started with."
