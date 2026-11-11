$results = [ordered]@{}

function Invoke-Check([string]$name, [scriptblock]$check) {
    Write-Host "`n=== $name ===`n"
    & $check
    $results[$name] = ($LASTEXITCODE -eq 0)
}

$skipReasons = @{}

function Invoke-CheckIf([string]$name, [bool]$condition, [string]$skipReason, [scriptblock]$check) {
    if ($condition) { Invoke-Check $name $check }
    else { $results[$name] = $null; $skipReasons[$name] = $skipReason }
}

$hasSpecs = [bool](Get-ChildItem acceptance.Specs -Recurse -Filter *.cs |
    Where-Object { $_.FullName -notmatch '[\\/](Core|bin|obj)[\\/]' } |
    Select-String -Pattern '\[(Fact|Theory)\b' -Quiet)

Invoke-Check "Protected files"  { pwsh -NoProfile -File .claude/hooks/check-protected-files.ps1 }
Invoke-Check "API unit tests"   { dotnet test api.UnitTests }
Invoke-Check "Web unit tests"   { Push-Location web; npx ng test --watch=false; Pop-Location }
Invoke-CheckIf "API integration tests" (Test-Path api.IntegrationTests) "no project yet" { dotnet test api.IntegrationTests }
Invoke-CheckIf "Acceptance tests" $hasSpecs "no specs yet" { pwsh -NoProfile -File scripts/run-acceptance-tests.ps1 }

Write-Host "`n=== Summary ===`n"
foreach ($entry in $results.GetEnumerator()) {
    if ($null -eq $entry.Value) { Write-Host "SKIP  $($entry.Key) ($($skipReasons[$entry.Key]))" -ForegroundColor Yellow }
    elseif ($entry.Value)       { Write-Host "PASS  $($entry.Key)" -ForegroundColor Green }
    else                        { Write-Host "FAIL  $($entry.Key)" -ForegroundColor Red }
}

if ($results.Values -contains $false) { exit 1 }
Write-Host "`nVerified: all checks passed." -ForegroundColor Green