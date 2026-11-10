$results = [ordered]@{}

function Invoke-Check([string]$name, [scriptblock]$check) {
    Write-Host "`n=== $name ===`n"
    & $check
    $results[$name] = ($LASTEXITCODE -eq 0)
}

function Invoke-CheckIfPresent([string]$name, [string]$path, [scriptblock]$check) {
    if (Test-Path $path) { Invoke-Check $name $check }
    else { $results[$name] = $null }
}

Invoke-Check "Protected files"  { pwsh -NoProfile -File .claude/hooks/check-protected-files.ps1 }
Invoke-Check "API unit tests"   { dotnet test api.UnitTests }
Invoke-Check "Web unit tests"   { Push-Location web; npx ng test --watch=false; Pop-Location }
Invoke-CheckIfPresent "API integration tests" "api.IntegrationTests" { dotnet test api.IntegrationTests }
Invoke-Check "Acceptance tests" { pwsh -NoProfile -File scripts/run-acceptance-tests.ps1 }

Write-Host "`n=== Summary ===`n"
foreach ($entry in $results.GetEnumerator()) {
    if ($null -eq $entry.Value) { Write-Host "SKIP  $($entry.Key) (no project yet)" -ForegroundColor Yellow }
    elseif ($entry.Value)       { Write-Host "PASS  $($entry.Key)" -ForegroundColor Green }
    else                        { Write-Host "FAIL  $($entry.Key)" -ForegroundColor Red }
}

if ($results.Values -contains $false) { exit 1 }
Write-Host "`nVerified: all checks passed." -ForegroundColor Green