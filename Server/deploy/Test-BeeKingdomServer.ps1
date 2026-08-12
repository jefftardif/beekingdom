param(
    [string]$BaseUrl = "http://127.0.0.1:5088",
    [int]$TimeoutSeconds = 10
)

$ErrorActionPreference = "Stop"

$normalizedBaseUrl = $BaseUrl.TrimEnd("/")

Write-Host "Bee Kingdom Server smoke test"
Write-Host "Base URL: $normalizedBaseUrl"

$healthUri = "$normalizedBaseUrl/health"
$pingUri = "$normalizedBaseUrl/protocol/ping"

$health = Invoke-RestMethod -Method Get -Uri $healthUri -TimeoutSec $TimeoutSeconds

if ($health.status -ne "Healthy") {
    throw "Health check failed. Expected status Healthy, received '$($health.status)'."
}

$pingBody = @{
    clientBuild = "server-018-smoke"
    sentAtUtc = (Get-Date).ToUniversalTime().ToString("O")
} | ConvertTo-Json

$ping = Invoke-RestMethod `
    -Method Post `
    -Uri $pingUri `
    -ContentType "application/json" `
    -Body $pingBody `
    -TimeoutSec $TimeoutSeconds

if (-not $ping.protocolVersion) {
    throw "Protocol ping failed. Missing protocolVersion in response."
}

[pscustomobject]@{
    HealthStatus = $health.status
    Service = $health.service
    ProtocolVersion = $ping.protocolVersion
    Environment = $ping.environment
    BaseUrl = $normalizedBaseUrl
}
