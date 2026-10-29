# Checks that every uncommitted change belongs to the role of the last session
# started in this worktree: the role the work in the tree was done under. Used by
# check-done.ps1, so work can't be called done while it changes files its role
# doesn't own. During a session, the role hooks enforce the same rule.
#
# It deliberately ignores a role set with set-role.ps1 since then: that role
# applies to the next session, not to the work already here.

. "$PSScriptRoot/../.claude/hooks/roles.ps1"

$role = Get-ActiveRole
$source = "the role of the last session started here"
if (-not $role) {
    $role = Get-WorktreeRole
    $source = "no session has started here yet, so this worktree's role"
}
$changed = @((Get-WorkingState).Keys)

if (Test-Unrestricted $role) {
    Write-Host "Role is $role ($source): no ownership limits to check."
    exit 0
}

$outside = @($changed | Where-Object { -not (Test-MayEdit $role $_) })
if ($outside -and $role -eq 'none') {
    $why = if (Get-ActiveRole) { "The last session started here had no role" } else { "No role is set for this worktree" }
    [Console]::Error.WriteLine("$why, so no uncommitted changes are allowed:`n$($outside -join "`n")")
    exit 1
}
if ($outside) {
    [Console]::Error.WriteLine(
        "These uncommitted changes are outside the $role role's files ($source):`n$($outside -join "`n")")
    exit 1
}
Write-Host "Protected files intact: every change belongs to the $role role ($source)."
exit 0
