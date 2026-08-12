param(
    [string]$Configuration = "Release",
    [string]$Runtime = "win-x64",
    [string]$OutputPath = "",
    [switch]$SelfContained,
    [switch]$SkipTests
)

$ErrorActionPreference = "Stop"

$root = Split-Path -Parent $PSScriptRoot
$serverProject = Join-Path $root "src\BeeKingdom.Server\BeeKingdom.Server.csproj"
$testsProject = Join-Path $root "tests\BeeKingdom.Tests\BeeKingdom.Tests.csproj"

if ([string]::IsNullOrWhiteSpace($OutputPath)) {
    $OutputPath = Join-Path $root "artifacts\BeeKingdom.Server"
}

Write-Host "Bee Kingdom Server publish"
Write-Host "Project: $serverProject"
Write-Host "Configuration: $Configuration"
Write-Host "Runtime: $Runtime"
Write-Host "Output: $OutputPath"

dotnet restore $serverProject

if (-not $SkipTests) {
    dotnet test $testsProject --configuration $Configuration --no-restore
}

$publishArgs = @(
    "publish",
    $serverProject,
    "--configuration", $Configuration,
    "--runtime", $Runtime,
    "--output", $OutputPath,
    "/p:SelfContained=$SelfContained"
)

dotnet @publishArgs

Write-Host "Published BeeKingdom.Server to $OutputPath"
Write-Host "Run locally with:"
Write-Host "  dotnet `"$OutputPath\BeeKingdom.Server.dll`""
