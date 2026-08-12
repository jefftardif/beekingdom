param(
    [string]$ContractRoot = $PSScriptRoot
)

$ErrorActionPreference = "Stop"
$ContractRoot = (Resolve-Path -LiteralPath $ContractRoot).Path

$requiredFiles = @(
    "world-map-chunk-contract-spec.md",
    "example-window-5x5.json",
    "example-edge-window.json",
    "BeeKingdom.WorldMapChunkContractVerifier.csproj",
    "Program.cs",
    "NuGet.Config"
)

foreach ($file in $requiredFiles) {
    $path = Join-Path $ContractRoot $file
    if (-not (Test-Path -LiteralPath $path -PathType Leaf)) {
        throw "Missing contract artifact: $file"
    }
}

$dotnet = Get-Command dotnet -ErrorAction Stop
$project = Join-Path $ContractRoot "BeeKingdom.WorldMapChunkContractVerifier.csproj"
$nugetConfig = Join-Path $ContractRoot "NuGet.Config"
$localCliHome = Join-Path $ContractRoot ".dotnet-home"
$localNuGetPackages = Join-Path $ContractRoot ".nuget-packages"
$localNuGetHttpCache = Join-Path $ContractRoot ".nuget-http-cache"
$localAppData = Join-Path $ContractRoot ".appdata"

try {
    $previousDotnetCliHome = $env:DOTNET_CLI_HOME
    $previousNuGetPackages = $env:NUGET_PACKAGES
    $previousNuGetHttpCache = $env:NUGET_HTTP_CACHE_PATH
    $previousAppData = $env:APPDATA
    $previousRestoreSources = $env:RestoreSources

    $env:DOTNET_CLI_HOME = $localCliHome
    $env:NUGET_PACKAGES = $localNuGetPackages
    $env:NUGET_HTTP_CACHE_PATH = $localNuGetHttpCache
    $env:APPDATA = $localAppData
    $env:RestoreSources = ""

    New-Item -ItemType Directory -Path $localCliHome, $localNuGetPackages, $localNuGetHttpCache, $localAppData -Force | Out-Null

    $assetsFile = Join-Path $ContractRoot "obj\project.assets.json"
    if (-not (Test-Path -LiteralPath $assetsFile -PathType Leaf)) {
        & $dotnet.Source restore $project --configfile $nugetConfig --packages $localNuGetPackages --nologo
        if ($LASTEXITCODE -ne 0) {
            throw "Offline verifier restore failed with exit code $LASTEXITCODE."
        }
    }

    & $dotnet.Source run --project $project --configuration Release --no-restore --no-launch-profile -- verify $ContractRoot
    if ($LASTEXITCODE -ne 0) {
        throw "Typed world-map chunk verifier failed with exit code $LASTEXITCODE."
    }
}
finally {
    $env:DOTNET_CLI_HOME = $previousDotnetCliHome
    $env:NUGET_PACKAGES = $previousNuGetPackages
    $env:NUGET_HTTP_CACHE_PATH = $previousNuGetHttpCache
    $env:APPDATA = $previousAppData
    $env:RestoreSources = $previousRestoreSources

    foreach ($directoryName in @("bin", "obj", ".dotnet-home", ".nuget-packages", ".nuget-http-cache", ".appdata")) {
        $candidate = Join-Path $ContractRoot $directoryName
        if (-not (Test-Path -LiteralPath $candidate -PathType Container)) {
            continue
        }

        $resolved = (Resolve-Path -LiteralPath $candidate).Path
        if (-not $resolved.StartsWith($ContractRoot + [IO.Path]::DirectorySeparatorChar, [StringComparison]::OrdinalIgnoreCase)) {
            throw "Refusing verifier cleanup outside contract root: $resolved"
        }

        Remove-Item -LiteralPath $resolved -Recurse -Force
    }
}
