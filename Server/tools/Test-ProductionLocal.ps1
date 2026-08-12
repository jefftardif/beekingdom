[CmdletBinding()]
param(
    [ValidateRange(1024, 65535)]
    [int]$Port = 5088,
    [switch]$NoBuild,
    [string]$AssemblyPath = ''
)

$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Net.Http
$serverRoot = Split-Path -Parent $PSScriptRoot
$project = Join-Path $serverRoot 'src\BeeKingdom.Server\BeeKingdom.Server.csproj'
$defaultAssembly = Join-Path $serverRoot 'src\BeeKingdom.Server\bin\Release\net8.0\BeeKingdom.Server.dll'
$assembly = if ([string]::IsNullOrWhiteSpace($AssemblyPath)) { $defaultAssembly } else { [System.IO.Path]::GetFullPath($AssemblyPath) }
$serverFullPath = [System.IO.Path]::GetFullPath($serverRoot).TrimEnd('\') + '\'
if (-not $assembly.StartsWith($serverFullPath, [StringComparison]::OrdinalIgnoreCase) -or [System.IO.Path]::GetFileName($assembly) -ne 'BeeKingdom.Server.dll') {
    throw 'AssemblyPath doit cibler BeeKingdom.Server.dll sous Server/.'
}

if (-not $NoBuild -and $assembly -eq $defaultAssembly) {
    & dotnet build $project --configuration Release --no-restore
    if ($LASTEXITCODE -ne 0) { throw "La compilation serveur a echoue ($LASTEXITCODE)." }
}

if (-not (Test-Path -LiteralPath $assembly)) {
    throw "Binaire Release absent: $assembly"
}

$baseUri = "http://127.0.0.1:$Port"
$probe = [System.Net.Sockets.TcpListener]::new([System.Net.IPAddress]::Loopback, $Port)
try {
    $probe.Start()
}
catch {
    throw "Le port loopback $Port est deja utilise; smoke annule."
}
finally {
    $probe.Stop()
}
$startInfo = [System.Diagnostics.ProcessStartInfo]::new()
$startInfo.FileName = (Get-Command dotnet).Source
$startInfo.Arguments = '"' + $assembly + '"'
$startInfo.WorkingDirectory = Split-Path -Parent $assembly
$startInfo.UseShellExecute = $false
$startInfo.CreateNoWindow = $true
$startInfo.RedirectStandardOutput = $true
$startInfo.RedirectStandardError = $true
$childEnvironment = @{
    DOTNET_ROLL_FORWARD = 'Major'
    ASPNETCORE_ENVIRONMENT = 'Production'
    ASPNETCORE_URLS = $baseUri
    Persistence__Provider = 'InMemory'
    Chat__Enabled = 'false'
    Chat__RealtimeEnabled = 'false'
    BeeKingdom__EnableBackgroundWorkers = 'false'
}
$previousEnvironment = @{}

$process = [System.Diagnostics.Process]::new()
$process.StartInfo = $startInfo
$client = [System.Net.Http.HttpClient]::new()
$client.Timeout = [TimeSpan]::FromSeconds(3)

try {
    foreach ($name in $childEnvironment.Keys) {
        $previousEnvironment[$name] = [Environment]::GetEnvironmentVariable($name, 'Process')
        [Environment]::SetEnvironmentVariable($name, $childEnvironment[$name], 'Process')
    }
    try {
        if (-not $process.Start()) { throw 'Le processus serveur local n’a pas demarre.' }
    }
    finally {
        foreach ($name in $childEnvironment.Keys) {
            [Environment]::SetEnvironmentVariable($name, $previousEnvironment[$name], 'Process')
        }
    }
    $deadline = [DateTimeOffset]::UtcNow.AddSeconds(30)
    $health = $null
    while ([DateTimeOffset]::UtcNow -lt $deadline -and -not $process.HasExited) {
        try {
            $health = $client.GetStringAsync("$baseUri/health").GetAwaiter().GetResult() | ConvertFrom-Json
            break
        }
        catch {
            Start-Sleep -Milliseconds 250
        }
    }

    if ($null -eq $health) {
        throw 'Le endpoint /health n’est pas devenu disponible dans les 30 secondes.'
    }

    $capabilitiesResponse = $client.GetAsync("$baseUri/chat/v1/capabilities").GetAwaiter().GetResult()
    if (-not $capabilitiesResponse.IsSuccessStatusCode) { throw "Capabilities indisponibles: $([int]$capabilitiesResponse.StatusCode)" }
    $cacheControl = [string]$capabilitiesResponse.Headers.CacheControl
    if ($cacheControl -notmatch 'no-store' -or $cacheControl -notmatch 'no-cache' -or $cacheControl -notmatch 'max-age=0' -or $cacheControl -notmatch 'must-revalidate') {
        throw "Capabilities cacheables: $cacheControl"
    }
    if ($capabilitiesResponse.Headers.Age -and $capabilitiesResponse.Headers.Age.TotalSeconds -gt 0) { throw 'Capabilities ne doit pas porter un Age positif.' }
    $capabilities = $capabilitiesResponse.Content.ReadAsStringAsync().GetAwaiter().GetResult() | ConvertFrom-Json
    $readiness = $client.GetStringAsync("$baseUri/runtime/chat-readiness").GetAwaiter().GetResult() | ConvertFrom-Json

    if ($health.status -ne 'Healthy') { throw "Sante inattendue: $($health.status)" }
    if ($capabilities.protocolVersion -ne 'chat-v1') { throw "Protocole inattendu: $($capabilities.protocolVersion)" }
    if ([int]$capabilities.idempotencyReceiptRetentionDays -lt 7) { throw 'Retention des recus chat incompatible.' }
    if ($capabilities.server -ne $false -or $capabilities.realtime -ne $false) { throw 'Le smoke local exige server=false et realtime=false.' }
    if ($readiness.status -ne 'PreparationOnly' -or $readiness.enabled -ne $false -or $readiness.realtimeEnabled -ne $false) { throw 'Readiness chat non sure pour le smoke local.' }

    [pscustomobject]@{
        Success = $true
        Environment = 'Production'
        BaseUri = $baseUri
        HealthStatus = $health.status
        ProtocolVersion = $capabilities.protocolVersion
        IdempotencyReceiptRetentionDays = [int]$capabilities.idempotencyReceiptRetentionDays
        ChatServer = $capabilities.server
        ChatRealtime = $capabilities.realtime
        Readiness = $readiness.status
    } | ConvertTo-Json -Depth 3
}
finally {
    $client.Dispose()
    if (-not $process.HasExited) {
        $process.Kill()
        $process.WaitForExit(5000) | Out-Null
    }
    $process.Dispose()
}
