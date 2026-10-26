$changes = git status --porcelain -- acceptance/ .claude/settings.json scripts/acceptance.ps1 scripts/check-guardrails.ps1
if ($changes) {
    [Console]::Error.WriteLine("Protected files have uncommitted changes:`n$changes")
    exit 2
}
Write-Host "Guardrails intact: no protected files have changed."
exit 0