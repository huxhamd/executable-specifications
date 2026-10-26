$protected = @(
    "acceptance/",
    ".claude/settings.json",
    ".claude/skills/implement-spec/",
    "scripts/acceptance.ps1",
    "scripts/check-guardrails.ps1",
    "scripts/preflight.ps1",
    "scripts/verify.ps1"
)
$changes = git status --porcelain -- $protected
if ($changes) {
    [Console]::Error.WriteLine("Protected files have uncommitted changes:`n$changes")
    exit 2
}
Write-Host "Guardrails intact: no protected files have changed."
exit 0