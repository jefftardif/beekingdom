[CmdletBinding(SupportsShouldProcess = $true)]
param(
    [ValidateSet('Initialize', 'Status', 'Sync')]
    [string]$Mode = 'Status',

    [string]$VmRoot = 'C:\projets\beekingdomgame-master',

    [string]$HostRoot = '',

    [switch]$ApplyDeletions,

    [switch]$AllowLocalTestRoots
)

$ErrorActionPreference = 'Stop'
$ExpectedUnityVersion = '6000.5.3f1'
$TimestampToleranceTicks = [TimeSpan]::FromSeconds(2).Ticks

$ExcludedDirectories = [Collections.Generic.HashSet[string]]::new(
    [StringComparer]::OrdinalIgnoreCase
)
@(
    '.codex', '.git', '.idea', '.utmp', '.vs', '.vscode',
    'artifacts', 'bin', 'Build', 'Builds', 'DEMO_Evidence_Staging',
    'Library', 'Logs', 'MemoryCaptures', 'obj', 'outputs',
    'Temp', 'UserSettings'
) | ForEach-Object { [void]$ExcludedDirectories.Add($_) }

$ExcludedFileNames = [Collections.Generic.HashSet[string]]::new(
    [StringComparer]::OrdinalIgnoreCase
)
@(
    'crashlytics-build.properties', 'Desktop.ini',
    'GoogleService-Info.plist', 'google-services.json', 'Thumbs.db'
) | ForEach-Object { [void]$ExcludedFileNames.Add($_) }

$ExcludedExtensions = [Collections.Generic.HashSet[string]]::new(
    [StringComparer]::OrdinalIgnoreCase
)
@(
    '.aab', '.apk', '.booproj', '.csproj', '.mdb', '.opendb',
    '.pdb', '.pidb', '.sln', '.suo', '.svd', '.tmp',
    '.unitypackage', '.user', '.userprefs'
) | ForEach-Object { [void]$ExcludedExtensions.Add($_) }

