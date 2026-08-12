[CmdletBinding()]
param(
    [ValidateRange(1024, 65535)]
    [int]$SmokePort = 5089
)

$ErrorActionPreference = 'Stop'
$serverRoot = Split-Path -Parent $PSScriptRoot
$project = Join-Path $serverRoot 'src\BeeKingdom.Server\BeeKingdom.Server.csproj'
$tests = Join-Path $serverRoot 'tests\BeeKingdom.ChatTranslation.Tests\BeeKingdom.ChatTranslation.Tests.csproj'
$fullTests = Join-Path $serverRoot 'tests\BeeKingdom.Tests\BeeKingdom.Tests.csproj'
$configCheck = Join-Path $PSScriptRoot 'Test-ProductionConfiguration.ps1'
$smoke = Join-Path $PSScriptRoot 'Test-ProductionLocal.ps1'
$stamp = [DateTime]::UtcNow.ToString('yyyyMMddTHHmmssZ')
$output = Join-Path $serverRoot "artifacts\candidates\BeeKingdom.Server.$stamp"

& powershell.exe -NoProfile -ExecutionPolicy Bypass -File $configCheck
if ($LASTEXITCODE -ne 0) { throw 'Validation de configuration Production echouee.' }

& dotnet build $project --configuration Release --no-restore
if ($LASTEXITCODE -ne 0) { throw 'Compilation Release echouee.' }

$previousRollForward = $env:DOTNET_ROLL_FORWARD
try {
    $env:DOTNET_ROLL_FORWARD = 'Major'
    & dotnet test $tests --configuration Release --no-restore --logger 'console;verbosity=minimal'
    if ($LASTEXITCODE -ne 0) { throw 'Tests serveur cibles echoues.' }
}
finally {
    $env:DOTNET_ROLL_FORWARD = $previousRollForward
}

& dotnet build $fullTests --configuration Release --framework net10.0 --no-restore /p:EnableNet10TestTarget=true
if ($LASTEXITCODE -ne 0) { throw 'Compilation de la cible HTTP net10 echouee.' }
& dotnet test $fullTests --configuration Release --framework net10.0 --no-build --no-restore /p:EnableNet10TestTarget=true --logger 'console;verbosity=minimal'
if ($LASTEXITCODE -ne 0) { throw 'Suite HTTP complete net10 echouee.' }

& dotnet publish $project --configuration Release --no-restore --output $output /p:UseAppHost=false /p:DebugType=None /p:DebugSymbols=false
if ($LASTEXITCODE -ne 0) { throw 'Publication locale echouee.' }

$assembly = Join-Path $output 'BeeKingdom.Server.dll'
$productionSettings = Join-Path $output 'appsettings.Production.json'
if (-not (Test-Path -LiteralPath $assembly) -or -not (Test-Path -LiteralPath $productionSettings)) {
    throw 'Le candidat ne contient pas le binaire ou appsettings.Production.json.'
}

$developmentSettings = Join-Path $output 'appsettings.Development.json'
if (Test-Path -LiteralPath $developmentSettings) {
    Remove-Item -LiteralPath $developmentSettings -Force
}
$publishedPdbs = Get-ChildItem -LiteralPath $output -Filter '*.pdb' -File
if ($publishedPdbs.Count -gt 0) {
    throw 'Le candidat Production ne doit pas contenir de symboles PDB.'
}

$settings = Get-Content -LiteralPath $productionSettings -Raw | ConvertFrom-Json
if ($settings.Chat.Enabled -ne $false -or $settings.Chat.RealtimeEnabled -ne $false -or $settings.Persistence.Provider -ne 'InMemory') {
    throw 'Le candidat embarque une configuration Production active ou persistante non autorisee.'
}
if (-not [string]::IsNullOrEmpty($settings.SqlServer.ConnectionString) -or -not [string]::IsNullOrEmpty($settings.Ops.AdminKey) -or -not [string]::IsNullOrEmpty($settings.Ops.MigrationApplyKey)) {
    throw 'Le candidat contient une valeur sensible ou un fallback SQL interdit.'
}

& powershell.exe -NoProfile -ExecutionPolicy Bypass -File $smoke -NoBuild -Port $SmokePort -AssemblyPath $assembly
if ($LASTEXITCODE -ne 0) { throw 'Smoke du candidat Production echoue.' }

$manifestFiles = Get-ChildItem -LiteralPath $output -Recurse -File | Sort-Object FullName | ForEach-Object {
    [pscustomobject]@{
        Path = $_.FullName.Substring($output.Length + 1).Replace('\','/')
        Length = $_.Length
        Sha256 = (Get-FileHash -LiteralPath $_.FullName -Algorithm SHA256).Hash.ToLowerInvariant()
    }
}
$manifest = [pscustomobject]@{
    SchemaVersion = 1
    CreatedAtUtc = [DateTime]::UtcNow.ToString('O')
    Environment = 'Production'
    DeploymentAuthorized = $false
    ChatEnabled = $false
    RealtimeEnabled = $false
    PersistenceProvider = 'InMemory'
    Files = $manifestFiles
}
$manifestPath = Join-Path $output 'candidate.manifest.json'
$manifest | ConvertTo-Json -Depth 6 | Set-Content -LiteralPath $manifestPath -Encoding UTF8

$statusPath = Join-Path (Split-Path -Parent $output) 'CANDIDATE-STATUS.json'
$statusCandidates = @()
if (Test-Path -LiteralPath $statusPath) {
    $existingStatus = Get-Content -LiteralPath $statusPath -Raw | ConvertFrom-Json
    foreach ($candidate in @($existingStatus.candidates)) {
        if ($candidate.status -eq 'local-validation-only') {
            $candidate.status = 'revoked'
            $candidate.reason = 'Superseded by a newer locally verified candidate.'
        }
        $statusCandidates += $candidate
    }
}
$candidateName = Split-Path -Leaf $output
$statusCandidates += [pscustomobject]@{
    name = $candidateName
    status = 'local-validation-only'
    reason = 'Current verified local candidate; external staging gates remain open.'
}
[pscustomobject]@{
    schemaVersion = 1
    updatedAtUtc = [DateTime]::UtcNow.ToString('O')
    currentLocalCandidate = $candidateName
    deploymentAuthorized = $false
    candidates = $statusCandidates
} | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath $statusPath -Encoding UTF8

[pscustomobject]@{
    Success = $true
    CandidatePath = $output
    ManifestPath = $manifestPath
    StatusPath = $statusPath
    FileCount = $manifestFiles.Count
    DeploymentAuthorized = $false
} | ConvertTo-Json -Depth 3
