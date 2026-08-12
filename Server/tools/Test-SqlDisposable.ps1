[CmdletBinding()]
param([switch]$NoBuild)

$ErrorActionPreference = 'Stop'
$variableName = 'BEE_SQL_INTEGRATION_CONNECTION_STRING'
$connection = [Environment]::GetEnvironmentVariable($variableName, 'Process')

if ([string]::IsNullOrWhiteSpace($connection)) {
    throw "$variableName doit etre fourni dans l'environnement du processus; aucune valeur n'est lue depuis le depot."
}
if ($connection -notmatch '(?i)(^|;)\s*(Server|Data Source)\s*=\s*\(localdb\)\\') {
    throw 'Seule une instance SQL Server LocalDB est autorisee; toute cible distante est refusee.'
}
if ($connection -match '(?i)(^|;)\s*(User ID|UID|Password|PWD)\s*=') {
    throw 'Les credentials SQL sont refuses; utiliser Integrated Security LocalDB.'
}
if ($connection -notmatch '(?i)(Integrated Security\s*=\s*(true|sspi)|Trusted_Connection\s*=\s*true)') {
    throw 'Integrated Security est obligatoire.'
}
if (-not (Get-Command SqlLocalDB.exe -ErrorAction SilentlyContinue)) {
    throw 'SQL Server LocalDB n’est pas installe sur cette VM.'
}

$serverRoot = Split-Path -Parent $PSScriptRoot
$project = Join-Path $serverRoot 'tests\BeeKingdom.Tests\BeeKingdom.Tests.csproj'
$arguments = @('test', $project, '--configuration', 'Release', '--filter', 'FullyQualifiedName~SqlServerOptInIntegrationTests', '--logger', 'console;verbosity=minimal')
if ($NoBuild) { $arguments += '--no-build' }

$previousRollForward = $env:DOTNET_ROLL_FORWARD
try {
    $env:DOTNET_ROLL_FORWARD = 'Major'
    & dotnet @arguments
    if ($LASTEXITCODE -ne 0) { throw "Les tests SQL jetables ont echoue ($LASTEXITCODE)." }
}
finally {
    $env:DOTNET_ROLL_FORWARD = $previousRollForward
}
