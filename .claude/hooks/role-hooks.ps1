# Claude Code hooks that enforce the session's role. Wired up in .claude/settings.json.
#
#   SessionStart       Locks this worktree's role for the session, records it as
#                      the worktree's active role, and gives the agent its role
#                      instructions. Its output becomes context.
#   UserPromptSubmit   Handles the role commands (/spec-agent and so on, and /role),
#                      which only the human can type. Records the working tree, so
#                      the Stop hook can tell what changed during the turn. Its
#                      output becomes context, so it prints only what the agent
#                      should see.
#   PreToolUse         Blocks file edits outside the role's files, edits inside
#                      the git folder, role changes and commits.
#   Stop               Checks what changed during the turn, including through
#                      shell commands the PreToolUse hook can't see into.
param(
    [Parameter(Mandatory)]
    [ValidateSet('SessionStart', 'UserPromptSubmit', 'PreToolUse', 'Stop')]
    [string]$Event
)

. "$PSScriptRoot/roles.ps1"

$hookInput = [Console]::In.ReadToEnd() | ConvertFrom-Json
$sessionId = $hookInput.session_id
$session = Read-Session $sessionId

function Block([string]$message) {
    [Console]::Error.WriteLine($message)
    exit 2
}

function Write-SystemMessage([string]$message) {
    @{ systemMessage = $message } | ConvertTo-Json -Compress
}

function Get-RoleContext([string]$role) {
    $lines = @("# Session role: $role", "")
    if ($role -eq 'none') {
        $lines += @(
            "No role is set for this worktree, so you can't change any files in it.",
            "You may read, explain and discuss. Before doing anything else, tell the human",
            "that no role is set, and that they can set one by typing a role command such",
            "as ``/app-agent`` and then ``/clear``, or with ``pwsh -File scripts/set-role.ps1 <role>``.",
            "Roles: $((Get-RoleNames) -join ', ').")
        return $lines -join "`n"
    }

    $instructions = Join-Path $RepoRoot ".claude/roles/$role.md"
    if (Test-Path -LiteralPath $instructions) {
        $lines += (Get-Content -LiteralPath $instructions -Raw).TrimEnd()
        $lines += ""
    }
    if (Test-Unrestricted $role) {
        $lines += "You may edit any file in the repo. Changes that cross the other agents' boundaries are reported to the human."
    } else {
        $lines += "You may edit only: $(Get-OwnedList $role). Everything else is read-only for you, and hooks enforce it."
    }

    $template = $RolesConfig.templateBranch
    $branch = git branch --show-current
    $worksOnTemplate = (Get-Role $role).worksOnTemplate
    if ($template -and $worksOnTemplate -eq $true -and $branch -ne $template) {
        $lines += "", "Warning: your work belongs on the ``$template`` branch, but this worktree is on ``$branch``. Point this out to the human before changing anything."
    }
    if ($template -and $worksOnTemplate -eq $false -and $branch -eq $template) {
        $lines += "", "Warning: this worktree is on the ``$template`` branch, which holds the template rather than a project. Point this out to the human before changing anything."
    }
    $lines += "", "State your role in your first reply."
    $lines -join "`n"
}