function Get-NormalizedRoot {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Path,

        [switch]$MayNotExist
    )

    if (Test-Path -LiteralPath $Path) {
        return (Get-Item -LiteralPath $Path).FullName.TrimEnd('\')
    }

    if (-not $MayNotExist) {
        if ($Path.StartsWith('\\tsclient\', [StringComparison]::OrdinalIgnoreCase)) {
            throw "Lecteur principal non redirige dans Hyper-V: $Path. Active le lecteur C dans VMConnect > Afficher les options > Ressources locales > Plus."
        }
        throw "Dossier introuvable: $Path"
    }

    $fullPath = [IO.Path]::GetFullPath($Path).TrimEnd('\')
    $missingSegments = [Collections.Generic.Stack[string]]::new()
    $existingAncestor = $fullPath
    while (-not (Test-Path -LiteralPath $existingAncestor)) {
        $missingSegments.Push((Split-Path -Leaf $existingAncestor))
        $parent = Split-Path -Parent $existingAncestor
        if ([string]::IsNullOrWhiteSpace($parent) -or $parent -eq $existingAncestor) {
            throw "Impossible de normaliser le chemin: $Path"
        }
        $existingAncestor = $parent
    }

    $normalized = (Get-Item -LiteralPath $existingAncestor).FullName.TrimEnd('\')
    while ($missingSegments.Count -gt 0) {
        $normalized = Join-Path $normalized $missingSegments.Pop()
    }
    return $normalized.TrimEnd('\')
}

function Assert-ProjectRoot {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Root
    )

    $versionPath = Join-Path $Root 'ProjectSettings\ProjectVersion.txt'
    $manifestPath = Join-Path $Root 'Packages\manifest.json'
    $assetsPath = Join-Path $Root 'Assets'

    if (-not (Test-Path -LiteralPath $versionPath -PathType Leaf) -or
        -not (Test-Path -LiteralPath $manifestPath -PathType Leaf) -or
        -not (Test-Path -LiteralPath $assetsPath -PathType Container)) {
        throw "Le dossier n'est pas un projet Unity Bee Kingdom valide: $Root"
    }

    $versionLine = Get-Content -LiteralPath $versionPath |
        Where-Object { $_ -like 'm_EditorVersion:*' } |
        Select-Object -First 1
    $version = ($versionLine -replace '^m_EditorVersion:\s*', '').Trim()
    if ($version -ne $ExpectedUnityVersion) {
        throw "Version Unity inattendue dans $Root. Attendu: $ExpectedUnityVersion; trouve: $version"
    }
}

function Test-IsExcludedRelativePath {
    param(
        [Parameter(Mandatory = $true)]
        [string]$RelativePath
    )

    $segments = $RelativePath -split '[\\/]'
    if ($segments.Count -gt 1) {
        foreach ($segment in $segments[0..($segments.Count - 2)]) {
            if ($ExcludedDirectories.Contains($segment)) {
                return $true
            }
        }
    }

    $fileName = $segments[-1]
    if ($ExcludedFileNames.Contains($fileName)) {
        return $true
    }

    return $ExcludedExtensions.Contains([IO.Path]::GetExtension($fileName))
}

function Get-Inventory {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Root
    )

    $inventory = [Collections.Generic.Dictionary[string, object]]::new(
        [StringComparer]::OrdinalIgnoreCase
    )
    $prefix = $Root.TrimEnd('\') + '\'

    Get-ChildItem -LiteralPath $Root -File -Recurse -Force | ForEach-Object {
        $relative = $_.FullName.Substring($prefix.Length).Replace('\', '/')
        if (-not (Test-IsExcludedRelativePath -RelativePath $relative)) {
            $inventory[$relative] = [pscustomobject]@{
                Length = [long]$_.Length
                LastWriteUtcTicks = [long]$_.LastWriteTimeUtc.Ticks
            }
        }
    }

    return $inventory
}

function Test-SameSignature {
    param(
        [AllowNull()]
        $Left,

        [AllowNull()]
        $Right
    )

    if ($null -eq $Left -or $null -eq $Right) {
        return $null -eq $Left -and $null -eq $Right
    }

    return [long]$Left.Length -eq [long]$Right.Length -and
        [Math]::Abs([long]$Left.LastWriteUtcTicks - [long]$Right.LastWriteUtcTicks) -le
            $TimestampToleranceTicks
}

function Join-SafeProjectPath {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Root,

        [Parameter(Mandatory = $true)]
        [string]$RelativePath
    )

    if ($RelativePath.Contains('..')) {
        throw "Chemin relatif refuse: $RelativePath"
    }

    $full = [IO.Path]::GetFullPath(
        (Join-Path $Root $RelativePath.Replace('/', '\'))
    )
    $prefix = $Root.TrimEnd('\') + '\'
    if (-not $full.StartsWith($prefix, [StringComparison]::OrdinalIgnoreCase)) {
        throw "Le chemin sort du projet: $full"
    }

    return $full
}

function Copy-SynchronizedFile {
    param(
        [Parameter(Mandatory = $true)]
        [string]$SourceRoot,

        [Parameter(Mandatory = $true)]
        [string]$DestinationRoot,

        [Parameter(Mandatory = $true)]
        [string]$RelativePath
    )

    $source = Join-SafeProjectPath -Root $SourceRoot -RelativePath $RelativePath
    $destination = Join-SafeProjectPath -Root $DestinationRoot -RelativePath $RelativePath
    $destinationDirectory = Split-Path -Parent $destination

    if (-not (Test-Path -LiteralPath $destinationDirectory)) {
        New-Item -ItemType Directory -Path $destinationDirectory -Force | Out-Null
    }

    Copy-Item -LiteralPath $source -Destination $destination -Force
    $sourceItem = Get-Item -LiteralPath $source
    (Get-Item -LiteralPath $destination).LastWriteTimeUtc = $sourceItem.LastWriteTimeUtc
}

function Get-FileSignature {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Path
    )

    $item = Get-Item -LiteralPath $Path
    return [pscustomobject]@{
        Length = [long]$item.Length
        LastWriteUtcTicks = [long]$item.LastWriteTimeUtc.Ticks
    }
}

function Test-SameFileContent {
    param(
        [Parameter(Mandatory = $true)]
        [string]$LeftPath,

        [Parameter(Mandatory = $true)]
        [string]$RightPath
    )

    $leftItem = Get-Item -LiteralPath $LeftPath
    $rightItem = Get-Item -LiteralPath $RightPath
    if ($leftItem.Length -ne $rightItem.Length) {
        return $false
    }

    return (Get-FileHash -Algorithm SHA256 -LiteralPath $LeftPath).Hash -eq
        (Get-FileHash -Algorithm SHA256 -LiteralPath $RightPath).Hash
}

function Read-SyncState {
    param(
        [Parameter(Mandatory = $true)]
        [string]$StatePath
    )

    if (-not (Test-Path -LiteralPath $StatePath -PathType Leaf)) {
        throw "Etat de synchronisation absent. Lance d'abord le mode Initialize: $StatePath"
    }

    $document = Get-Content -LiteralPath $StatePath -Raw | ConvertFrom-Json
    if ([int]$document.SchemaVersion -ne 1) {
        throw "Version d'etat de synchronisation non prise en charge."
    }

    $files = [Collections.Generic.Dictionary[string, object]]::new(
        [StringComparer]::OrdinalIgnoreCase
    )
    foreach ($property in $document.Files.PSObject.Properties) {
        $files[$property.Name] = $property.Value
    }

    return $files
}

function Write-SyncState {
    param(
        [Parameter(Mandatory = $true)]
        [string]$StatePath,

        [Parameter(Mandatory = $true)]
        [Collections.Generic.Dictionary[string, object]]$Files,

        [Parameter(Mandatory = $true)]
        [string]$ResolvedVmRoot,

        [Parameter(Mandatory = $true)]
        [string]$ResolvedHostRoot
    )

    $stateDirectory = Split-Path -Parent $StatePath
    if (-not (Test-Path -LiteralPath $stateDirectory)) {
        New-Item -ItemType Directory -Path $stateDirectory -Force | Out-Null
    }

    $orderedFiles = [ordered]@{}
    foreach ($key in ($Files.Keys | Sort-Object)) {
        $orderedFiles[$key] = $Files[$key]
    }

    $document = [ordered]@{
        SchemaVersion = 1
        UpdatedUtc = [DateTime]::UtcNow.ToString('o')
        UnityVersion = $ExpectedUnityVersion
        VmRoot = $ResolvedVmRoot
        HostRoot = $ResolvedHostRoot
        Files = $orderedFiles
    }
    $document | ConvertTo-Json -Depth 6 | Set-Content -LiteralPath $StatePath -Encoding UTF8
}

function Write-SyncReport {
    param(
        [Parameter(Mandatory = $true)]
        [string]$ReportPath,

        [Parameter(Mandatory = $true)]
        [hashtable]$Report
    )

    $directory = Split-Path -Parent $ReportPath
    if (-not (Test-Path -LiteralPath $directory)) {
        New-Item -ItemType Directory -Path $directory -Force | Out-Null
    }

    $lines = @(
        "Bee Kingdom VM Sync - $($Report.Mode)",
        "Date UTC: $([DateTime]::UtcNow.ToString('o'))",
        "Copies VM vers ordinateur: $($Report.VmToHost.Count)",
        "Copies ordinateur vers VM: $($Report.HostToVm.Count)",
        "Suppressions appliquees: $($Report.Deletions.Count)",
        "Suppressions en attente: $($Report.PendingDeletions.Count)",
        "Conflits bloques: $($Report.Conflicts.Count)",
        ''
    )

    foreach ($section in @('VmToHost', 'HostToVm', 'Deletions', 'PendingDeletions', 'Conflicts')) {
        $lines += "[$section]"
        $lines += $Report[$section]
        $lines += ''
    }

    $lines | Set-Content -LiteralPath $ReportPath -Encoding UTF8
}

$privateShareHosts = @('DESKTOP-D3D29K7')
try {
    $defaultGateway = Get-NetIPConfiguration -ErrorAction Stop |
        ForEach-Object { $_.IPv4DefaultGateway.NextHop } |
        Where-Object { -not [string]::IsNullOrWhiteSpace($_) } |
        Select-Object -First 1
    if (-not [string]::IsNullOrWhiteSpace($defaultGateway)) {
        $privateShareHosts += $defaultGateway
    }
}
catch {
    $defaultGateway = $null
}

if ([string]::IsNullOrWhiteSpace($HostRoot)) {
    $hostCandidates = @(
        $privateShareHosts | ForEach-Object { "\\$_\BeeKingdomHost" }
    ) + @('\\tsclient\C\projets\beekingdomgame-master')
    $HostRoot = $hostCandidates |
        Where-Object { Test-Path -LiteralPath $_ } |
        Select-Object -First 1
    if ([string]::IsNullOrWhiteSpace($HostRoot)) {
        throw 'Projet principal inaccessible. Configure le partage \\DESKTOP-D3D29K7\BeeKingdomHost ou redirige le lecteur C avec VMConnect.'
    }
}

$resolvedHostRoot = Get-NormalizedRoot -Path $HostRoot
$resolvedVmRoot = Get-NormalizedRoot -Path $VmRoot -MayNotExist

if (-not $AllowLocalTestRoots) {
    $isRedirectedDrive = $resolvedHostRoot.StartsWith(
        '\\tsclient\',
        [StringComparison]::OrdinalIgnoreCase
    )
    $isPrivateProjectShare = $privateShareHosts |
        Where-Object {
            $resolvedHostRoot.Equals(
                "\\$_\BeeKingdomHost",
                [StringComparison]::OrdinalIgnoreCase
            )
        } |
        Select-Object -First 1
    if (-not $isRedirectedDrive -and -not $isPrivateProjectShare) {
        throw 'Par securite, HostRoot doit etre le partage BeeKingdomHost ou un lecteur redirige \\tsclient.'
    }
}

if ($resolvedHostRoot.Equals($resolvedVmRoot, [StringComparison]::OrdinalIgnoreCase)) {
    throw 'HostRoot et VmRoot doivent etre deux dossiers distincts.'
}

Assert-ProjectRoot -Root $resolvedHostRoot

$statePath = Join-Path $resolvedVmRoot '.codex\vm-sync-state.json'
$reportPath = Join-Path $resolvedVmRoot '.codex\vm-sync-last-report.txt'

if ($Mode -eq 'Initialize') {
    if (Test-Path -LiteralPath $statePath) {
        throw "La synchronisation est deja initialisee: $statePath"
    }

    if (-not (Test-Path -LiteralPath $resolvedVmRoot)) {
        New-Item -ItemType Directory -Path $resolvedVmRoot -Force | Out-Null
    }

    $allowedBootstrapFiles = [Collections.Generic.HashSet[string]]::new(
        [StringComparer]::OrdinalIgnoreCase
    )
    @(
        'tools/vm-sync/BeeKingdom-VmSync.ps1',
        'tools/vm-sync/Initialiser-BeeKingdom-VM.cmd',
        'tools/vm-sync/README_FR.md',
        'tools/vm-sync/Synchroniser-BeeKingdom.cmd',
        'tools/vm-sync/Verifier-Synchronisation.cmd'
    ) | ForEach-Object { [void]$allowedBootstrapFiles.Add($_) }

    $vmPrefix = $resolvedVmRoot.TrimEnd('\') + '\'
    $unexpectedExistingFile = Get-ChildItem -LiteralPath $resolvedVmRoot -File -Recurse -Force |
        ForEach-Object {
            $relative = $_.FullName.Substring($vmPrefix.Length).Replace('\', '/')
            if (-not $relative.StartsWith('.codex/', [StringComparison]::OrdinalIgnoreCase) -and
                -not $allowedBootstrapFiles.Contains($relative)) {
                $_
            }
        } |
        Select-Object -First 1
    if ($null -ne $unexpectedExistingFile) {
        throw "La copie VM n'est pas vide. Initialisation refusee pour eviter un ecrasement: $resolvedVmRoot"
    }

    $robocopyArguments = @(
        $resolvedHostRoot,
        $resolvedVmRoot,
        '/E', '/COPY:DAT', '/DCOPY:DAT', '/R:2', '/W:2', '/XJ', '/MT:8',
        '/FFT', '/NP', '/NFL', '/NDL',
        '/XD'
    ) + @($ExcludedDirectories) + @(
        '/XF',
        '*.aab', '*.apk', '*.booproj', '*.csproj', '*.mdb', '*.opendb',
        '*.pdb', '*.pidb', '*.sln', '*.suo', '*.svd', '*.tmp',
        '*.unitypackage', '*.user', '*.userprefs',
        'crashlytics-build.properties', 'Desktop.ini',
        'GoogleService-Info.plist', 'google-services.json', 'Thumbs.db'
    )

    & robocopy @robocopyArguments | Out-Null
    $robocopyExitCode = $LASTEXITCODE
    if ($robocopyExitCode -ge 8) {
        throw "La copie initiale a echoue. Code Robocopy: $robocopyExitCode"
    }

    Assert-ProjectRoot -Root $resolvedVmRoot
    $initialInventory = Get-Inventory -Root $resolvedVmRoot
    $initialStateArguments = @{
        StatePath = $statePath
        Files = $initialInventory
        ResolvedVmRoot = $resolvedVmRoot
        ResolvedHostRoot = $resolvedHostRoot
    }
    Write-SyncState @initialStateArguments

    Write-Host "Initialisation terminee: $($initialInventory.Count) fichiers suivis."
    Write-Host "Copie VM: $resolvedVmRoot"
    exit 0
}

Assert-ProjectRoot -Root $resolvedVmRoot
$baseline = Read-SyncState -StatePath $statePath
$vmInventory = Get-Inventory -Root $resolvedVmRoot
$hostInventory = Get-Inventory -Root $resolvedHostRoot

$allPaths = [Collections.Generic.HashSet[string]]::new(
    [StringComparer]::OrdinalIgnoreCase
)
@($baseline.Keys) + @($vmInventory.Keys) + @($hostInventory.Keys) |
    ForEach-Object { [void]$allPaths.Add($_) }

$report = @{
    Mode = $Mode
    VmToHost = [Collections.Generic.List[string]]::new()
    HostToVm = [Collections.Generic.List[string]]::new()
    Deletions = [Collections.Generic.List[string]]::new()
    PendingDeletions = [Collections.Generic.List[string]]::new()
    Conflicts = [Collections.Generic.List[string]]::new()
}

foreach ($relativePath in ($allPaths | Sort-Object)) {
    $baselineSignature = if ($baseline.ContainsKey($relativePath)) { $baseline[$relativePath] } else { $null }
    $vmSignature = if ($vmInventory.ContainsKey($relativePath)) { $vmInventory[$relativePath] } else { $null }
    $hostSignature = if ($hostInventory.ContainsKey($relativePath)) { $hostInventory[$relativePath] } else { $null }

    $vmChanged = -not (Test-SameSignature -Left $baselineSignature -Right $vmSignature)
    $hostChanged = -not (Test-SameSignature -Left $baselineSignature -Right $hostSignature)

    if (-not $vmChanged -and -not $hostChanged) {
        continue
    }

    if ($vmChanged -and -not $hostChanged) {
        if ($null -eq $vmSignature) {
            if ($ApplyDeletions -and $Mode -eq 'Sync') {
                $hostPath = Join-SafeProjectPath -Root $resolvedHostRoot -RelativePath $relativePath
                if ($PSCmdlet.ShouldProcess($hostPath, 'Supprimer selon la copie VM')) {
                    Remove-Item -LiteralPath $hostPath -Force
                    [void]$baseline.Remove($relativePath)
                    $report.Deletions.Add("ordinateur <- suppression VM: $relativePath")
                }
            }
            else {
                $report.PendingDeletions.Add("ordinateur <- suppression VM: $relativePath")
            }
        }
        elseif ($Mode -eq 'Sync') {
            $copyToHostArguments = @{
                SourceRoot = $resolvedVmRoot
                DestinationRoot = $resolvedHostRoot
                RelativePath = $relativePath
            }
            Copy-SynchronizedFile @copyToHostArguments
            $baseline[$relativePath] = Get-FileSignature -Path (
                Join-SafeProjectPath -Root $resolvedVmRoot -RelativePath $relativePath
            )
            $report.VmToHost.Add($relativePath)
        }
        else {
            $report.VmToHost.Add($relativePath)
        }
        continue
    }

    if ($hostChanged -and -not $vmChanged) {
        if ($null -eq $hostSignature) {
            if ($ApplyDeletions -and $Mode -eq 'Sync') {
                $vmPath = Join-SafeProjectPath -Root $resolvedVmRoot -RelativePath $relativePath
                if ($PSCmdlet.ShouldProcess($vmPath, 'Supprimer selon la copie ordinateur')) {
                    Remove-Item -LiteralPath $vmPath -Force
                    [void]$baseline.Remove($relativePath)
                    $report.Deletions.Add("VM <- suppression ordinateur: $relativePath")
                }
            }
            else {
                $report.PendingDeletions.Add("VM <- suppression ordinateur: $relativePath")
            }
        }
        elseif ($Mode -eq 'Sync') {
            $copyToVmArguments = @{
                SourceRoot = $resolvedHostRoot
                DestinationRoot = $resolvedVmRoot
                RelativePath = $relativePath
            }
            Copy-SynchronizedFile @copyToVmArguments
            $baseline[$relativePath] = Get-FileSignature -Path (
                Join-SafeProjectPath -Root $resolvedHostRoot -RelativePath $relativePath
            )
            $report.HostToVm.Add($relativePath)
        }
        else {
            $report.HostToVm.Add($relativePath)
        }
        continue
    }

    if ($null -eq $vmSignature -and $null -eq $hostSignature) {
        [void]$baseline.Remove($relativePath)
        continue
    }

    if ($null -ne $vmSignature -and $null -ne $hostSignature) {
        $vmPath = Join-SafeProjectPath -Root $resolvedVmRoot -RelativePath $relativePath
        $hostPath = Join-SafeProjectPath -Root $resolvedHostRoot -RelativePath $relativePath
        if (Test-SameFileContent -LeftPath $vmPath -RightPath $hostPath) {
            $baseline[$relativePath] = Get-FileSignature -Path $vmPath
            continue
        }
    }

    $report.Conflicts.Add($relativePath)
}

if ($Mode -eq 'Sync') {
    $updatedStateArguments = @{
        StatePath = $statePath
        Files = $baseline
        ResolvedVmRoot = $resolvedVmRoot
        ResolvedHostRoot = $resolvedHostRoot
    }
    Write-SyncState @updatedStateArguments
}

Write-SyncReport -ReportPath $reportPath -Report $report

Write-Host "Copies VM vers ordinateur: $($report.VmToHost.Count)"
Write-Host "Copies ordinateur vers VM: $($report.HostToVm.Count)"
Write-Host "Suppressions appliquees: $($report.Deletions.Count)"
Write-Host "Suppressions en attente: $($report.PendingDeletions.Count)"
Write-Host "Conflits bloques: $($report.Conflicts.Count)"
Write-Host "Rapport: $reportPath"

if ($report.Conflicts.Count -gt 0 -or $report.PendingDeletions.Count -gt 0) {
    exit 2
}

exit 0
