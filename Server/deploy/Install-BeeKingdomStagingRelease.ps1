param(
    [Parameter(Mandatory = $true)]
    [string]$PackageZip,

    [string]$StagingRoot = "C:\BeeKingdom\staging",

    [string]$ReleaseName = "",

    [int]$Port = 5088,

    [string]$ExpectedPackageSha256 = "",

    [string]$ManifestPath = "",

    [switch]$WhatIf
)

$ErrorActionPreference = "Stop"

function Get-FullPathNoResolve {
    param([Parameter(Mandatory = $true)][string]$Path)
    return [System.IO.Path]::GetFullPath($Path)
}

function Test-IsSameOrChildPath {
    param(
        [Parameter(Mandatory = $true)][string]$Path,
        [Parameter(Mandatory = $true)][string]$Root
    )

    $fullPath = Get-FullPathNoResolve $Path
    $fullRoot = (Get-FullPathNoResolve $Root).TrimEnd([System.IO.Path]::DirectorySeparatorChar, [System.IO.Path]::AltDirectorySeparatorChar)

    if ($fullPath.Equals($fullRoot, [System.StringComparison]::OrdinalIgnoreCase)) {
        return $true
    }

    $rootWithSeparator = $fullRoot + [System.IO.Path]::DirectorySeparatorChar
    return $fullPath.StartsWith($rootWithSeparator, [System.StringComparison]::OrdinalIgnoreCase)
}

function Assert-SameOrChildPath {
    param(
        [Parameter(Mandatory = $true)][string]$Path,
        [Parameter(Mandatory = $true)][string]$Root,
        [Parameter(Mandatory = $true)][string]$Name
    )

    if (-not (Test-IsSameOrChildPath -Path $Path -Root $Root)) {
        throw "$Name must stay under its required root. Resolved: $(Get-FullPathNoResolve $Path)"
    }
}

function Assert-NoReparsePoint {
    param(
        [Parameter(Mandatory = $true)][string]$Path,
        [Parameter(Mandatory = $true)][string]$Name
    )

    $fullPath = Get-FullPathNoResolve $Path
    $current = $fullPath
    while (-not [string]::IsNullOrWhiteSpace($current)) {
        if (Test-Path -LiteralPath $current) {
            $item = Get-Item -LiteralPath $current -Force
            if (($item.Attributes -band [System.IO.FileAttributes]::ReparsePoint) -ne 0) {
                throw "$Name must not contain a reparse point: $current"
            }
        }

        $parent = Split-Path -Parent $current
        if ($parent -eq $current) {
            break
        }
        $current = $parent
    }
}

function Assert-WritePathSafe {
    param(
        [Parameter(Mandatory = $true)][string]$Path,
        [Parameter(Mandatory = $true)][string]$Root,
        [Parameter(Mandatory = $true)][string]$Name
    )

    Assert-SameOrChildPath -Path $Path -Root $Root -Name $Name
    Assert-NoReparsePoint -Path $Path -Name $Name
}

