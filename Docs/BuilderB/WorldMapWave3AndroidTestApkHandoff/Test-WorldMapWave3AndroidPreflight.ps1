[CmdletBinding()]
param(
    [string]$ProjectRoot = 'C:\projets\beekingdomgame-master',
    [string]$UnityRoot = 'C:\Program Files\Unity\Hub\Editor\6000.2.10f1\Editor',
    [ValidateSet('PASS', 'PASS_WITH_RESERVES', 'FAIL', 'PENDING')]
    [string]$QaRuntimeStep4D = 'PENDING',
    [ValidateSet('PASS', 'PASS_WITH_RESERVES', 'FAIL', 'PENDING')]
    [string]$QaArtBundleWave3 = 'PENDING',
    [ValidateSet('PASS', 'PASS_WITH_RESERVES', 'FAIL', 'PENDING')]
    [string]$QaUnityIntegrationWave3 = 'PENDING',
    [int]$MinimumFreeGiB = 20
)

$ErrorActionPreference = 'Stop'

$requiredScenes = @(
    'Assets/Scenes/SandboxPlayground.unity',
    'Assets/Scenes/WorldMapMmoFullscreenFoundation.unity'
)
$targetRelative = 'Builds/Android/BeeKingdom_WorldMapWave3_uib-wave3-continuous-v1_Development_001.apk'
$target = Join-Path $ProjectRoot $targetRelative
$androidPlayer = Join-Path $UnityRoot 'Data/PlaybackEngines/AndroidPlayer'
$sdk = Join-Path $androidPlayer 'SDK'
$jdk = Join-Path $androidPlayer 'OpenJDK'
$ndk = Join-Path $androidPlayer 'NDK'
$gradle = Join-Path $androidPlayer 'Tools/gradle'
$buildToolsRoot = Join-Path $sdk 'build-tools'
$buildTools = if (Test-Path -LiteralPath $buildToolsRoot) {
    Get-ChildItem -LiteralPath $buildToolsRoot -Directory | Sort-Object Name -Descending | Select-Object -First 1
} else {
    $null
}

$editorBuildSettingsPath = Join-Path $ProjectRoot 'ProjectSettings/EditorBuildSettings.asset'
$projectSettingsPath = Join-Path $ProjectRoot 'ProjectSettings/ProjectSettings.asset'
$projectVersionPath = Join-Path $ProjectRoot 'ProjectSettings/ProjectVersion.txt'
$editorBuildSettings = if (Test-Path -LiteralPath $editorBuildSettingsPath) {
    Get-Content -Raw -LiteralPath $editorBuildSettingsPath
} else {
    ''
}
$projectSettings = if (Test-Path -LiteralPath $projectSettingsPath) {
    Get-Content -Raw -LiteralPath $projectSettingsPath
} else {
    ''
}
$projectVersion = if (Test-Path -LiteralPath $projectVersionPath) {
    (Get-Content -Raw -LiteralPath $projectVersionPath).Trim()
} else {
    ''
}

$enabledSceneMatches = [regex]::Matches(
    $editorBuildSettings,
    '(?ms)- enabled:\s*1\s*\r?\n\s*path:\s*([^\r\n]+)'
)
$enabledScenes = @($enabledSceneMatches | ForEach-Object { $_.Groups[1].Value.Trim() })
$requiredScenesExist = @($requiredScenes | ForEach-Object {
    [ordered]@{
        path = $_
        exists = Test-Path -LiteralPath (Join-Path $ProjectRoot $_)
        enabled_in_build_settings = $enabledScenes -contains $_
    }
})
$firstTwoScenesMatch = $enabledScenes.Count -ge 2 -and
    $enabledScenes[0] -eq $requiredScenes[0] -and
    $enabledScenes[1] -eq $requiredScenes[1]

$driveName = ([System.IO.Path]::GetPathRoot($target)).TrimEnd('\').TrimEnd(':')
$freeBytes = (Get-PSDrive -Name $driveName).Free
$freeGiB = [math]::Round($freeBytes / 1GB, 2)
$toolChecks = [ordered]@{
    unity_exe = Test-Path -LiteralPath (Join-Path $UnityRoot 'Unity.exe')
    android_build_support = Test-Path -LiteralPath $androidPlayer
    embedded_sdk = Test-Path -LiteralPath $sdk
    embedded_jdk = Test-Path -LiteralPath $jdk
    embedded_ndk = Test-Path -LiteralPath $ndk
    embedded_gradle = Test-Path -LiteralPath $gradle
    adb = Test-Path -LiteralPath (Join-Path $sdk 'platform-tools/adb.exe')
    aapt2 = $null -ne $buildTools -and (Test-Path -LiteralPath (Join-Path $buildTools.FullName 'aapt2.exe'))
    apksigner = $null -ne $buildTools -and (Test-Path -LiteralPath (Join-Path $buildTools.FullName 'apksigner.bat'))
    zipalign = $null -ne $buildTools -and (Test-Path -LiteralPath (Join-Path $buildTools.FullName 'zipalign.exe'))
}
$toolsPass = @($toolChecks.Values) -notcontains $false
$scenesPass = @($requiredScenesExist | ForEach-Object { $_.exists -and $_.enabled_in_build_settings }) -notcontains $false
$customKeystoreDisabled = $projectSettings -match '(?m)^\s*androidUseCustomKeystore:\s*0\s*$'
$targetAbsent = -not (Test-Path -LiteralPath $target)
$spacePass = $freeGiB -ge $MinimumFreeGiB
$gatesPass = $QaRuntimeStep4D -eq 'PASS' -and
    $QaArtBundleWave3 -eq 'PASS' -and
    $QaUnityIntegrationWave3 -eq 'PASS'
$prerequisitesPass = $toolsPass -and $scenesPass -and $firstTwoScenesMatch -and
    $customKeystoreDisabled -and $targetAbsent -and $spacePass

$result = [ordered]@{
    schema = 'bee-kingdom.world-map-wave3-android-preflight.v1'
    mode = 'read_only_no_build'
    project_root = $ProjectRoot
    unity_root = $UnityRoot
    project_version = $projectVersion
    exact_future_target = $target
    target_absent = $targetAbsent
    minimum_free_gib = $MinimumFreeGiB
    observed_free_gib = $freeGiB
    disk_space_pass = $spacePass
    tools = $toolChecks
    tools_pass = $toolsPass
    required_scenes = $requiredScenesExist
    editor_build_settings_first_two_match = $firstTwoScenesMatch
    explicit_build_scenes_only = $requiredScenes
    custom_keystore_disabled = $customKeystoreDisabled
    gates = [ordered]@{
        qa_runtime_step4d = $QaRuntimeStep4D
        qa_art_bundle_wave3 = $QaArtBundleWave3
        qa_unity_integration_wave3 = $QaUnityIntegrationWave3
        all_strict_pass = $gatesPass
    }
    prerequisites_pass = $prerequisitesPass
    build_allowed_now = $prerequisitesPass -and $gatesPass
    no_unity_invoked = $true
    no_build_invoked = $true
    no_file_written = $true
}

$result | ConvertTo-Json -Depth 8
