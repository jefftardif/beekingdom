[CmdletBinding()]
param(
    [string]$SqlInstance = '(localdb)\MSSQLLocalDB',
    [ValidateSet('Debug', 'Release')]
    [string]$Configuration = 'Debug',
    [switch]$NoRestore
)

$ErrorActionPreference = 'Stop'

if (-not $SqlInstance.StartsWith('(localdb)\', [StringComparison]::OrdinalIgnoreCase)) {
    throw 'SERVER-B-057 refuses non-LocalDB SQL instances.'
}

foreach ($commandName in @('dotnet', 'sqllocaldb', 'sqlcmd')) {
    if (-not (Get-Command $commandName -ErrorAction SilentlyContinue)) {
        throw "Required local command '$commandName' is unavailable."
    }
}

$instanceName = $SqlInstance.Substring('(localdb)\'.Length)
& sqllocaldb start $instanceName | Out-Host
if ($LASTEXITCODE -ne 0) {
    throw "Could not start LocalDB instance '$instanceName'."
}

$solution = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..\..\BeeKingdom.Server.slnx'))
$connectionString = "Server=$SqlInstance;Database=master;Integrated Security=True;TrustServerCertificate=True;Connect Timeout=15;"
$previousConnectionString = $env:BEE_SQL_INTEGRATION_CONNECTION_STRING

try {
    $env:BEE_SQL_INTEGRATION_CONNECTION_STRING = $connectionString
    $arguments = @(
        'test',
        $solution,
        '--configuration', $Configuration,
        '--filter', 'FullyQualifiedName~SqlServerOptInIntegrationTests|FullyQualifiedName~DatabaseMigrationTests|FullyQualifiedName~PersistenceProviderSelectionTests',
        '--logger', 'console;verbosity=normal'
    )
    if ($NoRestore) {
        $arguments += '--no-restore'
    }

    & dotnet @arguments
    $testExitCode = $LASTEXITCODE

    $remaining = & sqlcmd -S $SqlInstance -E -W -h -1 -Q "SET NOCOUNT ON; SELECT COUNT(*) FROM sys.databases WHERE name LIKE N'BeeKingdom[_]Local[_]SERVERB057[_]%';"
    if ($LASTEXITCODE -ne 0) {
        throw 'Could not verify disposable database cleanup.'
    }

    $remainingCount = [int](($remaining | Where-Object { -not [string]::IsNullOrWhiteSpace($_) }) -join '').Trim()
    if ($remainingCount -ne 0) {
        throw "$remainingCount disposable SERVER-B-057 database(s) remain after the test run."
    }

    if ($testExitCode -ne 0) {
        throw "SQL readiness tests failed with exit code $testExitCode."
    }
}
finally {
    $env:BEE_SQL_INTEGRATION_CONNECTION_STRING = $previousConnectionString
}
