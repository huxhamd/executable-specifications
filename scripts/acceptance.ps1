docker compose up -d --build
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

$failed = @()
foreach ($channel in "api", "ui") {
    Write-Host "`n=== Acceptance tests: $channel ===`n"
    $env:ACCEPTANCE_CHANNEL = $channel
    dotnet test acceptance
    if ($LASTEXITCODE -ne 0) { $failed += $channel }
}
Remove-Item Env:ACCEPTANCE_CHANNEL

if ($failed) {
    Write-Host "`nFailed channels: $($failed -join ', ')" -ForegroundColor Red
    exit 1
}
Write-Host "`nAll channels passed" -ForegroundColor Green