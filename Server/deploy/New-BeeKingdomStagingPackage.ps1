param(
    [string]$PublishPath = "",
    [string]$OutputPath = "",
    [string]$PackageName = ""
)

$ErrorActionPreference = "Stop"

$serverRoot = Split-Path -Parent $PSScriptRoot

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
        throw "$Name must stay under the Bee Kingdom Server workspace. Resolved: $(Get-FullPathNoResolve $Path)"
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

function Assert-PackageName {
    param([Parameter(Mandatory = $true)][string]$Name)

    if ([string]::IsNullOrWhiteSpace($Name)) {
        throw "PackageName is required."
    }

    if ($Name -notmatch '^[A-Za-z0-9][A-Za-z0-9._-]{0,127}$') {
        throw "PackageName may only contain letters, numbers, dots, underscores and dashes, and must start with a letter or number."
    }

    if ($Name.Contains("..") -or $Name.Contains("/") -or $Name.Contains("\")) {
        throw "PackageName must not contain traversal or path separators."
    }
}

if ([string]::IsNullOrWhiteSpace($PublishPath)) {
    $PublishPath = Join-Path $serverRoot "artifacts\BeeKingdom.Server"
}

if ([string]::IsNullOrWhiteSpace($OutputPath)) {
    $OutputPath = Join-Path $serverRoot "artifacts\packages"
}

$publishFullPath = Get-FullPathNoResolve ((Resolve-Path -LiteralPath $PublishPath).Path)
$serverFullPath = Get-FullPathNoResolve $serverRoot
$outputFullPath = Get-FullPathNoResolve $OutputPath

Assert-SameOrChildPath -Path $publishFullPath -Root $serverFullPath -Name "PublishPath"
Assert-SameOrChildPath -Path $outputFullPath -Root $serverFullPath -Name "OutputPath"
Assert-NoReparsePoint -Path $publishFullPath -Name "PublishPath"
Assert-NoReparsePoint -Path $outputFullPath -Name "OutputPath"

$serverDll = Join-Path $publishFullPath "BeeKingdom.Server.dll"
if (-not (Test-Path -LiteralPath $serverDll)) {
    throw "PublishPath does not contain BeeKingdom.Server.dll: $publishFullPath"
}

if ($PSBoundParameters.ContainsKey("PackageName") -and [string]::IsNullOrWhiteSpace($PackageName)) {
    throw "PackageName is required when provided."
}

if (-not $PSBoundParameters.ContainsKey("PackageName")) {
    $stamp = (Get-Date).ToUniversalTime().ToString("yyyyMMddTHHmmssZ")
    $PackageName = "BeeKingdom.Server.$stamp.win-x64"
}

Assert-PackageName -Name $PackageName

$manifestPath = Join-Path $outputFullPath "$PackageName.manifest.json"
$zipPath = Join-Path $outputFullPath "$PackageName.zip"
$packageHashPath = Join-Path $outputFullPath "$PackageName.zip.sha256"

Assert-SameOrChildPath -Path $manifestPath -Root $outputFullPath -Name "ManifestPath"
Assert-SameOrChildPath -Path $zipPath -Root $outputFullPath -Name "ZipPath"
Assert-SameOrChildPath -Path $packageHashPath -Root $outputFullPath -Name "PackageHashPath"

if (Test-Path -LiteralPath $manifestPath) {
    throw "Manifest already exists: $manifestPath"
}

if (Test-Path -LiteralPath $zipPath) {
    throw "Package already exists: $zipPath"
}

if (Test-Path -LiteralPath $packageHashPath) {
    throw "Package hash already exists: $packageHashPath"
}

New-Item -ItemType Directory -Path $outputFullPath -Force | Out-Null

$files = Get-ChildItem -Path $publishFullPath -Recurse -File | Sort-Object FullName
$manifestFiles = foreach ($file in $files) {
    $baseWithSeparator = $publishFullPath.TrimEnd("\") + "\"
    if (-not $file.FullName.StartsWith($baseWithSeparator, [System.StringComparison]::OrdinalIgnoreCase)) {
        throw "File is outside publish path: $($file.FullName)"
    }

    $relative = $file.FullName.Substring($baseWithSeparator.Length)
    [pscustomobject]@{
        path = $relative.Replace("\", "/")
        length = $file.Length
        sha256 = (Get-FileHash -LiteralPath $file.FullName -Algorithm SHA256).Hash.ToLowerInvariant()
    }
}

$manifest = [pscustomobject]@{
    packageName = $PackageName
    createdAtUtc = (Get-Date).ToUniversalTime().ToString("O")
    sourcePath = $publishFullPath
    runtime = "win-x64"
    secretPolicy = "No secrets are expected in this package. Verify before deployment."
    files = $manifestFiles
}

$manifest | ConvertTo-Json -Depth 6 | Set-Content -LiteralPath $manifestPath -Encoding UTF8
Compress-Archive -Path (Join-Path $publishFullPath "*") -DestinationPath $zipPath -CompressionLevel Optimal

$packageHash = (Get-FileHash -LiteralPath $zipPath -Algorithm SHA256).Hash.ToLowerInvariant()
$packageHash | Set-Content -LiteralPath $packageHashPath -Encoding ASCII

[pscustomobject]@{
    PackageName = $PackageName
    ZipPath = $zipPath
    ZipSha256 = $packageHash
    ManifestPath = $manifestPath
    FileCount = $files.Count
}