function Assert-SafeName {
    param(
        [Parameter(Mandatory = $true)][string]$Name,
        [Parameter(Mandatory = $true)][string]$Value
    )

    if ([string]::IsNullOrWhiteSpace($Value)) {
        throw "$Name is required."
    }

    if ($Value -notmatch '^[A-Za-z0-9][A-Za-z0-9._-]{0,127}$') {
        throw "$Name may only contain letters, numbers, dots, underscores and dashes, and must start with a letter or number."
    }

    if ($Value.Contains("..") -or $Value.Contains("/") -or $Value.Contains("\")) {
        throw "$Name must not contain traversal or path separators."
    }
}

function Assert-Sha256 {
    param([Parameter(Mandatory = $true)][string]$Value)

    if ($Value -notmatch '^[A-Fa-f0-9]{64}$') {
        throw "ExpectedPackageSha256 must be a 64-character SHA-256 hex string."
    }
}

function Test-ManifestMatchesDirectory {
    param(
        [Parameter(Mandatory = $true)]$Manifest,
        [Parameter(Mandatory = $true)][string]$Directory
    )

    foreach ($entry in $Manifest.files) {
        if ($entry.path -match '(^/|^[A-Za-z]:|(^|/)\.\.(/|$)|\\)') {
            throw "Manifest contains an unsafe relative path: $($entry.path)"
        }

        $entryPath = Join-Path $Directory ($entry.path -replace '/', [System.IO.Path]::DirectorySeparatorChar)
        Assert-SameOrChildPath -Path $entryPath -Root $Directory -Name "Manifest entry"
        Assert-NoReparsePoint -Path $entryPath -Name "Manifest entry"

        if (-not (Test-Path -LiteralPath $entryPath -PathType Leaf)) {
            throw "Extracted package is missing manifest file: $($entry.path)"
        }

        $file = Get-Item -LiteralPath $entryPath -Force
        if ($file.Length -ne [int64]$entry.length) {
            throw "Extracted file length mismatch: $($entry.path)"
        }

        $hash = (Get-FileHash -LiteralPath $entryPath -Algorithm SHA256).Hash.ToLowerInvariant()
        if ($hash -ne ([string]$entry.sha256).ToLowerInvariant()) {
            throw "Extracted file SHA-256 mismatch: $($entry.path)"
        }
    }
}

function Assert-ManifestExtractionTargetsSafe {
    param(
        [Parameter(Mandatory = $true)]$Manifest,
        [Parameter(Mandatory = $true)][string]$Directory
    )

    Assert-WritePathSafe -Path $Directory -Root $Directory -Name "Extraction root"

    foreach ($entry in $Manifest.files) {
        if ($entry.path -match '(^/|^[A-Za-z]:|(^|/)\.\.(/|$)|\\)') {
            throw "Manifest contains an unsafe relative path: $($entry.path)"
        }

        $entryPath = Join-Path $Directory ($entry.path -replace '/', [System.IO.Path]::DirectorySeparatorChar)
        Assert-WritePathSafe -Path $entryPath -Root $Directory -Name "Manifest extraction target"
    }
}

$resolvedPackage = Get-FullPathNoResolve ((Resolve-Path -LiteralPath $PackageZip).Path)
$stagingFullPath = Get-FullPathNoResolve $StagingRoot
$requiredRoot = Get-FullPathNoResolve "C:\BeeKingdom\staging"

Assert-SameOrChildPath -Path $stagingFullPath -Root $requiredRoot -Name "StagingRoot"
Assert-NoReparsePoint -Path $stagingFullPath -Name "StagingRoot"
Assert-NoReparsePoint -Path $resolvedPackage -Name "PackageZip"

if ($Port -lt 1024 -or $Port -gt 65535) {
    throw "Port must be between 1024 and 65535."
}

if ([string]::IsNullOrWhiteSpace($ExpectedPackageSha256)) {
    $hashSidecar = "$resolvedPackage.sha256"
    if (Test-Path -LiteralPath $hashSidecar) {
        $ExpectedPackageSha256 = (Get-Content -LiteralPath $hashSidecar -Raw).Trim()
    } else {
        throw "ExpectedPackageSha256 is required, or a sidecar .sha256 file must exist next to PackageZip."
    }
}

Assert-Sha256 -Value $ExpectedPackageSha256
$actualPackageSha256 = (Get-FileHash -LiteralPath $resolvedPackage -Algorithm SHA256).Hash.ToLowerInvariant()
if ($actualPackageSha256 -ne $ExpectedPackageSha256.ToLowerInvariant()) {
    throw "Package SHA-256 mismatch. Refusing extraction."
}

if ([string]::IsNullOrWhiteSpace($ManifestPath)) {
    $zipDirectory = Split-Path -Parent $resolvedPackage
    $zipBaseName = [System.IO.Path]::GetFileNameWithoutExtension($resolvedPackage)
    $ManifestPath = Join-Path $zipDirectory "$zipBaseName.manifest.json"
}

$resolvedManifest = Get-FullPathNoResolve ((Resolve-Path -LiteralPath $ManifestPath).Path)
Assert-NoReparsePoint -Path $resolvedManifest -Name "ManifestPath"
$manifest = Get-Content -LiteralPath $resolvedManifest -Raw | ConvertFrom-Json
if (-not $manifest.files -or $manifest.files.Count -lt 1) {
    throw "Manifest does not contain package files."
}

if ($PSBoundParameters.ContainsKey("ReleaseName") -and [string]::IsNullOrWhiteSpace($ReleaseName)) {
    throw "ReleaseName is required when provided."
}

if (-not $PSBoundParameters.ContainsKey("ReleaseName")) {
    $ReleaseName = [System.IO.Path]::GetFileNameWithoutExtension($resolvedPackage)
}

Assert-SafeName -Name "ReleaseName" -Value $ReleaseName

$releasesPath = Join-Path $stagingFullPath "releases"
$currentPath = Join-Path $stagingFullPath "current"
$logsPath = Join-Path $stagingFullPath "logs"
$backupsPath = Join-Path $stagingFullPath "backups"
$configPath = Join-Path $stagingFullPath "config"
$releasePath = Join-Path $releasesPath $ReleaseName
$currentReleaseFile = Join-Path $stagingFullPath "current-release.txt"

foreach ($path in @($releasesPath, $currentPath, $logsPath, $backupsPath, $configPath, $releasePath, $currentReleaseFile)) {
    Assert-WritePathSafe -Path $path -Root $stagingFullPath -Name "Staging child path"
}

Write-Host "Bee Kingdom staging install"
Write-Host "Package: $resolvedPackage"
Write-Host "Staging root: $stagingFullPath"
Write-Host "Release: $ReleaseName"
Write-Host "Loopback URL: http://127.0.0.1:$Port"
Write-Host "Package SHA-256 verified before extraction."

if ($WhatIf) {
    Write-Host "WhatIf: would create staging directories, expand verified package, verify extracted files and update current-release.txt."
    return
}

foreach ($path in @($releasesPath, $currentPath, $logsPath, $backupsPath, $configPath)) {
    Assert-WritePathSafe -Path $path -Root $stagingFullPath -Name "Staging directory"
    New-Item -ItemType Directory -Path $path -Force | Out-Null
    Assert-WritePathSafe -Path $path -Root $stagingFullPath -Name "Staging directory"
}

Assert-WritePathSafe -Path $releasePath -Root $stagingFullPath -Name "Release path"
if (Test-Path -LiteralPath $releasePath) {
    throw "Release already exists: $releasePath"
}

Assert-WritePathSafe -Path $releasePath -Root $stagingFullPath -Name "Release path"
New-Item -ItemType Directory -Path $releasePath -Force | Out-Null
Assert-WritePathSafe -Path $releasePath -Root $stagingFullPath -Name "Release path"
Assert-ManifestExtractionTargetsSafe -Manifest $manifest -Directory $releasePath
Expand-Archive -LiteralPath $resolvedPackage -DestinationPath $releasePath -Force
Assert-WritePathSafe -Path $releasePath -Root $stagingFullPath -Name "Release path"
Test-ManifestMatchesDirectory -Manifest $manifest -Directory $releasePath

$serverDll = Join-Path $releasePath "BeeKingdom.Server.dll"
if (-not (Test-Path -LiteralPath $serverDll)) {
    throw "Expanded package does not contain BeeKingdom.Server.dll: $releasePath"
}

Assert-WritePathSafe -Path $currentReleaseFile -Root $stagingFullPath -Name "Current release file"
$previousRelease = if (Test-Path -LiteralPath $currentReleaseFile) {
    Get-Content -LiteralPath $currentReleaseFile -Raw
} else {
    ""
}

Assert-WritePathSafe -Path $currentReleaseFile -Root $stagingFullPath -Name "Current release file"
Set-Content -LiteralPath $currentReleaseFile -Value $ReleaseName -Encoding ASCII

[pscustomobject]@{
    InstalledRelease = $ReleaseName
    PreviousRelease = $previousRelease.Trim()
    ReleasePath = $releasePath
    CurrentReleaseFile = $currentReleaseFile
    LoopbackUrl = "http://127.0.0.1:$Port"
    PackageSha256 = $actualPackageSha256
    ManifestPath = $resolvedManifest
    VerifiedFileCount = $manifest.files.Count
    StartCommand = "dotnet `"$serverDll`" --urls http://127.0.0.1:$Port"
}
