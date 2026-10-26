$dirty = git status --porcelain
if ($dirty) {
    [Console]::Error.WriteLine("Working tree is not clean:`n$dirty")
    exit 1
}

$specs = git diff --name-only HEAD~1 HEAD -- acceptance/Specs
if (-not $specs) {
    [Console]::Error.WriteLine("The latest commit doesn't add or change a spec in acceptance/Specs.")
    exit 1
}

Write-Host "Preflight passed. Specs in the latest commit:`n$specs"