$changes = git status --porcelain -- acceptance/ .claude/settings.json scripts/acceptance.ps1 scripts/check-guardrails.ps1
if ($changes) {
    [Console]::Error.WriteLine("Protected files have uncommitted changes:`n$changes")
    exit 2
}
exit 0