switch ($Event) {

    'SessionStart' {
        # Fires again on resume and after compaction: keep the role the session started with.
        # /clear starts a fresh conversation, so it picks up the worktree's role afresh.
        if (-not $session -or $hookInput.source -eq 'clear') {
            $session = [pscustomobject]@{
                role     = Get-WorktreeRole
                roleFile = Get-RoleFileHash
                state    = Get-WorkingState
            }
            Save-Session $sessionId $session
        }
        # Also on resume and compaction: this session is the active one again.
        Set-ActiveRole $session.role
        Get-RoleContext $session.role
        exit 0
    }

    'UserPromptSubmit' {
        # Role commands. This hook only runs for prompts the human submits, so the
        # agent can't trigger them. Recognise the typed command, or the marker in
        # the command file in case the prompt arrives expanded.
        $prompt = [string]$hookInput.prompt
        $command = $null
        if ($prompt -match '^\s*/([\w-]+)(?:\s+([\w-]+))?\s*$') {
            $command = @($Matches[1], $Matches[2])
        } elseif ($prompt -match '<!--\s*role-command:\s*([\w-]+)(?:\s+([\w-]+))?\s*-->') {
            $command = @($Matches[1], $Matches[2])
        }
        if ($command -and ($command[0] -eq 'role' -or $command[0] -cin (Get-RoleNames))) {
            $sessionRole = if ($session) { $session.role } else { 'none' }
            $wanted = if ($command[0] -eq 'role') { $command[1] } else { $command[0] }
            $result = @("Role command result. Relay this to the human, briefly, and do nothing else.", "")
            if ($wanted) {
                $problem = Set-WorktreeRole $wanted
                if ($problem) {
                    $result += $problem
                } elseif ($wanted -eq 'none') {
                    $result += "Role cleared for the next session in this worktree. This session stays $sessionRole."
                } else {
                    $result += "The next session in this worktree will be $wanted. This session stays $sessionRole."
                    $result += "Type /clear to start a fresh session as $wanted."
                }
                $result += ""
            }
            $result += Get-RoleStatus $sessionRole
            $result -join "`n"
        }

        if (-not $session) {
            # The hooks were installed after this session started. Fail closed.
            $session = [pscustomobject]@{ role = 'none'; roleFile = ''; state = $null }
            "This session started before its role could be recorded, so you can't change any files. Tell the human to start a new session."
        }
        $session.roleFile = Get-RoleFileHash
        $session.state = Get-WorkingState
        Save-Session $sessionId $session
        exit 0
    }

    'PreToolUse' {
        $role = if ($session) { $session.role } else { 'none' }
        $tool = $hookInput.tool_name
        $toolInput = $hookInput.tool_input

        if ($tool -eq 'Bash') {
            $command = [string]$toolInput.command
            if ($command -match 'claude-role|claude-sessions|set-role') {
                Block "Only the human can change a worktree's role. Don't try another way: tell the human what you need."
            }
            if ($command -match '\bgit\s+(?:(?:-C|-c)\s+\S+\s+|--?[\w-]+(?:=\S+)?\s+)*(?:commit|push)\b') {
                Block "Never commit or push. Leave your changes uncommitted: the human commits after review."
            }
            exit 0
        }

        $path = if ($toolInput.file_path) { $toolInput.file_path } else { $toolInput.notebook_path }
        if (-not $path) { exit 0 }

        if (Test-InGitFolder $path) {
            Block "Files in the git folder, including the role settings, can't be edited."
        }
        if (Test-InOtherWorktree $path) {
            Block "That file is in another worktree, which has its own role. Work only in this worktree."
        }
        $repoPath = Get-RepoPath $path
        if ($null -eq $repoPath) { exit 0 }   # outside the repo: not ours to police
        if (Test-MayEdit $role $repoPath) { exit 0 }

        if ($role -eq 'none') {
            Block "No role is set for this session, so files can't be changed. Tell the human to type a role command such as /app-agent, then /clear."
        }
        $owner = Get-Owner $repoPath
        $ownedBy = if ($owner) { "belongs to $owner" } else { "isn't owned by any role" }
        Block ("``$repoPath`` $ownedBy, and this session is $role. " +
            "Don't try another way to change it. If the change is needed, stop and tell the human which agent should make it.")
    }

    'Stop' {
        if (-not $session -or $null -eq $session.state) { exit 0 }
        $role = $session.role
        $changed = Get-ChangedPaths $session.state (Get-WorkingState)
        $roleFileChanged = (Get-RoleFileHash) -ne $session.roleFile
        $roleWarning = if ($roleFileChanged) {
            "This worktree's role was changed during the turn. If you didn't run set-role.ps1 yourself, check why."
        }

        if (Test-Unrestricted $role) {
            $groups = $changed | Group-Object { $o = Get-Owner $_; if ($o) { $o } else { 'no role' } }
            $notes = @()
            # Report changes that span agents, touch unowned files, or touch the harness itself.
            $harness = @(Get-RoleNames | Where-Object { (Get-Role $_).worksOnTemplate -eq $true })
            if ($groups.Count -gt 1 -or ($groups | Where-Object { $_.Name -eq 'no role' -or $_.Name -in $harness })) {
                $notes += "This $role turn changed files owned by other agents:`n" +
                    (($groups | ForEach-Object { "- $($_.Name): $($_.Group -join ', ')" }) -join "`n")
            }
            if ($roleWarning) { $notes += $roleWarning }
            if ($notes) { Write-SystemMessage ($notes -join "`n`n") }
            exit 0
        }

        $violations = @($changed | Where-Object { -not (Test-MayEdit $role $_) })
        if (-not $violations) {
            if ($roleWarning) { Write-SystemMessage $roleWarning }
            exit 0
        }

        if ($hookInput.stop_hook_active) {
            # Already asked once. Let the turn end rather than loop, and warn the human.
            $note = "Files outside $role's files changed during this turn and were not restored:`n" +
                ($violations -join "`n")
            if ($roleWarning) { $note += "`n`n$roleWarning" }
            Write-SystemMessage $note
            exit 0
        }

        # Files the human had already changed must not be restored with git, or their work is lost.
        $clean = @($violations | Where-Object { -not $session.state.Contains($_) })
        $dirty = @($violations | Where-Object { $session.state.Contains($_) })
        $message = "You changed files outside $role's files during this turn. You must not change these."
        if ($clean) {
            $message += "`n`nUndo these with git restore (or delete them if you created them):`n" +
                ($clean -join "`n")
        }
        if ($dirty) {
            $message += "`n`nThese already had the human's uncommitted changes before this turn. " +
                "Undo only your own edits and do not use git restore, which would discard " +
                "the human's work:`n" + ($dirty -join "`n")
        }
        # A role-file warning is for the human; it's shown when the turn ends.
        $message += "`n`nThen stop and tell the human which agent should make the change."
        Block $message
    }
}
