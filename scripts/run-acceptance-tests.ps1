# Deploys the system in fresh containers and runs every spec through each channel,
# or through one: -Channel ui.

# Not mandatory: PowerShell would prompt for it, which an agent can't answer.
param([string]$Channel)

# The channels, by name. acceptance.Dsl/Core/Wiring/ChannelType.cs keeps its own list
# (the ChannelType enum): add or rename a channel in both places.
$allChannels = "api", "ui"

# Checked before deploying, so a mistyped channel fails at once.
$channels = $allChannels
if ($Channel -and $Channel.Trim()) {
    $channels = @($Channel.Trim().ToLowerInvariant())
    if ($channels[0] -notin $allChannels) {
        [Console]::Error.WriteLine(
            "Unknown channel '$Channel'. Use one of: $($allChannels -join ', '), or none for every channel.")
        exit 1
    }
}

# Docker must be running first. If it isn't, try to start Docker Desktop and wait for it.
if (-not (Get-Command docker -ErrorAction SilentlyContinue)) {
    [Console]::Error.WriteLine("Docker isn't installed, or isn't on the PATH.")
    exit 1
}

function Test-DockerRunning {
    docker info *> $null
    $LASTEXITCODE -eq 0
}

if (-not (Test-DockerRunning)) {
    $waitSeconds = 60
    Write-Host "Docker isn't running. Starting Docker Desktop (waiting up to $waitSeconds seconds)..."
    # --detach returns at once; Docker is ready only when `docker info` succeeds.
    docker desktop start --detach *> $null
    if ($LASTEXITCODE -ne 0) {
        # No Docker Desktop CLI: an older Docker Desktop, or Docker Engine without Desktop.
        [Console]::Error.WriteLine(
            "Docker isn't running and Docker Desktop couldn't be started from here. Start Docker, then run this again.")
        exit 1
    }
    $timer = [Diagnostics.Stopwatch]::StartNew()
    while (-not (Test-DockerRunning) -and $timer.Elapsed.TotalSeconds -lt $waitSeconds) {
        Start-Sleep -Seconds 3
    }
    if (-not (Test-DockerRunning)) {
        [Console]::Error.WriteLine(
            "Docker isn't running and couldn't be started. Start Docker Desktop, then run this again.")
        exit 1
    }
    Write-Host "Docker is running."
}

docker compose up -d --build --force-recreate
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

# $name, not $channel: PowerShell variable names ignore case, so $channel is $Channel.
$failed = @()
foreach ($name in $channels) {
    Write-Host "`n=== Acceptance tests: $name ===`n"
    $env:ACCEPTANCE_CHANNEL = $name
    dotnet test acceptance.Specs
    if ($LASTEXITCODE -ne 0) { $failed += $name }
}
Remove-Item Env:ACCEPTANCE_CHANNEL

if ($failed) {
    Write-Host "`nFailed channels: $($failed -join ', ')" -ForegroundColor Red
    exit 1
}
$passed = if ($channels.Count -eq $allChannels.Count) { "All channels passed" } else { "Channel $($channels -join ', ') passed" }
Write-Host "`n$passed" -ForegroundColor Green