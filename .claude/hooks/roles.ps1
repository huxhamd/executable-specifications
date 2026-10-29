# Shared by the role hooks, scripts/set-role.ps1 and scripts/check-protected-files.ps1.
# Dot-source it: . "$PSScriptRoot/roles.ps1" from this folder, or
# . "$PSScriptRoot/../.claude/hooks/roles.ps1" from scripts/.
#
# Each worktree has one role, kept in its private git folder (never committed).
# Each Claude Code session locks the role it started with, in a session record
# next to it. .claude/roles.json says which files each role may edit.

# This file lives in .claude/hooks, two levels below the repo root.
$RepoRoot = (Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
Set-Location $RepoRoot

$RolesConfig = Get-Content -LiteralPath (Join-Path $RepoRoot '.claude/roles.json') -Raw |
    ConvertFrom-Json
$PathComparison = if ($IsWindows) { [StringComparison]::OrdinalIgnoreCase } else { [StringComparison]::Ordinal }

function Get-RoleNames { @($RolesConfig.roles.PSObject.Properties.Name) }

function Get-Role([string]$name) {
    if ($name) { $RolesConfig.roles.PSObject.Properties[$name].Value }
}

function Test-Unrestricted([string]$role) { [bool](Get-Role $role).unrestricted }

# A path relative to the repo root with forward slashes, or $null if it is outside the repo.
function Get-RepoPath([string]$path) {
    $full = [IO.Path]::GetFullPath(($path -replace '\\', '/'), $RepoRoot)
    $relative = [IO.Path]::GetRelativePath($RepoRoot, $full) -replace '\\', '/'
    if ($relative -eq '.' -or $relative -eq '..' -or $relative.StartsWith('../') -or
        [IO.Path]::IsPathRooted($relative)) { return $null }
    $relative
}

# True for anything inside this worktree's git folder or the shared one, where roles live.
function Test-InGitFolder([string]$path) {
    $full = [IO.Path]::GetFullPath(($path -replace '\\', '/'), $RepoRoot)
    foreach ($folder in @((git rev-parse --absolute-git-dir), (git rev-parse --path-format=absolute --git-common-dir))) {
        $folder = [IO.Path]::GetFullPath($folder).TrimEnd('/', '\')
        if ($full.Equals($folder, $PathComparison) -or
            $full.StartsWith("$folder/", $PathComparison) -or
            $full.StartsWith("$folder\", $PathComparison)) { return $true }
    }
    $false
}

# True for files in another worktree of this repo, which has its own role.
function Test-InOtherWorktree([string]$path) {
    $full = [IO.Path]::GetFullPath(($path -replace '\\', '/'), $RepoRoot)
    $here = [IO.Path]::GetFullPath($RepoRoot).TrimEnd('/', '\')
    foreach ($line in git worktree list --porcelain 2>$null) {
        if ($line -notlike 'worktree *') { continue }
        $folder = [IO.Path]::GetFullPath($line.Substring(9)).TrimEnd('/', '\')
        if ($folder.Equals($here, $PathComparison)) { continue }
        if ($full.Equals($folder, $PathComparison) -or
            $full.StartsWith("$folder/", $PathComparison) -or
            $full.StartsWith("$folder\", $PathComparison)) { return $true }
    }
    $false
}

# The role that owns a repo path: the most specific matching entry wins.
# Entries ending in / are folders; others are single files. $null if nobody owns it.
function Get-Owner([string]$repoPath) {
    $owner = $null
    $longest = -1
    foreach ($role in $RolesConfig.roles.PSObject.Properties) {
        foreach ($entry in @($role.Value.owns)) {
            if (-not $entry) { continue }
            $isMatch = if ($entry.EndsWith('/')) {
                $repoPath.StartsWith($entry, $PathComparison)
            } else {
                $repoPath.Equals($entry, $PathComparison)
            }
            if ($isMatch -and $entry.Length -gt $longest) {
                $owner = $role.Name
                $longest = $entry.Length
            }
        }
    }
    $owner
}

function Test-MayEdit([string]$role, [string]$repoPath) {
    if (-not (Get-Role $role)) { return $false }
    if (Test-Unrestricted $role) { return $true }
    (Get-Owner $repoPath) -eq $role
}

# Every changed or untracked file (ignored files excluded), with a hash of its content.
function Get-WorkingState {
    # 2>$null: git's line-ending warnings would otherwise end up in hook messages.
    $paths = @(git -c core.quotepath=off diff --name-only HEAD 2>$null) +
             @(git -c core.quotepath=off ls-files --others --exclude-standard 2>$null)
    $state = [ordered]@{}
    foreach ($path in $paths | Where-Object { $_ } | Sort-Object -Unique) {
        $state[$path] = if (Test-Path -LiteralPath $path -PathType Leaf) {
            git hash-object -- $path 2>$null
        } else { 'deleted' }
    }
    $state
}

# Two files, both in this worktree's private git folder:
#   claude-role          the role for the next session, as set by set-role.ps1
#   claude-role-active   the role of the last session started here, written by the
#                        SessionStart hook. check-protected-files.ps1 judges the
#                        working tree by it, so switching the role mid-session
#                        doesn't change how the open session's work is judged.
function Get-RoleFilePath { git rev-parse --git-path claude-role }
function Get-ActiveRoleFilePath { git rev-parse --git-path claude-role-active }

# A role name from a role file: a known role, 'none', or $null if the file is missing.
function Read-RoleFile([string]$path) {
    if (-not (Test-Path -LiteralPath $path)) { return $null }
    $name = (Get-Content -LiteralPath $path -Raw).Trim()
    if (Get-Role $name) { $name } else { 'none' }
}

# The role for the next session in this worktree. 'none' if unset or unknown.
function Get-WorktreeRole {
    $role = Read-RoleFile (Get-RoleFilePath)
    if ($role) { $role } else { 'none' }
}

# The role of the last session started in this worktree, or $null if none has started.
function Get-ActiveRole { Read-RoleFile (Get-ActiveRoleFilePath) }

function Set-ActiveRole([string]$role) {
    Set-Content -LiteralPath (Get-ActiveRoleFilePath) -Value $role
}

# A fingerprint of both role files, so a change to either during a turn can be spotted.
function Get-RoleFileHash {
    (@((Get-RoleFilePath), (Get-ActiveRoleFilePath)) | ForEach-Object {
        if (Test-Path -LiteralPath $_) { git hash-object -- $_ 2>$null } else { 'none' }
    }) -join ' '
}

function Get-SessionPath([string]$sessionId) {
    $folder = git rev-parse --git-path claude-sessions
    New-Item -ItemType Directory -Force -Path $folder | Out-Null
    $safeId = $sessionId -replace '[^A-Za-z0-9_-]', '_'
    Join-Path $folder "$safeId.json"
}

# The session record: { role, roleFile, state }, or $null if there is none yet.
function Read-Session([string]$sessionId) {
    $path = Get-SessionPath $sessionId
    if (-not (Test-Path -LiteralPath $path)) { return $null }
    $json = Get-Content -LiteralPath $path -Raw | ConvertFrom-Json
    $state = [ordered]@{}
    if ($json.state) { $json.state.PSObject.Properties | ForEach-Object { $state[$_.Name] = $_.Value } }
    [pscustomobject]@{ role = $json.role; roleFile = $json.roleFile; state = $state }
}

function Save-Session([string]$sessionId, $session) {
    $session | ConvertTo-Json -Depth 4 -Compress |
        Set-Content -LiteralPath (Get-SessionPath $sessionId)
}

# Files whose content differs between two working states.
function Get-ChangedPaths($before, $after) {
    @(@($before.Keys) + @($after.Keys) | Sort-Object -Unique |
        Where-Object { $before[$_] -ne $after[$_] })
}

# An error message if two roles claim the same path, otherwise $null.
function Test-RolesTable {
    $seen = @{}
    foreach ($name in Get-RoleNames) {
        foreach ($entry in @((Get-Role $name).owns)) {
            if (-not $entry) { continue }
            if ($seen.ContainsKey($entry)) {
                return ".claude/roles.json gives '$entry' to both $($seen[$entry]) and $name."
            }
            $seen[$entry] = $name
        }
    }
    $null
}

# Sets the role for this worktree's next session ('none' clears it).
# Returns $null on success, or an error message.
function Set-WorktreeRole([string]$role) {
    $problem = Test-RolesTable
    if ($problem) { return $problem }
    if ($role -eq 'none') {
        Remove-Item -LiteralPath (Get-RoleFilePath) -ErrorAction SilentlyContinue
        return $null
    }
    if ($role -cnotin (Get-RoleNames)) {
        return "Unknown role '$role'. Choose one of: $((Get-RoleNames) -join ', '), or none."
    }
    Set-Content -LiteralPath (Get-RoleFilePath) -Value $role
    $null
}

# The worktree's roles, one per line. Pass the running session's role to include it.
function Get-RoleStatus([string]$sessionRole) {
    $lines = @()
    $stored = if (Test-Path -LiteralPath (Get-RoleFilePath)) { (Get-Content -LiteralPath (Get-RoleFilePath) -Raw).Trim() }
    if ($stored -and $stored -ne (Get-WorktreeRole)) {
        $lines += "This worktree is set to '$stored', which is no longer a role, so it counts as none."
    }
    if ($sessionRole) { $lines += "This session:                 $sessionRole" }
    $active = Get-ActiveRole
    $lines += "Next session:                 $(Get-WorktreeRole)"
    $lines += "Last session started here:    $(if ($active) { $active } else { '(none yet)' })"
    $lines
}

function Get-OwnedList([string]$role) {
    (@((Get-Role $role).owns) | ForEach-Object { "``$_``" }) -join ', '
}
