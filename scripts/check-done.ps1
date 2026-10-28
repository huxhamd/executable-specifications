$results = [ordered]@{}

function Invoke-Check([string]$name, [scriptblock]$check) {
    Write-Host "`n=== $name ===`n"
    & $check
    $results[$name] = ($LASTEXITCODE -eq 0)
}

Invoke-Check "Protected files"  { pwsh -NoProfile -File scripts/check-protected-files.ps1 }
Invoke-Check "API unit tests"   { dotnet test api.Tests }
Invoke-Check "Web unit tests"   { Push-Location web; npx ng test --watch=false; Pop-Location }
Invoke-Check "Acceptance tests" { pwsh -NoProfile -File scripts/run-acceptance-tests.ps1 }

Write-Host "`n=== Summary ===`n"
foreach ($entry in $results.GetEnumerator()) {
    if ($entry.Value) { Write-Host "PASS  $($entry.Key)" -ForegroundColor Green }
    else              { Write-Host "FAIL  $($entry.Key)" -ForegroundColor Red }
}

if ($results.Values -contains $false) { exit 1 }
Write-Host "`nVerified: all checks passed." -ForegroundColor Green