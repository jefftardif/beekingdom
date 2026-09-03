[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$Email,

    [switch]$DryRun,

    [ValidateSet('Development', 'Staging', 'Production')]
    [string]$Environment = 'Development',

    [switch]$AllowProduction
)

$ErrorActionPreference = 'Stop'

if ($Environment -eq 'Production' -and -not $AllowProduction) {
    Write-Error "Refuse: -Environment Production exige aussi -AllowProduction. C'est une protection deliberee contre un reset accidentel en production."
    exit 2
}

# Two supported layouts, auto-detected so the SAME script works both inside a
# full repo checkout (dev machine) and next to a standalone published copy of
# BeeKingdom.Tools (deployment server with no source/SDK project available):
#   - Published: .\publish\BeeKingdom.Tools.dll sitting next to this script.
#   - Dev/source: this script inside Server\tools\, project at ..\src\BeeKingdom.Tools\.
$publishedDll = Join-Path $PSScriptRoot 'publish\BeeKingdom.Tools.dll'
$devProject = Join-Path (Split-Path -Parent $PSScriptRoot) 'src\BeeKingdom.Tools\BeeKingdom.Tools.csproj'

if (Test-Path -LiteralPath $publishedDll) {
    $mode = 'Published'
}
elseif (Test-Path -LiteralPath $devProject) {
    $mode = 'Dev'
}
else {
    Write-Error "Ni $publishedDll ni $devProject trouves. Copiez le dossier 'publish\' a cote de ce script (deploiement), ou lancez depuis un checkout complet du depot (dev)."
    exit 2
}

function Invoke-ResetTool {
    param([bool]$Apply)

    $previousEnv = $env:DOTNET_ENVIRONMENT
    $previousAspEnv = $env:ASPNETCORE_ENVIRONMENT
    try {
        $env:DOTNET_ENVIRONMENT = $Environment
        $env:ASPNETCORE_ENVIRONMENT = $Environment

        if ($mode -eq 'Published') {
            $toolArgs = @($publishedDll, 'reset-test-account', $Email)
        }
        else {
            $toolArgs = @('run', '--project', $devProject, '--configuration', 'Release', '--', 'reset-test-account', $Email)
        }
        if ($Apply) { $toolArgs += '--apply' }

        $output = & dotnet @toolArgs 2>&1
        $exitCode = $LASTEXITCODE
        return [pscustomobject]@{ Output = $output; ExitCode = $exitCode }
    }
    finally {
        $env:DOTNET_ENVIRONMENT = $previousEnv
        $env:ASPNETCORE_ENVIRONMENT = $previousAspEnv
    }
}

Write-Host '============================================================'
Write-Host 'BEEKINGDOM TEST ACCOUNT RESET'
Write-Host '============================================================'
Write-Host "Email:       $Email"
Write-Host "Environment: $Environment"
if ($Environment -eq 'Production') {
    Write-Host ''
    Write-Host '*** PRODUCTION DATABASE ***' -ForegroundColor Red
    Write-Host '*** This targets the LIVE production database. ***' -ForegroundColor Red
}
Write-Host ''

# --- Discovery / dry-run pass (always runs, never writes anything) ------------
$discovery = Invoke-ResetTool -Apply $false
$discovery.Output | ForEach-Object { Write-Host $_ }

if ($discovery.ExitCode -ne 0) {
    Write-Error "Le passage de decouverte a echoue (code $($discovery.ExitCode)). Arret sans tentative d'ecriture."
    exit $discovery.ExitCode
}

if ($DryRun) {
    Write-Host ''
    Write-Host 'DryRun demande explicitement - aucune donnee modifiee.'
    exit 0
}

# --- Safety gate: retype the exact email to confirm ---------------------------
Write-Host ''
Write-Host 'THIS WILL PERMANENTLY DELETE ALL BEEKINGDOM DATA FOR THIS PLAYER.' -ForegroundColor Yellow
Write-Host 'The underlying Google/email account itself is never touched - only its BeeKingdom identity and data.' -ForegroundColor Yellow
$retyped = Read-Host "Retapez exactement l'email pour confirmer ($Email)"
if ($retyped -ne $Email) {
    Write-Error 'Email retape different - annule. Aucune donnee modifiee.'
    exit 3
}

if ($Environment -eq 'Production') {
    $productionConfirm = Read-Host "Confirmation renforcee: tapez PRODUCTION en majuscules pour continuer"
    if ($productionConfirm -ne 'PRODUCTION') {
        Write-Error 'Confirmation Production non fournie - annule. Aucune donnee modifiee.'
        exit 3
    }
}

# --- Apply pass -----------------------------------------------------------------
Write-Host ''
Write-Host 'Applying...'
$apply = Invoke-ResetTool -Apply $true
$apply.Output | ForEach-Object { Write-Host $_ }

exit $apply.ExitCode
